import subprocess
import time
import sys
import os
import signal
import pytest
import urllib.request

TOOL_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PORT = 18080
BASE_URL = f"http://localhost:{PORT}"


def _kill_port(port):
    """杀掉占用指定端口的进程"""
    try:
        out = subprocess.check_output(["lsof", "-ti", f":{port}"], stderr=subprocess.DEVNULL)
        for pid in out.strip().split():
            os.kill(int(pid), signal.SIGTERM)
    except (subprocess.CalledProcessError, FileNotFoundError):
        pass


@pytest.fixture(scope="session")
def server():
    """启动测试服务器，session 结束后关闭"""
    _kill_port(PORT)
    time.sleep(0.5)

    proc = subprocess.Popen(
        [sys.executable, os.path.join(TOOL_DIR, "server.py"), str(PORT)],
        cwd=TOOL_DIR,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    # 等服务器就绪
    for _ in range(30):
        try:
            urllib.request.urlopen(f"{BASE_URL}/api/health", timeout=1)
            break
        except Exception:
            time.sleep(0.3)
    else:
        proc.kill()
        raise RuntimeError("Server failed to start")

    yield BASE_URL

    proc.terminate()
    proc.wait(timeout=5)
    _kill_port(PORT)


@pytest.fixture(scope="session")
def browser_context_args():
    return {"viewport": {"width": 1280, "height": 900}}
