"""Configure trusted Cloudflare IPs; lock origin HTTPS only after all VIPCloud records are proxied."""
import argparse
import ipaddress
import json
import re
from pathlib import Path
import subprocess
from urllib.request import Request, urlopen

HOSTS = ("karen-multiplayer-relay-c7a91e5d.17181718.xyz",
         "karen-run-observatory-b4d82f6a.17181718.xyz",
         "panstack-private-downloads-7f2c9a61.17181718.xyz")
CONFIG = Path("/etc/nginx/conf.d/00-karen-cloudflare.conf")
STATE = Path("/var/lib/karen-cloudflare/networks.json")


def run(*args):
    subprocess.run(args, check=True)


def configure(lock_https=False):
    networks = []
    with urlopen("https://api.cloudflare.com/client/v4/ips", timeout=20) as response:
        official = json.load(response)
    if not official.get("success"):
        raise SystemExit("Cloudflare IP API unavailable; existing policy retained")
    for version in (4, 6):
        entries = [ipaddress.ip_network(line) for line in official["result"][f"ipv{version}_cidrs"]]
        if len(entries) < 5 or any(n.version != version or n.prefixlen == 0 for n in entries):
            raise SystemExit("Unexpected Cloudflare IP list; existing policy retained")
        networks += entries
    config = "# Managed by Karen Cloudflare origin protection; official Cloudflare IP lists only.\n"
    config += "".join(f"set_real_ip_from {network};\n" for network in networks)
    config += "real_ip_header CF-Connecting-IP;\nreal_ip_recursive on;\n"
    # CF supplies the verified legacy gateway IP. Only that gateway can forward a client identity.
    config += '''map "$remote_addr:$http_x_karen_migration_ip" $karen_client_ip {
    default $remote_addr;
    "~^47\\.95\\.213\\.214:(?<karen_legacy_client_ip>[0-9a-fA-F:.]{3,45})$" $karen_legacy_client_ip;
}
'''
    previous = CONFIG.read_bytes() if CONFIG.exists() else None
    if previous and "Managed by Karen Cloudflare origin protection" not in previous.decode():
        raise SystemExit("Refusing to replace unrelated nginx configuration")
    try:
        CONFIG.write_text(config, encoding="utf-8")
        run("nginx", "-t")
        run("systemctl", "reload", "nginx")
    except Exception:
        if previous is None:
            CONFIG.unlink(missing_ok=True)
        else:
            CONFIG.write_bytes(previous)
        raise
    if not lock_https:
        print(f"Trusted {len(networks)} Cloudflare networks; firewall unchanged")
        return
    for host in HOSTS:
        request = Request(f"https://cloudflare-dns.com/dns-query?name={host}&type=A", headers={"Accept": "application/dns-json"})
        with urlopen(request, timeout=15) as response:
            addresses = [ipaddress.ip_address(r["data"]) for r in json.load(response).get("Answer", []) if r["type"] == 1]
        if not addresses or any(not any(ip in network for network in networks if ip.version == network.version) for ip in addresses):
            raise SystemExit(f"Enable Cloudflare proxy for {host} before locking HTTPS")
    status = subprocess.check_output(["ufw", "status", "verbose"], text=True)
    if "Status: active" not in status or "deny (incoming)" not in status:
        raise SystemExit("Active default-deny UFW is required; firewall unchanged")
    old = json.loads(STATE.read_text()) if STATE.exists() else []
    for network in networks:
        run("ufw", "allow", "from", str(network), "to", "any", "port", "443", "proto", "tcp", "comment", "Cloudflare HTTPS origin")
    # Add the complete allowlist before removing the old broad rule. SSH and HTTP/ACME stay intact.
    if any(re.match(r"^443/tcp(?: \(v6\))?\s+ALLOW(?: IN)?\s+Anywhere", line.strip()) for line in status.splitlines()):
        run("ufw", "--force", "delete", "allow", "443/tcp")
    current = [str(n) for n in networks]
    for network in set(old) - set(current):
        ipaddress.ip_network(network)
        run("ufw", "--force", "delete", "allow", "from", network, "to", "any", "port", "443", "proto", "tcp")
    STATE.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    STATE.write_text(json.dumps(current), encoding="utf-8")
    service = Path("/etc/systemd/system/karen-cloudflare-refresh.service")
    timer = Path("/etc/systemd/system/karen-cloudflare-refresh.timer")
    service.write_text("""[Unit]
Description=Refresh official Cloudflare origin allowlist
After=network-online.target
Wants=network-online.target
[Service]
Type=oneshot
ExecStart=/usr/bin/python3 /opt/karen-rooms/current/deploy/protect_cloudflare_origin.py --lock-https
TimeoutStartSec=180
UMask=0077
""", encoding="utf-8")
    timer.write_text("""[Unit]
Description=Weekly Cloudflare origin IP maintenance
[Timer]
OnCalendar=weekly
RandomizedDelaySec=1h
Persistent=true
[Install]
WantedBy=timers.target
""", encoding="utf-8")
    run("systemctl", "daemon-reload")
    run("systemctl", "enable", "--now", "karen-cloudflare-refresh.timer")
    print(f"HTTPS allows only {len(networks)} Cloudflare networks; HTTP/ACME and SSH retained")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--lock-https", action="store_true")
    configure(parser.parse_args().lock_https)
