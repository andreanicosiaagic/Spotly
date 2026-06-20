import os
import socket
import subprocess
import sys
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BACKEND_DIR = ROOT.parent / "backend"
FRONTEND_DIR = ROOT
BACKEND_PORT = 55205
FRONTEND_PORT = 55173
BASE_URL = f"http://127.0.0.1:{FRONTEND_PORT}"
NPM_COMMAND = "npm.cmd" if os.name == "nt" else "npm"


def wait_for_port(port: int, timeout_seconds: int = 45) -> None:
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.settimeout(1)
            if sock.connect_ex(("127.0.0.1", port)) == 0:
                return
        time.sleep(0.5)
    raise TimeoutError(f"Port {port} not ready within {timeout_seconds}s")


def terminate(process: subprocess.Popen[str] | None) -> None:
    if process is None or process.poll() is not None:
        return
    process.terminate()
    try:
        process.wait(timeout=15)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=5)


def main() -> int:
    backend_env = {
        **os.environ,
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": f"http://127.0.0.1:{BACKEND_PORT}",
    }
    frontend_env = {
        **os.environ,
        "VITE_USE_MSW": "false",
        "VITE_BACKEND_PROXY_TARGET": f"http://127.0.0.1:{BACKEND_PORT}",
    }
    test_env = {
        **os.environ,
        "SPOTLY_E2E_BASE_URL": BASE_URL,
    }

    backend_process: subprocess.Popen[str] | None = None
    frontend_process: subprocess.Popen[str] | None = None

    try:
        backend_process = subprocess.Popen(
            ["dotnet", "run", "--no-launch-profile", "--project", "Spotly.Api"],
            cwd=BACKEND_DIR,
            env=backend_env,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.STDOUT,
            text=True,
        )
        wait_for_port(BACKEND_PORT)

        frontend_process = subprocess.Popen(
            [NPM_COMMAND, "run", "dev", "--", "--host", "127.0.0.1", "--port", str(FRONTEND_PORT)],
            cwd=FRONTEND_DIR,
            env=frontend_env,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.STDOUT,
            text=True,
        )
        wait_for_port(FRONTEND_PORT)

        completed = subprocess.run(
            [sys.executable, "e2e/integrated_smoke.py"],
            cwd=FRONTEND_DIR,
            env=test_env,
            check=False,
        )
        return completed.returncode
    finally:
        terminate(frontend_process)
        terminate(backend_process)


if __name__ == "__main__":
    raise SystemExit(main())
