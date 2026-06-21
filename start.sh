#!/bin/bash
set -e

echo "=== 保险经纪佣金结算系统 ==="
echo ""

# 检查 dotnet
if ! command -v dotnet &> /dev/null; then
    echo "❌ 请先安装 .NET 8 SDK"
    exit 1
fi

# 检查 node
if ! command -v node &> /dev/null; then
    echo "❌ 请先安装 Node.js 18+"
    exit 1
fi

echo "1/3 启动后端 (端口 5000)..."
cd "$(dirname "$0")/backend/CommissionSettlement"
dotnet restore > /dev/null 2>&1
dotnet run > backend.log 2>&1 &
BACKEND_PID=$!
echo "  后端 PID: $BACKEND_PID"
sleep 5

echo "2/3 安装前端依赖..."
cd "$(dirname "$0")/frontend"
if [ ! -d "node_modules" ]; then
    npm install --silent
fi

echo "3/3 启动前端 (端口 4200)..."
npm start > frontend.log 2>&1 &
FRONTEND_PID=$!
echo "  前端 PID: $FRONTEND_PID"

echo ""
echo "✅ 启动完成！"
echo "   前端: http://localhost:4200"
echo "   后端: http://localhost:5000/swagger"
echo ""
echo "   演示账号: admin / finance01 / sup01 / agent01 / agent02"
echo "   按 Ctrl+C 停止所有服务"

trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; echo 服务已停止" EXIT
wait
