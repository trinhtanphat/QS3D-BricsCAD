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
registration_attach = method_block(source, "public void Attach(Document document)")
recovery = method_block(source, "public void TryCompleteFailedInitialAttachCleanup()")
quiescence_aborted = method_block(source, "private void OnHostQuiescenceAborted(object? sender, EventArgs e)")
require(bool(attach), "top-level DocumentBoundWindowLifetime.Attach is missing", errors)
require(bool(registration_attach), "Registration.Attach is missing", errors)
require(bool(recovery), "failed-initial-attach cleanup recovery method is missing", errors)
require(bool(quiescence_aborted), "host-quiescence-aborted handler is missing", errors)

for token in (
    "private sealed class AttachGate",
    "public bool IsAttaching;",
    "ConditionalWeakTable<Window, AttachGate>",
    "AttachGates",
    "private bool _initialAttachFailed;",
    "public bool IsAttached => _attached;",
    "public bool HasFailedInitialAttach => _initialAttachFailed;",
    "public bool CanRestartAfterFailedInitialAttach =>",
    "_initialAttachFailed &&",
    "!_attached &&",
    "_nativeLifecycleSubscription == null;",
):
    require(token in source, "initial Attach retry state contract missing token: " + token, errors)

for token in (
    "lock (attachGate)",
    "if (attachGate.IsAttaching)",
    "attachGate.IsAttaching = true;",
    "Registrations.GetValue(window",
    "if (registration.HasFailedInitialAttach)",
    "registration.TryCompleteFailedInitialAttachCleanup();",
    "if (!registration.CanRestartAfterFailedInitialAttach)",
    "Registrations.Remove(window);",
    "registration = Registrations.GetValue(window",
    "var wasAttached = registration.IsAttached;",
    "registration.Attach(document);",
    "if (!wasAttached && registration.CanRestartAfterFailedInitialAttach &&",
    "Registrations.TryGetValue(window, out var currentRegistration)",
    "ReferenceEquals(currentRegistration, registration)",
    "attachGate.IsAttaching = false;",
    "throw;",
):
    require(token in attach, "initial Attach retry contract missing token: " + token, errors)

for token in (
    "_initialAttachFailed = true;",
    "_attached = true;",
    "Detach();",
):
    require(token in registration_attach, "failed initial Attach must retain rollback obligation: " + token, errors)

for token in (
    "if (!_initialAttachFailed) return;",
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;",
    "if (_attached) Detach();",
):
    require(token in recovery, "deferred failed-initial cleanup contract missing token: " + token, errors)

for token in (
    "if (_initialAttachFailed)",
    "TryCompleteFailedInitialAttachCleanup();",
    "return;",
):
    require(token in quiescence_aborted, "quit-abort must eagerly discharge failed initial Attach cleanup: " + token, errors)
if quiescence_aborted:
    failed_index = quiescence_aborted.find("if (_initialAttachFailed)")
    cleanup_index = quiescence_aborted.find("TryCompleteFailedInitialAttachCleanup();", failed_index)
    return_index = quiescence_aborted.find("return;", cleanup_index)
    existing_recovery_index = quiescence_aborted.find("if (Volatile.Read(ref _windowClosedDuringQuiescence)", return_index)
    require(
        0 <= failed_index < cleanup_index < return_index < existing_recovery_index,
        "failed initial Attach cleanup must run before ordinary modeless quit-abort recovery",
        errors,
    )

if attach:
    get_index = attach.find("Registrations.GetValue(window")
    failed_index = attach.find("if (registration.HasFailedInitialAttach)", get_index)
    cleanup_index = attach.find("registration.TryCompleteFailedInitialAttachCleanup();", failed_index)
    restart_index = attach.find("if (!registration.CanRestartAfterFailedInitialAttach)", cleanup_index)
    first_remove = attach.find("Registrations.Remove(window);", restart_index)
    recreate_index = attach.find("registration = Registrations.GetValue(window", first_remove)
    state_index = attach.find("var wasAttached = registration.IsAttached;", recreate_index)
    call_index = attach.find("registration.Attach(document);", state_index)
    catch_index = attach.find("catch", call_index)
    initial_only_index = attach.find("if (!wasAttached && registration.CanRestartAfterFailedInitialAttach &&", catch_index)
    exact_index = attach.find("Registrations.TryGetValue(window, out var currentRegistration)", initial_only_index)
    identity_index = attach.find("ReferenceEquals(currentRegistration, registration)", exact_index)
    second_remove = attach.find("Registrations.Remove(window);", identity_index)
    require(
        0 <= get_index < failed_index < cleanup_index < restart_index < first_remove < recreate_index
        < state_index < call_index < catch_index < initial_only_index < exact_index < identity_index < second_remove,
        "retry must discharge deferred cleanup before recreating from the retry document, and failure eviction must require cleanup completion",
        errors,
    )

print("QS3D V25 modeless initial Attach retry preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: failed initial modeless Attach retains cleanup ownership through quiescence, cleans eagerly after quit abort, recreates only after cleanup is complete, rejects reentrancy, and preserves successful rebind ownership.")
