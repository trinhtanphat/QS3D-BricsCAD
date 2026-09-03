#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
CONSENT = SRC / "McpOAuthConsent.cs"
AUTH = SRC / "McpOAuthAuthorizationServer.cs"


def require(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


def ordered(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    cursor = -1
    for token in tokens:
        found = text.find(token, cursor + 1)
        if found < 0:
            errors.append(f"{label} missing ordered token: {token}")
            return
        cursor = found


def main() -> int:
    missing = [path for path in (CONSENT, AUTH) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    consent = CONSENT.read_text(encoding="utf-8")
    auth = AUTH.read_text(encoding="utf-8")
    errors: list[str] = []

    request_marker = "internal static McpOAuthConsentResult RequestApproval"
    callback_marker = "private static void ShowConsentInCadContext"
    request_at = consent.find(request_marker)
    callback_at = consent.find(callback_marker)
    if request_at < 0 or callback_at < 0 or request_at >= callback_at:
        errors.append("OAuth consent methods missing or unexpectedly reordered")
        request = consent
        callback = consent
    else:
        request = consent[request_at:callback_at]
        callback = consent[callback_at:]

    require(errors, consent, (
        "InteractionRequired = 3,",
    ), "OAuth interaction result")

    require(errors, request, (
        "if (!ConsentGate.Wait(0)) return McpOAuthConsentResult.InteractionRequired;",
        "McpCadMutationCoordinator.EnterInteractiveModal(",
        '"oauth_interactive_consent"',
        "catch (InvalidOperationException)",
        "return McpOAuthConsentResult.InteractionRequired;",
        "ExecuteInApplicationContext(ShowConsentInCadContext, item)",
        "item.Done.Wait(ConsentTimeoutMilliseconds)",
        "ConsentCancelledBeforeStart",
        "item.Done.Wait();",
        "interactionAdmission.Dispose();",
        "ConsentGate.Release();",
    ), "OAuth interactive CAD admission")

    if "McpCadMutationCoordinator.EnterMutation(" in request:
        errors.append("OAuth consent must not model foreground UI as a CAD mutation")

    # Runtime ordering spans two methods: RequestApproval owns semantic interactive admission
    # while the application-context callback presents the modal and signals Done. Assert each
    # method's causal ordering instead of comparing source offsets between separate bodies.
    ordered(errors, request, (
        "McpCadMutationCoordinator.EnterInteractiveModal(",
        "ExecuteInApplicationContext(ShowConsentInCadContext, item)",
        "item.Done.Wait(ConsentTimeoutMilliseconds)",
        "ConsentCancelledBeforeStart",
        "item.Done.Wait();",
        "interactionAdmission.Dispose();",
        "ConsentGate.Release();",
    ), "OAuth request admission/dispatch/wait/release ordering")

    ordered(errors, callback, (
        "Interlocked.CompareExchange(ref item.DispatchState, ConsentRunning, ConsentQueued)",
        "MessageBox.Show(",
        "item.Done.Set();",
    ), "OAuth foreground callback modal/signal ordering")

    require(errors, auth, (
        "consent == McpOAuthConsentResult.InteractionRequired",
        '"interaction_required"',
    ), "OAuth interaction_required protocol mapping")

    authorize_at = auth.find("var consent = McpOAuthConsent.RequestApproval(resource, normalizedScope);")
    interaction_at = auth.find("consent == McpOAuthConsentResult.InteractionRequired", authorize_at + 1)
    denied_at = auth.find("consent == McpOAuthConsentResult.Denied", authorize_at + 1)
    if authorize_at < 0 or interaction_at < 0 or denied_at < 0 or not (authorize_at < interaction_at < denied_at):
        errors.append("OAuth authorization must map InteractionRequired before access-denied/fallback handling")

    if errors:
        print("ERROR: MCP OAuth/CAD interaction preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: OAuth consent acquires semantic shared interactive-modal admission before UI dispatch, holds it until the foreground callback closes/signals, cancels queued dispatch before releasing admission, prevents concurrent modal storms, and maps CAD-busy admission failures to OAuth interaction_required.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
