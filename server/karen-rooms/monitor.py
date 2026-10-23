"""One read-only local health observation; run once a minute with systemd."""
import argparse
import asyncio
import json
import os
from pathlib import Path
import shutil
import ssl
import time
import urllib.request
from websockets.asyncio.client import connect


def assess(status, previous, certificate_seconds, disk_free):
    alerts = []
    if certificate_seconds < 48 * 3600: alerts.append("certificate_expiring")
    if disk_free < 1024 ** 3: alerts.append("disk_low")
    if status["connections"] >= status["max_connections"] * .8: alerts.append("connection_capacity")
    if status["rooms"] >= status["max_rooms"]: alerts.append("room_capacity")
    if (status.get("rss_bytes") or 0) > 256 * 1024 ** 2: alerts.append("memory_high")
    if (status.get("open_fds") or 0) > 1600: alerts.append("fds_high")
    if status.get("loop_lag_seconds", 0) > .5: alerts.append("event_loop_lag")
    counters = status.get("counters", {})
    old = previous.get("counters", {}) if previous else {}
    if counters.get("internal_errors_total", 0) > old.get("internal_errors_total", 0): alerts.append("relay_errors")
    rejects = sum(v for k, v in counters.items() if k.startswith("rejected_"))
    old_rejects = sum(v for k, v in old.items() if k.startswith("rejected_"))
    if rejects - old_rejects >= 50: alerts.append("abuse_spike")
    return alerts


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".tmp")
    tmp.write_text(json.dumps(value, sort_keys=True) + "\n", encoding="utf-8")
    tmp.replace(path)


async def observe(endpoint, status_url, state_path, summary_path):
    report = {"ok": False, "checked_at": time.time(), "alerts": []}
    previous = {}
    try: previous = json.loads(state_path.read_text()).get("status", {})
    except (OSError, ValueError, TypeError): pass
    try:
        with urllib.request.urlopen(status_url, timeout=3) as response: status = json.load(response)
        if status.get("service") != "karen-rooms": raise ValueError("unexpected service")
        report["status"] = status
    except Exception as error:
        report["alerts"].append("status_unavailable")
        report["status_error"] = type(error).__name__
    try:
        async with asyncio.timeout(8):
            async with connect(endpoint, subprotocols=["karen-rooms-v1"], compression=None, proxy=None,
                               open_timeout=5, close_timeout=1) as socket:
                await socket.send('{"op":"probe","protocol":1}')
                ready = json.loads(await socket.recv())
                if ready.get("op") != "ready" or ready.get("protocol") != 1: raise ValueError("protocol mismatch")
                certificate = socket.transport.get_extra_info("ssl_object").getpeercert()
                report["certificate_seconds"] = int(ssl.cert_time_to_seconds(certificate["notAfter"]) - time.time())
    except Exception as error:
        report["alerts"].append("tls_websocket_unavailable")
        report["probe_error"] = type(error).__name__
    if "status" in report and "certificate_seconds" in report:
        report["alerts"] += assess(report["status"], previous, report["certificate_seconds"], shutil.disk_usage("/").free)
    report["ok"] = not report["alerts"]
    write_json(state_path, report)
    write_json(summary_path, {k: v for k, v in report.items() if k in ("ok", "checked_at", "certificate_seconds")})
    print(json.dumps({k: v for k, v in report.items() if k != "status"}), flush=True)
    return 0 if report["ok"] else 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--endpoint", default="wss://47.95.213.214:8443/rooms")
    parser.add_argument("--status-url", default="http://127.0.0.1:42766/status")
    parser.add_argument("--state", type=Path, default=Path(os.getenv("STATE_DIRECTORY", "/var/lib/karen-roomwatch")) / "health.json")
    parser.add_argument("--summary", type=Path, default=Path(os.getenv("RUNTIME_DIRECTORY", "/run/karen-rooms-health")) / "summary.json")
    args = parser.parse_args()
    raise SystemExit(asyncio.run(observe(args.endpoint, args.status_url, args.state, args.summary)))
