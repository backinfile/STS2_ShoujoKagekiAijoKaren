"""Run over the authorized SSH connection after uploading a release archive."""
import hashlib
import json
import os
import pwd
import re
import secrets
from pathlib import Path
import socket
import subprocess
import sys
import tarfile
import time
import urllib.request

ROOT = Path("/opt/karen-rooms")
ARCHIVE = Path(sys.argv[1]).resolve()
EXPECTED = sys.argv[2]
assert ARCHIVE.parent == ROOT / "incoming"
assert hashlib.sha256(ARCHIVE.read_bytes()).hexdigest() == EXPECTED
RELEASE = ROOT / "releases" / EXPECTED[:16]
ALLOWED = {"server.py", "security.py", "monitor.py", "requirements.txt", "web/index.html", "web/app.js", "web/style.css",
           "deploy/karen-rooms.service", "deploy/nginx.conf", "deploy/logrotate.conf",
           "deploy/karen-rooms-monitor.service", "deploy/karen-rooms-monitor.timer",
           "deploy/protect_cloudflare_origin.py"}
SERVICE = Path("/etc/systemd/system/karen-rooms.service")
NGINX = Path("/etc/nginx/conf.d/karen-rooms.conf")
CURRENT = ROOT / "current"


def run(*args):
    subprocess.run(args, check=True)


def available(port):
    with socket.socket() as probe:
        return probe.connect_ex(("127.0.0.1", port)) != 0


if not Path("/etc/letsencrypt/live/karen-services/fullchain.pem").exists():
    raise SystemExit("Run setup_domain_certificate.py before installing the TLS relay.")

existing = subprocess.run(["systemctl", "is-active", "--quiet", "karen-rooms"]).returncode == 0
if not existing and not available(42766):
    raise SystemExit("Chosen ports are occupied; refusing deployment.")
if NGINX.exists() and not any(marker in NGINX.read_text() for marker in ("karen_rooms_connections", "karen_direct_connections")):
    raise SystemExit("Refusing to overwrite an unrelated nginx configuration.")
RELEASE.mkdir(parents=True, exist_ok=True)
with tarfile.open(ARCHIVE) as archive:
    if {item.name for item in archive} != ALLOWED:
        raise SystemExit("Unexpected release archive content")
    for item in archive:
        if not item.isfile() or item.name not in ALLOWED:
            raise SystemExit("Unsafe archive member")
        target = RELEASE / item.name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(archive.extractfile(item).read())
        target.chmod(0o644)
if not (ROOT / "venv/bin/python").exists():
    result = subprocess.run(["python3", "-m", "venv", str(ROOT / "venv")])
    if result.returncode:
        run("apt-get", "update", "-qq")
        run("apt-get", "install", "-y", "python3-venv")
        run("python3", "-m", "venv", str(ROOT / "venv"))
run(str(ROOT / "venv/bin/python"), "-m", "pip", "install", "--disable-pip-version-check",
    "-r", str(RELEASE / "requirements.txt"))
try:
    pwd.getpwnam("karen-roomwatch")
except KeyError:
    run("useradd", "--system", "--no-create-home", "--shell", "/usr/sbin/nologin", "karen-roomwatch")
status_file = ROOT / "status-path"
if status_file.exists():
    status_path = status_file.read_text().strip()
else:
    status_path = "karen-observatory-" + secrets.token_hex(24)
    status_file.write_text(status_path + "\n")
    status_file.chmod(0o600)
if not re.fullmatch(r"karen-observatory-[0-9a-f]{48}", status_path):
    raise SystemExit("Unexpected status path format")
previous_link = os.readlink(CURRENT) if CURRENT.is_symlink() else None
previous_nginx = NGINX.read_bytes() if NGINX.exists() else None
previous_service = SERVICE.read_bytes() if SERVICE.exists() else None
extras = {Path("/etc/systemd/system/karen-rooms-monitor.service"): "deploy/karen-rooms-monitor.service",
          Path("/etc/systemd/system/karen-rooms-monitor.timer"): "deploy/karen-rooms-monitor.timer",
          Path("/etc/logrotate.d/karen-rooms"): "deploy/logrotate.conf"}
previous_extras = {path: path.read_bytes() if path.exists() else None for path in extras}
monitor_enabled = subprocess.run(["systemctl", "is-enabled", "--quiet", "karen-rooms-monitor.timer"]).returncode == 0
monitor_active = subprocess.run(["systemctl", "is-active", "--quiet", "karen-rooms-monitor.timer"]).returncode == 0

def set_current(target):
    temporary = ROOT / "current.next"
    temporary.unlink(missing_ok=True)
    temporary.symlink_to(target)
    temporary.replace(CURRENT)
if CURRENT.exists() and not CURRENT.is_symlink():
    raise SystemExit("Refusing to replace a non-symlink current directory.")
try:
    set_current(RELEASE)
    logs = Path("/var/log/karen-rooms")
    logs.mkdir(mode=0o755, exist_ok=True)
    for name in ("karen-services.d", "karen-services-http.d"):
        Path("/etc/nginx", name).mkdir(mode=0o755, exist_ok=True)
    for path, source in extras.items(): path.write_bytes((RELEASE / source).read_bytes())
    SERVICE.write_bytes((RELEASE / "deploy/karen-rooms.service").read_bytes())
    NGINX.write_text((RELEASE / "deploy/nginx.conf").read_text().replace("__STATUS_PATH__", status_path))
    run("nginx", "-t")
    run("systemctl", "daemon-reload")
    run("systemctl", "enable", "karen-rooms")
    run("systemctl", "restart", "karen-rooms")
    for attempt in range(30):
        try:
            with urllib.request.urlopen("http://127.0.0.1:42766/healthz", timeout=2) as response:
                health = json.load(response)
            assert health["service"] == "karen-rooms" and health["protocol"] == 1
            break
        except Exception:
            time.sleep(.3)
    else:
        raise RuntimeError("New relay did not become ready")
    run("systemctl", "reload", "nginx")
    run("systemctl", "start", "karen-rooms-monitor.service")
    run("systemctl", "enable", "--now", "karen-rooms-monitor.timer")
    run("logrotate", "--debug", "/etc/logrotate.d/karen-rooms")
    print(json.dumps({"release": EXPECTED[:16], "health": health, "public_port": 443,
                      "backend": "127.0.0.1:42766", "status_url": "https://karen-multiplayer-relay-c7a91e5d.17181718.xyz/" + status_path + "/"}))
except Exception:
    subprocess.run(["systemctl", "stop", "karen-rooms-monitor.timer", "karen-rooms-monitor.service"])
    if not monitor_enabled: subprocess.run(["systemctl", "disable", "karen-rooms-monitor.timer"])
    if previous_link:
        set_current(previous_link)
    else:
        CURRENT.unlink(missing_ok=True)
    for path, previous in [(NGINX, previous_nginx), (SERVICE, previous_service), *previous_extras.items()]:
        if previous is None:
            path.unlink(missing_ok=True)
        else:
            path.write_bytes(previous)
    if not existing:
        subprocess.run(["systemctl", "stop", "karen-rooms"])
        subprocess.run(["systemctl", "disable", "karen-rooms"])
    run("systemctl", "daemon-reload")
    if existing:
        subprocess.run(["systemctl", "restart", "karen-rooms"])
    subprocess.run(["nginx", "-t"], check=True)
    subprocess.run(["systemctl", "reload", "nginx"])
    if monitor_active: subprocess.run(["systemctl", "start", "karen-rooms-monitor.timer"])
    raise
