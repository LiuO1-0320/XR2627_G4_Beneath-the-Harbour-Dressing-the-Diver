"""Exercise the staff HTTP controls against a disposable BasicScene session.

Run python Tests/check-mobile-controls.py while the exhibit is idle. Paste the
local staff panel's control link at the hidden prompt. This starts two test
visits and resets both. A failing case stops at the observed state.
"""

from concurrent.futures import ThreadPoolExecutor
from getpass import getpass
from http.client import HTTPException
import json
from threading import Barrier
import time
from urllib.error import HTTPError, URLError
from urllib.parse import parse_qs, urlsplit
from urllib.request import HTTPRedirectHandler, ProxyHandler, Request, build_opener
from uuid import UUID, uuid4


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def require(condition, description):
    if not condition:
        raise AssertionError(description)


def run(control_link):
    url = urlsplit(control_link.strip())
    key = parse_qs(url.fragment).get("key", [""])[0]
    runtime = parse_qs(url.query).get("runtime", [""])[0]
    require(url.scheme in ("http", "https") and url.netloc and key and runtime,
            "Use the full control link copied from the local staff panel.")
    origin = url.scheme + "://" + url.netloc
    checked = []

    def http(path, payload=None, token=None, sender=origin):
        data = None if payload is None else json.dumps(payload).encode("utf-8")
        headers = {"Accept": "application/json"}
        if data is not None:
            headers.update({"Origin": sender, "Content-Type": "application/json"})
        if token is not None:
            headers["Authorization"] = "Bearer " + token
        request = Request(origin + path, data=data, headers=headers)
        # Every request is a new connection to this host. Never forward a credential.
        client = build_opener(ProxyHandler({}), NoRedirect())
        try:
            response = client.open(request, timeout=8)
        except HTTPError as failure:
            response = failure
        with response:
            body = response.read()
            require(key.encode() not in body, "Response disclosed staff credential.")
            try:
                result = json.loads(body)
            except (ValueError, UnicodeError):
                result = {}
            return response.status, result

    def view():
        code, incoming = http("/api/view")
        require(code == 200 and incoming.get("snapshot", {}).get("runtimeId") == runtime,
                "Link runtime differs from the current Unity instance.")
        return incoming["snapshot"]

    def wait_for(predicate, description):
        deadline = time.monotonic() + 6
        while time.monotonic() < deadline:
            s = view()
            if predicate(s):
                return s
            time.sleep(0.10)
        raise AssertionError(description)

    def make(snapshot, action):
        return dict(protocolVersion=1, commandId=str(uuid4()), runtimeId=runtime,
                    sessionId=snapshot["sessionId"], expectedRevision=snapshot["revision"], action=action)

    def send(command):
        status, reply = http("/api/command", command, key)
        require(status == 200, f"{command['action']} returned HTTP {status} before dispatch.")
        require(reply.get("commandId") == command["commandId"] and reply.get("runtimeId") == runtime,
                "HTTP reply was not correlated to the command and runtime.")
        return reply

    def applied(command):
        reply = send(command)
        require(reply.get("accepted") and reply.get("code") == "Applied", command["action"] + " was not applied.")
        return reply

    def at(reply, status, stage):
        return wait_for(lambda s: s["revision"] == reply["revision"] and
                        s["sessionId"] == reply["sessionId"] and s["status"] == status and
                        s["stage"] == stage, "Published view did not reach " + status + "/" + stage)

    def unchanged(previous):
        snap = view()
        require(all(previous[k] == snap[k] for k in ("runtimeId", "sessionId", "revision", "stage", "status")),
                "A rejected or duplicate command changed the visit.")

    def mark(name):
        checked.append(name)
        print("CONTROL_OK " + name, flush=True)

    idle = view()
    require(idle["status"] == "Idle" and UUID(idle["sessionId"]).int == 0 and idle["ready"],
            "This check requires an idle, ready exhibit with no visitor.")
    target = parse_qs(url.query)
    require(target.get("visit") == [idle["sessionId"]] and target.get("rev") == [str(idle["revision"])],
            "Copy a fresh control link for this waiting period from the staff panel.")
    start = make(idle, "Start")
    old_idle_start = make(idle, "Start")
    for label, token, sender, expected in [
        ("missing key", None, origin, 401),
        ("wrong key", "wrong-token", origin, 401),
        ("foreign Origin", key, "http://other.invalid", 403),
    ]:
        try:
            code, _ = http("/api/command", start, token, sender)
        except (OSError, HTTPException) as error:
            raise AssertionError(f"{label}: transport {type(error).__name__}, no command retry.") from error
        require(code == expected, f"{label}: expected HTTP {expected}, got {code}.")
        print(f"CONTROL_HTTP {label} returned {code}", flush=True)
    unchanged(idle)
    mark("missing key, wrong key and foreign Origin all rejected")

    start_reply = applied(start)
    first = at(start_reply, "Running", "Briefing")
    repeated_start = applied(start)
    require(repeated_start == start_reply, "Immediate Start retry did not return its original result.")
    unchanged(first)
    mark("first visit started once")

    pause = make(first, "Pause")
    pause_reply = applied(pause)
    paused = at(pause_reply, "Paused", "Briefing")
    require(applied(pause) == pause_reply, "Immediate Pause retry changed the result.")
    unchanged(paused)
    mark("Pause and same-ID retry")
    running = at(applied(make(paused, "Resume")), "Running", "Briefing")
    mark("Resume")

    # Two staff devices use the same view but different IDs for Skip.
    time.sleep(1.1)
    barrier = Barrier(2)

    def race(command):
        barrier.wait(timeout=5)
        return send(command)

    commands = [make(running, "Skip"), make(running, "Skip")]
    with ThreadPoolExecutor(max_workers=2) as devices:
        responses = list(devices.map(race, commands))
    winners = [(c, r) for c, r in zip(commands, responses) if r.get("accepted")]
    require(len(winners) == 1 and any(r.get("code") == "StaleState" for r in responses),
            "Two competing Skip requests did not produce one winner and one stale reply.")
    skip, winner = winners[0]
    case = at(winner, "Running", "TheCase")
    results = {row["stage"]: row["result"] for row in case["stages"]}
    require(results["Briefing"] == "Skipped" and results["TheCase"] == "Active",
            "Skipped stage was marked complete or the wrong stage started.")
    require(applied(skip) == winner, "Retry of the winning Skip did not match the original.")
    unchanged(case)
    mark("two staff Skip requests compete; one Skipped stage and one idempotent retry")

    unpublished_skip = make(case, "Skip")
    unpublished_reset = make(case, "Reset")
    reset = make(case, "Reset")
    reset_reply = applied(reset)
    idle_again = at(reset_reply, "Idle", "Briefing")
    require(applied(reset) == reset_reply, "Same reset command executed twice or did not replay.")
    unchanged(idle_again)
    mark("Reset and immediate idempotent retry")
    for old in (start, old_idle_start):
        reply = send(old)
        require(reply.get("code") == "StaleState" and not reply.get("accepted"),
                "Previous Idle Start was not rejected at the next Idle.")
    unchanged(idle_again)
    mark("previous Idle Start cannot start another visit during handover")

    # Separate two visitor handovers from the HTTP request-rate limit.
    time.sleep(1.1)
    second = at(applied(make(idle_again, "Start")), "Running", "Briefing")
    require(first["sessionId"] != second["sessionId"], "Second visitor reused the first visitor ID.")
    for old in (skip, reset, start, unpublished_skip, unpublished_reset, old_idle_start):
        reply = send(old)
        require(reply.get("code") == "StaleState" and not reply.get("accepted"),
                "An old " + old["action"] + " was not rejected in the new visit.")
    unchanged(second)
    require(all(r["result"] in ("Pending", "Active") for r in second["stages"]),
            "A previous visit's stage result leaked into the new visit.")
    _, current = http("/api/view")
    require(current["snapshot"]["sessionId"] == second["sessionId"] and
            all(e["sessionId"] == second["sessionId"] and e["runtimeId"] == runtime for e in current["events"]),
            "Published operation history includes another visitor's commands.")
    mark("second visit rejects cached and delayed first-visit Skip, Reset and Start")
    at(applied(make(second, "Reset")), "Idle", "Briefing")
    mark("second visit reset for handover")
    return {"runtimeId": runtime, "firstSession": first["sessionId"], "secondSession": second["sessionId"],
            "checks": checked}


if __name__ == "__main__":
    try:
        print("This will start and reset two test visits. Use an idle exhibit.")
        run(getpass("Control link (hidden): "))
    except (AssertionError, ValueError, ConnectionError, URLError, HTTPException) as error:
        raise SystemExit("CONTROL_FAIL " + str(error))
