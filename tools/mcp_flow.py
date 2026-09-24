"""Run bounded, declarative STS2 MCP test flows against isolated game instances."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.request import ProxyHandler, Request, build_opener


ROOT = Path(__file__).resolve().parent.parent
MISSING = object()
ENDPOINTS = {"singleplayer", "multiplayer", "settings"}
OPS = {"assert", "wait", "post", "snapshot"}
CHECKS = {"eq", "contains", "gte", "exists"}


class FlowError(Exception):
    pass


def resolve_pointer(value: object, pointer: str) -> object:
    if pointer == "":
        return value
    if not pointer.startswith("/"):
        raise FlowError(f"JSON pointer must start with '/': {pointer!r}")
    for raw_part in pointer[1:].split("/"):
        part = raw_part.replace("~1", "/").replace("~0", "~")
        if isinstance(value, dict):
            value = value.get(part, MISSING)
        elif isinstance(value, list) and part.isdigit():
            index = int(part)
            value = value[index] if index < len(value) else MISSING
        else:
            return MISSING
    return value


def check_one(state: dict, check: dict) -> dict:
    actual = resolve_pointer(state, check["path"])
    operator = next(key for key in CHECKS if key in check)
    expected = check[operator]
    if operator == "exists":
        passed = (actual is not MISSING) == expected
    elif actual is MISSING:
        passed = False
    elif operator == "eq":
        passed = actual == expected and type(actual) is type(expected)
    elif operator == "contains":
        passed = ((isinstance(actual, str) and isinstance(expected, str) and expected in actual)
                  or (isinstance(actual, list) and expected in actual)
                  or (isinstance(actual, dict) and isinstance(expected, str) and expected in actual))
    else:
        passed = (isinstance(actual, (int, float)) and not isinstance(actual, bool)
                  and actual >= expected)
    return {"path": check["path"], "operator": operator, "expected": expected,
            "actual": None if actual is MISSING else actual, "missing": actual is MISSING,
            "passed": passed}


def check_all(state: dict, checks: list[dict]) -> list[dict]:
    return [check_one(state, check) for check in checks]


def validate_checks(checks: object, label: str) -> None:
    if not isinstance(checks, list):
        raise FlowError(f"{label} must be an array")
    for check in checks:
        if not isinstance(check, dict) or not isinstance(check.get("path"), str):
            raise FlowError(f"{label} entries need a JSON pointer 'path'")
        if check["path"] and not check["path"].startswith("/"):
            raise FlowError(f"Invalid JSON pointer in {label}: {check['path']!r}")
        operators = CHECKS.intersection(check)
        if len(operators) != 1:
            raise FlowError(f"{label} entries need exactly one of {sorted(CHECKS)}")
        operator = next(iter(operators))
        if operator == "exists" and not isinstance(check[operator], bool):
            raise FlowError(f"{label} 'exists' must be boolean")
        if operator == "gte" and (not isinstance(check[operator], (int, float))
                                  or isinstance(check[operator], bool)):
            raise FlowError(f"{label} 'gte' must be numeric")


def validate_flow(flow: object) -> dict:
    if not isinstance(flow, dict):
        raise FlowError("Flow must be a JSON object")
    targets = flow.get("targets")
    steps = flow.get("steps")
    if not isinstance(targets, dict) or not targets:
        raise FlowError("Flow needs at least one target")
    if not isinstance(steps, list) or not 1 <= len(steps) <= 200:
        raise FlowError("Flow needs 1 to 200 steps")
    for name, target in targets.items():
        if not isinstance(name, str) or not name or not isinstance(target, dict):
            raise FlowError("Each target needs a name and object definition")
        port = target.get("port")
        if type(port) is not int or not 1 <= port <= 65535:
            raise FlowError(f"Target {name!r} needs a valid port")
        if target.get("mode", "singleplayer") not in {"singleplayer", "multiplayer"}:
            raise FlowError(f"Target {name!r} has invalid mode")
    for index, step in enumerate(steps, 1):
        label = f"step {index}"
        if not isinstance(step, dict) or step.get("op") not in OPS:
            raise FlowError(f"{label} needs op: {sorted(OPS)}")
        if step.get("target") not in targets:
            raise FlowError(f"{label} refers to unknown target")
        if "endpoint" in step and step["endpoint"] not in ENDPOINTS:
            raise FlowError(f"{label} has invalid endpoint")
        for key in ("expect", "before", "after"):
            if key in step:
                validate_checks(step[key], f"{label}.{key}")
        if step["op"] in {"assert", "wait"} and not step.get("expect"):
            raise FlowError(f"{label} needs a nonempty expect array")
        if step["op"] == "post":
            body = step.get("body")
            if not isinstance(body, dict) or not body:
                raise FlowError(f"{label} needs a nonempty body")
            sources = step.get("body_from_state", {})
            if not isinstance(sources, dict) or any(
                not isinstance(field, str) or not field or not isinstance(pointer, str)
                or not pointer.startswith("/") for field, pointer in sources.items()
            ):
                raise FlowError(f"{label}.body_from_state must map fields to JSON pointers")
            if "action" in sources:
                raise FlowError(f"{label} cannot read action name from game state")
            endpoint = step.get("endpoint", targets[step["target"]].get("mode", "singleplayer"))
            if endpoint != "settings" and not isinstance(body.get("action"), str):
                raise FlowError(f"{label} action body needs an 'action' string")
        for key, low, high in (("timeout", 0.2, 120), ("interval", 0.1, 5)):
            if key in step and (not isinstance(step[key], (int, float))
                                or isinstance(step[key], bool) or not low <= step[key] <= high):
                raise FlowError(f"{label}.{key} must be between {low} and {high}")
    return flow


class McpClient:
    def __init__(self, port: int):
        self.base_url = f"http://127.0.0.1:{port}/api/v1"
        self.opener = build_opener(ProxyHandler({}))

    def request(self, endpoint: str, body: dict | None = None) -> dict:
        url = f"{self.base_url}/{endpoint}"
        if body is None and endpoint != "settings":
            url += "?format=json"
        payload = None if body is None else json.dumps(body, ensure_ascii=False).encode("utf-8")
        request = Request(url, data=payload, headers={"Content-Type": "application/json"})
        try:
            with self.opener.open(request, timeout=10) as response:
                result = json.load(response)
        except HTTPError as exc:
            raise FlowError(f"HTTP {exc.code} from {endpoint}: {exc.read(500).decode('utf-8', 'replace')}") from exc
        except URLError as exc:
            raise FlowError(f"Cannot reach {url}: {exc.reason}") from exc
        if not isinstance(result, dict) or result.get("status") == "error":
            raise FlowError(f"MCP returned an error: {result}")
        if body is not None and result.get("status") != "ok":
            raise FlowError(f"MCP did not confirm the action: {result}")
        return result


def summary(state: dict) -> dict:
    return {key: state[key] for key in ("state_type", "menu_screen", "status", "message")
            if key in state}


def wait_until(client: McpClient, endpoint: str, checks: list[dict],
               timeout: float, interval: float) -> tuple[dict, list[dict], int]:
    deadline = time.monotonic() + timeout
    polls = 0
    while True:
        state = client.request(endpoint)
        polls += 1
        results = check_all(state, checks)
        if all(result["passed"] for result in results):
            return state, results, polls
        if time.monotonic() >= deadline:
            raise FlowError(f"Timed out after {polls} polls; last checks: {results}")
        time.sleep(min(interval, max(0, deadline - time.monotonic())))


def run_step(step: dict, client: McpClient, endpoint: str) -> dict:
    op = step["op"]
    if op == "wait":
        state, checks, polls = wait_until(client, endpoint, step["expect"],
                                          step.get("timeout", 30), step.get("interval", 0.5))
        return {"state": summary(state), "checks": checks, "polls": polls}
    state = client.request(endpoint)
    if op in {"snapshot", "assert"}:
        checks = check_all(state, step.get("expect", []))
        if not all(check["passed"] for check in checks):
            raise FlowError(f"Assertion failed: {checks}")
        return {"state": summary(state), "checks": checks}
    before = check_all(state, step.get("before", []))
    if not all(check["passed"] for check in before):
        raise FlowError(f"Precondition failed; action was not sent: {before}")
    body = dict(step["body"])
    for field, pointer in step.get("body_from_state", {}).items():
        value = resolve_pointer(state, pointer)
        if value is MISSING:
            raise FlowError(f"Action field {field!r} is missing at {pointer!r}; action was not sent")
        body[field] = value
    # Never retry POST. A transport timeout may happen after the game accepted it.
    response = client.request(endpoint, body)
    result = {"before": summary(state), "preconditions": before, "body": body,
              "response": response}
    if step.get("after"):
        after, checks, polls = wait_until(client, endpoint, step["after"],
                                          step.get("timeout", 30), step.get("interval", 0.5))
        result.update({"after": summary(after), "checks": checks, "polls": polls})
    return result


def run_flow(flow: dict, report_path: Path) -> None:
    clients = {name: McpClient(target["port"]) for name, target in flow["targets"].items()}
    report_path.parent.mkdir(parents=True, exist_ok=True)
    with report_path.open("w", encoding="utf-8") as report:
        for index, step in enumerate(flow["steps"], 1):
            target = step["target"]
            endpoint = step.get("endpoint", flow["targets"][target].get("mode", "singleplayer"))
            record = {"time": datetime.now(timezone.utc).isoformat(), "step": index,
                      "name": step.get("name", f"step {index}"), "target": target,
                      "endpoint": endpoint, "op": step["op"]}
            started = time.monotonic()
            try:
                record["result"] = run_step(step, clients[target], endpoint)
                record["status"] = "passed"
            except (FlowError, TimeoutError, OSError, ValueError) as exc:
                record["status"] = "failed"
                record["error"] = str(exc)
            record["duration_seconds"] = round(time.monotonic() - started, 3)
            report.write(json.dumps(record, ensure_ascii=False) + "\n")
            report.flush()
            print(f"{record['status'].upper():6} {index}/{len(flow['steps'])} {record['name']}")
            if record["status"] == "failed":
                raise FlowError(f"Step {index} failed: {record['error']}")


def main(argv: list[str] | None = None) -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="Run a JSON STS2 MCP test flow")
    parser.add_argument("flow", type=Path, help="Flow JSON file")
    parser.add_argument("--report", type=Path, help="JSONL report path")
    parser.add_argument("--only", help="Run steps for one named target")
    parser.add_argument("--dry-run", action="store_true", help="Validate and print steps without connecting")
    args = parser.parse_args(argv)
    try:
        flow = validate_flow(json.loads(args.flow.read_text(encoding="utf-8")))
        if args.only:
            if args.only not in flow["targets"]:
                raise FlowError(f"Unknown target: {args.only}")
            flow = {**flow, "steps": [step for step in flow["steps"] if step["target"] == args.only]}
            if not flow["steps"]:
                raise FlowError(f"No steps for target: {args.only}")
        if args.dry_run:
            for index, step in enumerate(flow["steps"], 1):
                print(f"{index}. {step.get('name', step['op'])} [{step['target']}]")
            return 0
        report = args.report or ROOT / "artifacts" / "test-logs" / (
            f"mcp-flow-{datetime.now().strftime('%Y%m%d-%H%M%S-%f')}.jsonl")
        run_flow(flow, report)
        print(f"Report: {report.resolve()}")
        return 0
    except (FlowError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
