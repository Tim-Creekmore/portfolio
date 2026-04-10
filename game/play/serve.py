import http.server
import sys

class GodotHTTPHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    extensions_map = {
        **http.server.SimpleHTTPRequestHandler.extensions_map,
        ".wasm": "application/wasm",
        ".pck": "application/octet-stream",
    }

port = int(sys.argv[1]) if len(sys.argv) > 1 else 8060
print(f"Serving Godot export on http://localhost:{port}")
http.server.HTTPServer(("", port), GodotHTTPHandler).serve_forever()
