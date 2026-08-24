"""
Smoke test for the Voxel Game using the lightweight TestServer.

Prerequisites:
  1. TestServer component is in the scene
     (Unity Editor -> Tools -> Add Test Server to Scene, then Ctrl+S).
  2. The game is running in Unity Editor (hit Play).

Run:
  python tests/alt_smoke_test.py
"""

import json
import logging
import os
import socket
import sys
import time
from pathlib import Path

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

HOST = "127.0.0.1"
PORT = 13000
TIMEOUT = 10
SCREENSHOT_DIR = Path(__file__).parent / "screenshots"
SCREENSHOT_PATH = SCREENSHOT_DIR / "smoke_test.png"


class GameClient:
    """Simple TCP client for the Unity TestServer."""

    def __init__(self, host: str = HOST, port: int = PORT, timeout: float = TIMEOUT):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.settimeout(timeout)
        self.sock.connect((host, port))
        self._buf = b""
        log.info("Connected to TestServer at %s:%d", host, port)

    def send(self, command: str) -> dict:
        self.sock.sendall((command.strip() + "\n").encode("utf-8"))
        return self._recv_json()

    def _recv_json(self) -> dict:
        while True:
            if b"\n" in self._buf:
                line, self._buf = self._buf.split(b"\n", 1)
                text = line.decode("utf-8")
                log.debug("RAW RECV: %r", text)
                return json.loads(text)
            chunk = self.sock.recv(4096)
            if not chunk:
                raise ConnectionError("Server closed connection")
            self._buf += chunk

    def close(self):
        try:
            self.sock.close()
        except OSError:
            pass


def run_smoke_test() -> bool:
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)

    log.info("Connecting to game on %s:%d ...", HOST, PORT)
    client = GameClient()
    passed = False

    try:
        # 1. Ping
        r = client.send("PING")
        assert r.get("pong") is True, f"Unexpected PING response: {r}"
        log.info("PING OK")

        # 2. Check active scene
        r = client.send("SCENE")
        log.info("Active scene: %s", r.get("scene"))

        # 3. Find the Player object
        log.info("Looking for 'Player' ...")
        r = client.send("FIND Player")
        assert r.get("found") is True, f"Player not found: {r}"
        assert r.get("enabled") is True, f"Player not enabled: {r}"
        log.info("Found Player at (%.1f, %.1f, %.1f), enabled=%s",
                 r["x"], r["y"], r["z"], r["enabled"])

        # 4. Find the Crosshair UI element
        log.info("Looking for 'Crosshair' ...")
        r = client.send("FIND Crosshair")
        assert r.get("found") is True, f"Crosshair not found: {r}"
        log.info("Found Crosshair, enabled=%s", r.get("enabled"))

        # 5. Take a screenshot
        screenshot_rel = str(SCREENSHOT_PATH).replace("\\", "/")
        log.info("Taking screenshot -> %s", screenshot_rel)
        r = client.send(f"SCREENSHOT {screenshot_rel}")

        if r.get("saved"):
            log.info("Screenshot saved: %s (%d bytes)", r["path"], r["size"])
        else:
            # ScreenCapture may use a relative path from the project root
            time.sleep(2)
            if SCREENSHOT_PATH.exists():
                log.info("Screenshot found at %s (%d bytes)",
                         SCREENSHOT_PATH, SCREENSHOT_PATH.stat().st_size)
            else:
                log.warning("Screenshot may not have saved: %s", r)

        passed = True

    except Exception as exc:
        log.error("SMOKE TEST FAILED: %s", exc)
        raise

    finally:
        client.close()
        if passed:
            log.info("=== SMOKE TEST PASSED ===")
        else:
            log.error("=== SMOKE TEST FAILED ===")

    return passed


if __name__ == "__main__":
    success = run_smoke_test()
    sys.exit(0 if success else 1)
