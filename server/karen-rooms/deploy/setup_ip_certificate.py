"""Provision the room origin certificate after installing the IP HTTP-01 location."""
from pathlib import Path
import subprocess
import urllib.request

ROOT = Path("/opt/karen-rooms")
WEBROOT = Path("/var/lib/karen-rooms-acme")
IP = "47.95.213.214"

def run(*args):
    subprocess.run(args, check=True)

challenge = WEBROOT / ".well-known/acme-challenge/karen-ip-check"
challenge.parent.mkdir(parents=True, exist_ok=True)
challenge.write_text("karen-ip-acme-ok\n", encoding="utf-8")
with urllib.request.urlopen(f"http://{IP}/.well-known/acme-challenge/karen-ip-check", timeout=10) as reply:
    assert reply.read() == b"karen-ip-acme-ok\n", "Public IP HTTP-01 path is unavailable"
if not (Path("/opt/certbot/bin/python")).exists():
    run("python3", "-m", "venv", "/opt/certbot")
run(str(Path("/opt/certbot/bin/pip")), "install", "certbot==5.8.0", "certbot-nginx==5.8.0")
command = [str(Path("/opt/certbot/bin/certbot")), "certonly", "--non-interactive", "--agree-tos",
    "--register-unsafely-without-email", "--required-profile", "shortlived", "--webroot", "-w", str(WEBROOT),
    "--ip-address", IP, "--cert-name", "karen-rooms-ip", "--config-dir", "/etc/letsencrypt-karen-rooms",
    "--work-dir", "/var/lib/letsencrypt-karen-rooms", "--logs-dir", "/var/log/letsencrypt-karen-rooms",
    "--deploy-hook", "nginx -t && systemctl reload nginx"]
run(*command)
for name in ["karen-rooms-certbot.service", "karen-rooms-certbot.timer"]:
    Path("/etc/systemd/system", name).write_bytes(Path(__file__).with_name(name).read_bytes())
run("systemctl", "daemon-reload")
run("systemctl", "enable", "--now", "karen-rooms-certbot.timer")
challenge.unlink()
