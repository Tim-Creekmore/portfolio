"""
Voxel Game Diagnostic Tool
===========================
Connects to the in-game TestServer (TCP port 13000) and generates a
diagnostic report you can paste into a chat with your AI assistant.

Usage:
  1. Hit Play in Unity (TestServer must be in the scene).
  2. Run:  python tests/diagnose.py
  3. Copy the output and paste it into your AI chat.

Modes:
  python tests/diagnose.py              Full diagnostic report
  python tests/diagnose.py --trees      Tree-focused diagnostics
  python tests/diagnose.py --terrain    Terrain height sampling
  python tests/diagnose.py --logs       Recent Unity log output
  python tests/diagnose.py --interactive  Live REPL to send commands
  python tests/diagnose.py --screenshot   Take a screenshot
"""

import argparse
import json
import logging
import socket
import sys
import time
from pathlib import Path

import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

logging.basicConfig(level=logging.INFO, format="%(message)s")
log = logging.getLogger(__name__)

HOST = "127.0.0.1"
PORT = 13000
TIMEOUT = 10
SCREENSHOT_DIR = Path(__file__).parent / "screenshots"


class GameClient:
    def __init__(self, host=HOST, port=PORT, timeout=TIMEOUT):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.settimeout(timeout)
        self.sock.connect((host, port))
        self._buf = b""

    def send(self, command: str) -> dict:
        self.sock.sendall((command.strip() + "\n").encode("utf-8"))
        return self._recv_json()

    def _recv_json(self) -> dict:
        while True:
            if b"\n" in self._buf:
                line, self._buf = self._buf.split(b"\n", 1)
                return json.loads(line.decode("utf-8"))
            chunk = self.sock.recv(8192)
            if not chunk:
                raise ConnectionError("Server closed connection")
            self._buf += chunk

    def close(self):
        try:
            self.sock.close()
        except OSError:
            pass


def connect():
    try:
        return GameClient()
    except (ConnectionRefusedError, socket.timeout):
        log.error("Cannot connect to game on %s:%d", HOST, PORT)
        log.error("Make sure Unity is in Play mode with TestServer in the scene.")
        sys.exit(1)


# ── Report: Full ────────────────────────────────────────────────────────

def report_full(client: GameClient):
    print("=" * 60)
    print("  VOXEL GAME DIAGNOSTIC REPORT")
    print("=" * 60)
    print()

    report_stats(client)
    print()
    report_scene_overview(client)
    print()
    report_trees_summary(client)
    print()
    report_terrain_sample(client)
    print()
    report_errors(client)
    print()
    print("=" * 60)
    print("  END OF REPORT")
    print("=" * 60)


# ── Report: Stats ───────────────────────────────────────────────────────

def report_stats(client: GameClient):
    stats = client.send("STATS")
    print("── Performance & Camera ──")
    print(f"  FPS:              {stats.get('fps', '?')}")
    print(f"  Total GameObjects: {stats.get('totalGameObjects', '?')}")
    print(f"  Mesh Renderers:    {stats.get('meshRenderers', '?')}")
    print(f"  Memory (total):    {stats.get('totalMemoryMB', '?')} MB")
    print(f"  Memory (GPU):      {stats.get('gpuMemoryMB', '?')} MB")
    cam_pos = stats.get("cameraPosition")
    cam_rot = stats.get("cameraRotation")
    if cam_pos and cam_pos != "null":
        print(f"  Camera position:   ({cam_pos[0]}, {cam_pos[1]}, {cam_pos[2]})")
        print(f"  Camera rotation:   ({cam_rot[0]}, {cam_rot[1]}, {cam_rot[2]})")


# ── Report: Scene Overview ──────────────────────────────────────────────

def report_scene_overview(client: GameClient):
    scene = client.send("SCENE")
    print("── Scene Overview ──")
    print(f"  Scene name:    {scene.get('scene', '?')}")
    print(f"  Root objects:  {scene.get('rootObjects', '?')}")
    print(f"  Total objects: {scene.get('totalObjects', '?')}")

    hierarchy = client.send("HIERARCHY 2")
    roots = hierarchy.get("roots", [])
    if roots:
        print()
        print("  Hierarchy (depth 2):")
        for root in roots:
            print_tree(root, indent=4)


def print_tree(node, indent=0):
    prefix = " " * indent
    name = node.get("name", "?")
    extras = []
    if "verts" in node:
        extras.append(f"{node['verts']} verts")
    if "material" in node:
        extras.append(f"mat={node['material']}")
    children_count = node.get("childCount", 0)
    if children_count > 0 and "children" not in node:
        extras.append(f"{children_count} children")

    suffix = f"  ({', '.join(extras)})" if extras else ""
    active = "" if node.get("active", True) else " [INACTIVE]"
    print(f"{prefix}- {name}{active}{suffix}")

    for child in node.get("children", []):
        print_tree(child, indent + 2)


# ── Report: Trees ───────────────────────────────────────────────────────

def report_trees_summary(client: GameClient):
    print("── Trees ──")
    result = client.send("COUNT Tree_*")
    total = result.get("total", 0)
    matches = result.get("matches", [])
    print(f"  Total trees found: {total}")

    if not matches:
        print("  (no trees in scene)")
        return

    for tree in matches[:20]:
        name = tree.get("name", "?")
        pos = tree.get("pos", [0, 0, 0])
        print(f"  {name} at ({pos[0]:.1f}, {pos[1]:.1f}, {pos[2]:.1f})")

        mesh_data = client.send(f"MESHINFO {name}")
        meshes = mesh_data.get("meshes", [])
        for m in meshes:
            go_name = m.get("gameObject", "?")
            verts = m.get("vertices", 0)
            tris = m.get("triangles", 0)
            bounds = m.get("boundsSize", [0, 0, 0])
            mat = m.get("material", "?")
            shader = m.get("shader", "?")
            print(f"    └─ {go_name}: {verts} verts, {tris} tris, "
                  f"bounds=({bounds[0]:.1f}×{bounds[1]:.1f}×{bounds[2]:.1f}), "
                  f"shader={shader}")


def report_trees_detailed(client: GameClient):
    print("=" * 60)
    print("  TREE DIAGNOSTICS")
    print("=" * 60)
    print()
    report_trees_summary(client)

    result = client.send("COUNT Tree_*")
    matches = result.get("matches", [])

    if matches:
        print()
        print("── Detailed Inspection (first 5 trees) ──")
        for tree in matches[:5]:
            name = tree.get("name", "?")
            print()
            print(f"  [{name}]")
            data = client.send(f"INSPECT {name}")
            pos = data.get("position", [0, 0, 0])
            scl = data.get("scale", [1, 1, 1])
            rot = data.get("rotation", [0, 0, 0])
            print(f"    position: ({pos[0]:.2f}, {pos[1]:.2f}, {pos[2]:.2f})")
            print(f"    scale:    ({scl[0]:.2f}, {scl[1]:.2f}, {scl[2]:.2f})")
            print(f"    rotation: ({rot[0]:.1f}, {rot[1]:.1f}, {rot[2]:.1f})")

            children_data = client.send(f"CHILDREN {name}")
            for child in children_data.get("children", []):
                cname = child.get("name", "?")
                inspect = client.send(f"INSPECT {name}/{cname}")
                mesh = inspect.get("mesh")
                renderer = inspect.get("renderer")
                if mesh:
                    print(f"    {cname}: {mesh['vertices']} verts, {mesh['triangles']} tris")
                    print(f"      bounds: {mesh.get('boundsSize', '?')}")
                    print(f"      colors: {mesh.get('hasColors', '?')}")
                if renderer:
                    print(f"      material: {renderer.get('material', '?')}")
                    print(f"      shader: {renderer.get('shader', '?')}")
                    print(f"      shadows: {renderer.get('shadowCasting', '?')}")


# ── Report: Terrain ─────────────────────────────────────────────────────

def report_terrain_sample(client: GameClient):
    print("── Terrain Samples ──")
    print("  Sampling height and biome across the map:")
    print(f"  {'X':>6} {'Z':>6} {'Height':>8} {'Biome':<10} {'Pond?'}")
    print(f"  {'─'*6} {'─'*6} {'─'*8} {'─'*10} {'─'*5}")

    sample_points = [
        (10, 10), (60, 10), (110, 10),
        (10, 60), (60, 60), (110, 60),
        (10, 100), (60, 100), (110, 100),
        (20, 20), (50, 22), (100, 20),
        (20, 60), (60, 55), (100, 60),
        (20, 100), (60, 108), (100, 100),
        (30, 78), (50, 45),
    ]

    for x, z in sample_points:
        data = client.send(f"TERRAIN {x} {z}")
        h = float(data.get("height", 0))
        biome = data.get("biome", "?")
        pond = "YES" if data.get("isPond") else ""
        print(f"  {x:>6} {z:>6} {h:>8.2f} {biome:<10} {pond}")


def report_terrain_detailed(client: GameClient):
    print("=" * 60)
    print("  TERRAIN DIAGNOSTICS")
    print("=" * 60)
    print()
    print("  Terrain height grid (every 5 units):")
    print()

    step = 10
    header = "      "
    for x in range(0, 121, step):
        header += f"{x:>7}"
    print(header)
    print("      " + "─" * (7 * (120 // step + 1)))

    for z in range(120, -1, -step):
        row = f"z={z:>3} |"
        for x in range(0, 121, step):
            data = client.send(f"TERRAIN {x} {z}")
            h = float(data.get("height", 0))
            row += f" {h:>5.1f} "
        print(row)

    print()
    print("  Biome map:")
    print()

    header = "      "
    for x in range(0, 121, step):
        header += f"{x:>7}"
    print(header)
    print("      " + "─" * (7 * (120 // step + 1)))

    biome_short = {
        "Meadow": "MDOW", "Forest": "FRST", "Rocky": "ROCK", "Pond": "POND",
        "Farm": "FARM", "Beach": "BECH", "Cliff": "CLIF", "Thicket": "THKT",
        "Moor": "MOOR", "Village": "VILL", "Ruins": "RUIN", "Road": "ROAD",
        "River": "RIVR",
    }
    for z in range(120, -1, -step):
        row = f"z={z:>3} |"
        for x in range(0, 121, step):
            data = client.send(f"TERRAIN {x} {z}")
            biome = data.get("biome", "?")
            short = biome_short.get(biome, biome[:4])
            if data.get("isPond"):
                short = "~~~~"
            row += f" {short:>5} "
        print(row)


# ── Report: Logs ────────────────────────────────────────────────────────

def report_errors(client: GameClient):
    print("── Recent Errors & Warnings ──")
    data = client.send("LOG 100")
    entries = data.get("entries", [])

    errors = [e for e in entries if e.startswith("[ERROR]") or e.startswith("[WARN")]
    if not errors:
        print("  No errors or warnings in recent logs.")
    else:
        for e in errors[-20:]:
            print(f"  {e}")


def report_logs(client: GameClient):
    print("=" * 60)
    print("  UNITY LOG OUTPUT (last 100 entries)")
    print("=" * 60)
    data = client.send("LOG 100")
    entries = data.get("entries", [])
    if not entries:
        print("  (empty)")
    for e in entries:
        print(f"  {e}")


# ── Screenshot ──────────────────────────────────────────────────────────

def take_screenshot(client: GameClient):
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)
    ts = time.strftime("%Y%m%d_%H%M%S")
    filename = f"diag_{ts}.png"
    filepath = str(SCREENSHOT_DIR / filename).replace("\\", "/")

    print(f"Taking screenshot -> {filepath}")
    result = client.send(f"SCREENSHOT {filepath}")

    if result.get("saved"):
        print(f"Saved: {result['path']} ({result['size']} bytes)")
    else:
        time.sleep(2)
        p = SCREENSHOT_DIR / filename
        if p.exists():
            print(f"Saved: {p} ({p.stat().st_size} bytes)")
        else:
            print(f"Screenshot may not have saved: {result}")


# ── Interactive REPL ────────────────────────────────────────────────────

def interactive(client: GameClient):
    print("Connected to TestServer. Type HELP for commands, Ctrl+C to exit.")
    print()
    while True:
        try:
            cmd = input(">> ").strip()
            if not cmd:
                continue
            if cmd.lower() in ("exit", "quit"):
                break
            result = client.send(cmd)
            print(json.dumps(result, indent=2))
            print()
        except KeyboardInterrupt:
            print()
            break
        except Exception as e:
            print(f"Error: {e}")


# ── Main ────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Voxel Game Diagnostic Tool")
    parser.add_argument("--trees", action="store_true", help="Detailed tree diagnostics")
    parser.add_argument("--terrain", action="store_true", help="Terrain height/biome grid")
    parser.add_argument("--logs", action="store_true", help="Recent Unity log output")
    parser.add_argument("--screenshot", action="store_true", help="Take a screenshot")
    parser.add_argument("--interactive", "-i", action="store_true", help="Interactive REPL")
    args = parser.parse_args()

    client = connect()

    try:
        if args.interactive:
            interactive(client)
        elif args.trees:
            report_trees_detailed(client)
        elif args.terrain:
            report_terrain_detailed(client)
        elif args.logs:
            report_logs(client)
        elif args.screenshot:
            take_screenshot(client)
        else:
            report_full(client)
    finally:
        client.close()


if __name__ == "__main__":
    main()
