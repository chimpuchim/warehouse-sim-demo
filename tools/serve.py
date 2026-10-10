"""Static server for the WebGL build. Serves Build/WebGL on 127.0.0.1:8790 with no caching."""
import os
import sys
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Build", "WebGL")
PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8790


class Handler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, fmt, *args):
        sys.stderr.write("%s %s\n" % (self.address_string(), fmt % args))


os.chdir(ROOT)
print("Serving", ROOT, "on http://127.0.0.1:%d" % PORT, flush=True)
ThreadingHTTPServer(("127.0.0.1", PORT), partial(Handler, directory=ROOT)).serve_forever()
