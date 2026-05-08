# 📤 GitHub上传指南

## 快速上传（推荐方式）

### 方式1: 使用自动脚本
```bash
cd /home/appops/workspace/unity-td-shooter
./push_to_github.sh
```
按提示输入GitHub用户名和仓库名即可。

### 方式2: 手动命令
```bash
cd /home/appops/workspace/unity-td-shooter

# 1. 切换到main分支
git branch -M main

# 2. 添加远程仓库（替换为你的用户名和仓库名）
git remote add origin https://github.com/AsFawn124/zombie-defense-unity.git

# 3. 推送到GitHub
git push -u origin main
```

## 详细步骤

### 第1步: 在GitHub创建仓库
1. 访问 https://github.com/new
2. 填写仓库信息:
   - Repository name: `zombie-defense-unity`
   - Description: `Unity塔防射击游戏 - 僵尸防线`
   - 选择 Public 或 Private
   - 不要勾选 "Initialize this repository with a README"
3. 点击 "Create repository"

### 第2步: 推送代码
在终端执行:
```bash
cd /home/appops/workspace/unity-td-shooter
git branch -M main
git remote add origin https://github.com/AsFawn124/zombie-defense-unity.git
git push -u origin main
```

### 第3步: 登录验证
如果提示输入用户名和密码:
- 用户名: 你的GitHub用户名
- 密码: 你的GitHub个人访问令牌(PAT)，不是登录密码

#### 如何创建个人访问令牌(PAT):
1. 访问 https://github.com/settings/tokens
2. 点击 "Generate new token (classic)"
3. 填写Note: "Unity Project"
4. 选择有效期
5. 勾选权限: `repo` (完整仓库访问)
6. 点击 "Generate token"
7. 复制生成的令牌（只显示一次）

### 第4步: 验证上传
访问 `https://github.com/AsFawn124/zombie-defense-unity`
确认代码已成功上传。

## SSH方式（可选）

如果你已配置SSH密钥，可以使用SSH方式:

```bash
git remote add origin git@github.com:AsFawn124/zombie-defense-unity.git
git push -u origin main
```

### 配置SSH密钥:
```bash
# 生成密钥
ssh-keygen -t ed25519 -C "your-email@example.com"

# 添加密钥到ssh-agent
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_ed25519

# 复制公钥到GitHub
cat ~/.ssh/id_ed25519.pub
# 然后访问 https://github.com/settings/keys 添加新密钥
```

## 常见问题

### Q: 推送时提示 "Permission denied"
A: 检查是否使用了正确的个人访问令牌，或SSH密钥是否已配置。

### Q: 提示 "fatal: repository not found"
A: 检查仓库名是否正确，或仓库是否已创建。

### Q: 提示 "fatal: remote origin already exists"
A: 先删除现有远程仓库: `git remote remove origin`，然后重新添加。

### Q: 如何更新已上传的代码？
A: 修改代码后执行:
```bash
git add -A
git commit -m "更新说明"
git push
```

## 项目信息

- **本地路径**: `/home/appops/workspace/unity-td-shooter`
- **代码行数**: 6,600+ 行
- **脚本数量**: 27个C#脚本
- **文档数量**: 9个Markdown文档
- **总文件数**: 40+

## 上传后

上传成功后，你可以:
1. 在GitHub上查看代码
2. 分享给其他人克隆
3. 在其他电脑上继续开发
4. 使用GitHub Actions自动构建

## 克隆项目

其他人可以通过以下命令克隆项目:
```bash
git clone https://github.com/AsFawn124/zombie-defense-unity.git
```

---

**现在就开始上传到GitHub吧！** 🚀
