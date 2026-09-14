#!/usr/bin/env python3
from pathlib import Path
import sys

SOURCE = Path("src/QS3D.BricsCAD.V25/UI/DocumentBoundWindowLifetime.cs")


def require(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        return ""
    brace = source.find("{", start)
    if brace < 0:
        return ""
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start:index + 1]
    return ""


errors: list[str] = []
require(SOURCE.exists(), "DocumentBoundWindowLifetime.cs is missing", errors)
if errors:
    print("QS3D V25 modeless initial Attach retry preflight")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
attach = method_block(source, "public static void Attach(Window window, Document document)")
require(bool(attach), "top-level DocumentBoundWindowLifetime.Attach is missing", errors)

for token in (
    "ConditionalWeakTable<Window, object>",
    "AttachGates",
    "lock (attachGate)",
    "Registrations.GetValue(window",
    "registration.Attach(document);",
    "Registrations.Remove(window);",
    "throw;",
):
    require(token in source if token in ("ConditionalWeakTable<Window, object>", "AttachGates") else token in attach,
            "initial Attach retry contract missing token: " + token,
            errors)

if attach:
    try_index = attach.find("try")
    call_index = attach.find("registration.Attach(document);")
    catch_index = attach.find("catch", call_index)
    remove_index = attach.find("Registrations.Remove(window);", catch_index)
    throw_index = attach.find("throw;", remove_index)
    require(0 <= try_index < call_index < catch_index < remove_index < throw_index,
            "failed initial Attach must evict the failed per-window registration before rethrow",
            errors)
    gate_index = attach.find("lock (attachGate)")
    get_index = attach.find("Registrations.GetValue(window", gate_index)
    require(0 <= gate_index < get_index < call_index,
            "per-window retry gate must serialize registration lookup, attach, and failed-registration eviction",
            errors)

print("QS3D V25 modeless initial Attach retry preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: failed initial modeless Attach is serialized per Window and evicts the failed registration before a retry can bind stale wrapper/native-generation affinity.")
