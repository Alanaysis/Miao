#!/bin/bash
# PixelForge 一键启动脚本

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PORT="${1:-8080}"

# 检查虚拟环境
if [ ! -d "$SCRIPT_DIR/.venv" ]; then
    echo "创建虚拟环境..."
    python3 -m venv "$SCRIPT_DIR/.venv"
    source "$SCRIPT_DIR/.venv/bin/activate"
    pip install Pillow requests -q
else
    source "$SCRIPT_DIR/.venv/bin/activate"
fi

# 启动服务器
echo ""
echo "  PIXELFORGE"
echo "  http://localhost:$PORT"
echo "  Ctrl+C 停止"
echo ""

python3 "$SCRIPT_DIR/server.py" "$PORT"
