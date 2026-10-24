"""Run as root on vipcloud after the two A records resolve here."""
from pathlib import Path
import os
import subprocess
import time

DOMAINS = ("karen-multiplayer-relay-c7a91e5d.17181718.xyz",
           "karen-run-observatory-b4d82f6a.17181718.xyz")
CONFIG = Path("/etc/nginx/conf.d/karen-services-acme.conf")
CERT_NAME = "karen-services"


def run(*args):
    subprocess.run(args, check=True)


def configure_https():
    if os.geteuid() != 0:
        raise SystemExit("Run as root")
    run("python3", str(Path(__file__).with_name("protect_cloudflare_origin.py")))
    # Back up only the file this script owns; all other sites stay in place.
    previous = CONFIG.read_bytes() if CONFIG.exists() else None
    if previous and "# Karen domain HTTP/ACME entry" not in previous.decode():
        raise SystemExit("Refusing to overwrite an unrelated nginx configuration")
    if previous:
        backup = Path("/var/backups/karen-services")
        backup.mkdir(mode=0o700, parents=True, exist_ok=True)
        (backup / f"acme-{time.time_ns()}.conf").write_bytes(previous)
    config = "# Karen domain HTTP/ACME entry\nserver {\n    listen 80;\n    listen [::]:80;\n    server_name " + " ".join(DOMAINS) + ";\n    location ^~ /.well-known/acme-challenge/ { return 404; }\n    location / { return 404; }\n}\n"
    try:
        CONFIG.write_text(config, encoding="utf-8")
        run("nginx", "-t")
        run("systemctl", "reload", "nginx")
        hook = Path("/etc/letsencrypt/renewal-hooks/deploy/karen-nginx-reload")
        hook.parent.mkdir(parents=True, exist_ok=True)
        hook.write_text("#!/bin/sh\nset -eu\n/usr/sbin/nginx -t\n/bin/systemctl reload nginx\n", encoding="utf-8")
        hook.chmod(0o755)
        command = ["certbot", "certonly", "--nginx", "--non-interactive", "--agree-tos", "--cert-name", CERT_NAME]
        for domain in DOMAINS:
            command += ["-d", domain]
        run(*command)
        CONFIG.write_text(config.replace("location / { return 404; }", "location / { return 308 https://$host$request_uri; }"), encoding="utf-8")
        run("nginx", "-t")
        run("systemctl", "reload", "nginx")
        run("systemctl", "enable", "--now", "certbot.timer")
    except Exception:
        if previous is None:
            CONFIG.unlink(missing_ok=True)
        else:
            CONFIG.write_bytes(previous)
        run("nginx", "-t")
        run("systemctl", "reload", "nginx")
        raise


if __name__ == "__main__":
    configure_https()
