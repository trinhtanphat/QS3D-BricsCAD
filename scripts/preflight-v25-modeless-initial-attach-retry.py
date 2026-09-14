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
    "private sealed class AttachGate",
    "public bool IsAttaching;",
    "ConditionalWeakTable<Window, AttachGate>",
    "AttachGates",
):
    require(token in source, "initial Attach retry gate missing token: " + token, errors)

for token in (
    "lock (attachGate)",
    "if (attachGate.IsAttaching)",
    "attachGate.IsAttaching = true;",
    "Registrations.GetValue(window",
    "registration.Attach(document);",
    "Registrations.TryGetValue(window, out var currentRegistration)",
    "ReferenceEquals(currentRegistration, registration)",
    "Registrations.Remove(window);",
    "attachGate.IsAttaching = false;",
    "throw;",
):
    require(token in attach, "initial Attach retry contract missing token: " + token, errors)

if attach:
    lock_index = attach.find("lock (attachGate)")
    fence_index = attach.find("if (attachGate.IsAttaching)", lock_index)
    mark_index = attach.find("attachGate.IsAttaching = true;", fence_index)
    get_index = attach.find("Registrations.GetValue(window", mark_index)
    call_index = attach.find("registration.Attach(document);", get_index)
    catch_index = attach.find("catch", call_index)
    exact_index = attach.find("Registrations.TryGetValue(window, out var currentRegistration)", catch_index)
    identity_index = attach.find("ReferenceEquals(currentRegistration, registration)", exact_index)
    remove_index = attach.find("Registrations.Remove(window);", identity_index)
    throw_index = attach.find("throw;", remove_index)
    finally_index = attach.find("finally", throw_index)
    clear_index = attach.find("attachGate.IsAttaching = false;", finally_index)
    require(
        0 <= lock_index < fence_index < mark_index < get_index < call_index < catch_index
        < exact_index < identity_index < remove_index < throw_index < finally_index < clear_index,
        "failed initial Attach must reject reentrancy, evict only the exact failed registration, rethrow, then clear the in-progress fence",
        errors,
    )

print("QS3D V25 modeless initial Attach retry preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: failed initial modeless Attach is serialized per Window, rejects reentrant replacement, and evicts only the exact failed registration before a retry can bind stale wrapper/native-generation affinity.")
