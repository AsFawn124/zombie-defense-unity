#!/bin/bash

# 僵尸防线 - GitHub推送脚本

echo "=== 僵尸防线 GitHub推送脚本 ==="
echo ""

# 检查是否已初始化git
if [ ! -d ".git" ]; then
    echo "错误: 未找到.git目录，请先运行 git init"
    exit 1
fi

# 获取GitHub用户名
echo "请输入你的GitHub用户名:"
read username

# 获取仓库名
echo "请输入仓库名 (默认: zombie-defense-unity):"
read repo_name
if [ -z "$repo_name" ]; then
    repo_name="zombie-defense-unity"
fi

# 选择协议
echo "选择连接方式:"
echo "1) HTTPS (需要用户名密码/PAT)"
echo "2) SSH (需要配置SSH密钥)"
read -p "请选择 (1或2): " protocol

if [ "$protocol" = "2" ]; then
    remote_url="git@github.com:$username/$repo_name.git"
else
    remote_url="https://github.com/$username/$repo_name.git"
fi

echo ""
echo "配置信息:"
echo "  用户名: $username"
echo "  仓库名: $repo_name"
echo "  地址: $remote_url"
echo ""

# 检查远程仓库是否已存在
if git remote get-url origin &> /dev/null; then
    echo "远程仓库已存在，更新地址..."
    git remote set-url origin "$remote_url"
else
    echo "添加远程仓库..."
    git remote add origin "$remote_url"
fi

# 切换到main分支
echo "切换到main分支..."
git branch -M main

# 推送到GitHub
echo ""
echo "正在推送到GitHub..."
echo "如果提示输入密码，请输入你的GitHub个人访问令牌(PAT)"
echo ""

if git push -u origin main; then
    echo ""
    echo "✅ 推送成功!"
    echo ""
    echo "仓库地址: https://github.com/$username/$repo_name"
    echo ""
    echo "接下来你可以:"
    echo "1. 在Unity中打开项目"
    echo "2. 使用 Tools → Quick Scene Setup 创建场景"
    echo "3. 运行游戏!"
else
    echo ""
    echo "❌ 推送失败"
    echo ""
    echo "可能的解决方案:"
    echo "1. 确保GitHub仓库已创建: https://github.com/new"
    echo "2. 检查用户名和仓库名是否正确"
    echo "3. 如果使用HTTPS，确保使用个人访问令牌而非密码"
    echo "4. 如果使用SSH，确保已配置SSH密钥"
    echo ""
    echo "手动推送命令:"
    echo "  git push -u origin main"
fi
