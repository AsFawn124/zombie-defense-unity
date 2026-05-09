using UnityEngine;
using System;
using System.Collections;
using ZombieDefense.Upgrade.Data;

namespace ZombieDefense.Upgrade.Systems.Hero
{
    /// <summary>
    /// 英雄控制器 - 处理英雄的移动、攻击和技能
    /// </summary>
    public class HeroController : MonoBehaviour
    {
        [Header("组件")]
        public Animator Animator;
        public SpriteRenderer SpriteRenderer;
        public Rigidbody2D Rigidbody;

        [Header("攻击")]
        public Transform AttackPoint;
        public float AttackRadius = 0.5f;
        public LayerMask EnemyLayer;

        [Header("特效")]
        public ParticleSystem MoveEffect;
        public ParticleSystem AttackEffect;
        public GameObject LevelUpEffect;

        // 数据
        private HeroData heroData;
        private HeroStats currentStats;

        // 状态
        private bool isMoving = false;
        private bool isAttacking = false;
        private bool isDead = false;
        private Vector2 moveDirection;
        private float lastAttackTime = 0f;

        // 目标
        private Enemy currentTarget;
        private Vector3 targetPosition;

        // 事件
        public event Action OnAttack;
        public event Action<float> OnDamageDealt;
        public event Action OnMoveStart;
        public event Action OnMoveEnd;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(HeroData data, HeroStats stats)
        {
            heroData = data;
            currentStats = stats;

            // 设置外观
            if (SpriteRenderer != null && data.HeroPortrait != null)
            {
                // 使用头像作为精灵（实际项目中应该有专门的精灵）
            }

            isDead = false;
        }

        private void Update()
        {
            if (isDead) return;

            HandleInput();
            HandleMovement();
            HandleAttack();
            UpdateAnimation();
        }

        #region 输入处理

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 键盘移动
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveDirection = new Vector2(horizontal, vertical).normalized;

            // 鼠标点击移动
            if (Input.GetMouseButtonDown(1)) // 右键点击
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                SetTargetPosition(mousePos);
            }

            // 技能释放
            if (Input.GetKeyDown(KeyCode.Q))
                TryUseSkill(0);
            if (Input.GetKeyDown(KeyCode.W))
                TryUseSkill(1);
            if (Input.GetKeyDown(KeyCode.E))
                TryUseSkill(2);
            if (Input.GetKeyDown(KeyCode.R))
                TryUseSkill(3);
        }

        #endregion

        #region 移动

        /// <summary>
        /// 设置目标位置
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            targetPosition = position;
            isMoving = true;
            OnMoveStart?.Invoke();

            if (MoveEffect != null)
                MoveEffect.Play();
        }

        /// <summary>
        /// 处理移动
        /// </summary>
        private void HandleMovement()
        {
            if (!isMoving) return;

            Vector3 direction = targetPosition - transform.position;
            direction.z = 0;

            if (direction.magnitude < 0.1f)
            {
                // 到达目标
                isMoving = false;
                OnMoveEnd?.Invoke();

                if (MoveEffect != null)
                    MoveEffect.Stop();
                return;
            }

            // 移动
            direction.Normalize();
            float speed = currentStats?.MoveSpeed ?? 5f;
            transform.position += direction * speed * Time.deltaTime;

            // 朝向
            if (direction.x != 0)
            {
                SpriteRenderer.flipX = direction.x < 0;
            }
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
            moveDirection = Vector2.zero;

            if (MoveEffect != null)
                MoveEffect.Stop();
        }

        #endregion

        #region 攻击

        /// <summary>
        /// 处理攻击
        /// </summary>
        private void HandleAttack()
        {
            if (isAttacking) return;

            // 寻找目标
            FindTarget();

            // 自动攻击
            if (currentTarget != null && CanAttack())
            {
                Attack();
            }
        }

        /// <summary>
        /// 寻找目标
        /// </summary>
        private void FindTarget()
        {
            if (currentStats == null) return;

            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, currentStats.AttackRange, EnemyLayer);

            float closestDistance = float.MaxValue;
            Enemy closestEnemy = null;

            foreach (var collider in enemies)
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null && enemy.IsAlive)
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            }

            currentTarget = closestEnemy;
        }

        /// <summary>
        /// 检查是否可以攻击
        /// </summary>
        private bool CanAttack()
        {
            if (currentStats == null) return false;

            float interval = 1f / currentStats.AttackSpeed;
            return Time.time - lastAttackTime >= interval;
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        private void Attack()
        {
            if (currentTarget == null) return;

            isAttacking = true;
            lastAttackTime = Time.time;

            // 朝向目标
            Vector2 direction = currentTarget.transform.position - transform.position;
            if (direction.x != 0)
            {
                SpriteRenderer.flipX = direction.x < 0;
            }

            // 播放动画
            Animator?.SetTrigger("Attack");

            // 延迟造成伤害（配合动画）
            StartCoroutine(DealDamageDelayed(0.2f));

            OnAttack?.Invoke();
        }

        /// <summary>
        /// 延迟造成伤害
        /// </summary>
        private IEnumerator DealDamageDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (currentTarget != null && currentTarget.IsAlive)
            {
                float damage = CalculateDamage();
                currentTarget.TakeDamage(damage);

                // 播放特效
                if (AttackEffect != null)
                {
                    AttackEffect.Play();
                }

                OnDamageDealt?.Invoke(damage);
            }

            isAttacking = false;
        }

        /// <summary>
        /// 计算伤害
        /// </summary>
        private float CalculateDamage()
        {
            if (currentStats == null) return 0;

            float damage = currentStats.AttackDamage;

            // 暴击判定
            if (UnityEngine.Random.value < currentStats.CritChance)
            {
                damage *= currentStats.CritDamage;
            }

            return damage;
        }

        #endregion

        #region 技能

        /// <summary>
        /// 尝试使用技能
        /// </summary>
        private void TryUseSkill(int skillIndex)
        {
            if (HeroSystem.Instance != null)
            {
                Vector3 targetPos = GetMouseWorldPosition();
                HeroSystem.Instance.UseSkill(skillIndex, targetPos);
            }
        }

        /// <summary>
        /// 获取鼠标世界位置
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
            return Camera.main.ScreenToWorldPoint(mousePos);
        }

        #endregion

        #region 动画

        /// <summary>
        /// 更新动画
        /// </summary>
        private void UpdateAnimation()
        {
            if (Animator == null) return;

            Animator.SetBool("IsMoving", isMoving);
            Animator.SetBool("IsAttacking", isAttacking);
        }

        #endregion

        #region 伤害和死亡

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            HeroSystem.Instance?.TakeDamage(damage);

            // 播放受击动画
            Animator?.SetTrigger("Hit");

            // 检查死亡
            if (HeroSystem.Instance?.GetCurrentHealth() <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 死亡
        /// </summary>
        private void Die()
        {
            isDead = true;
            Animator?.SetTrigger("Die");

            // 禁用碰撞
            var collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;

            Debug.Log($"[HeroController] 英雄死亡");
        }

        #endregion

        #region 效果

        /// <summary>
        /// 播放升级特效
        /// </summary>
        public void PlayLevelUpEffect()
        {
            if (LevelUpEffect != null)
            {
                Instantiate(LevelUpEffect, transform.position, Quaternion.identity);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // 攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentStats?.AttackRange ?? 5f);

            // 移动目标
            if (isMoving)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.2f);
            }
        }

        #endregion
    }
}