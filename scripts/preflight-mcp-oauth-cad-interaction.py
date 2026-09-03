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


def main() -> int:
    missing = [path for path in (CONSENT, AUTH) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    consent = CONSENT.read_text(encoding="utf-8")
    auth = AUTH.read_text(encoding="utf-8")
    errors: list[str] = []

    require(errors, consent, (
        "InteractionRequired = 3,",
        'McpCadMutationCoordinator.EnterMutation(string.Empty, "oauth_interactive_consent", null)',
        "catch (InvalidOperationException)",
        "return McpOAuthConsentResult.InteractionRequired;",
        "item.Done.Wait();",
        "interactionAdmission.Dispose();",
    ), "OAuth interactive CAD admission")

    require(errors, auth, (
        "consent == McpOAuthConsentResult.InteractionRequired",
        '"interaction_required"',
    ), "OAuth interaction_required protocol mapping")

    if 'MessageBox.Show(' not in consent:
        errors.append("OAuth consent prompt unexpectedly disappeared; this guard expects the explicit foreground consent boundary")

    admission_at = consent.find('McpCadMutationCoordinator.EnterMutation(string.Empty, "oauth_interactive_consent", null)')
    dispatch_at = consent.find("ExecuteInApplicationContext(ShowConsentInCadContext, item)")
    modal_at = consent.find("MessageBox.Show(")
    dispose_at = consent.find("interactionAdmission.Dispose();")
    if admission_at < 0 or dispatch_at < 0 or modal_at < 0 or dispose_at < 0 or not (admission_at < dispatch_at < modal_at < dispose_at):
        errors.append("OAuth consent ordering must be CAD admission -> UI dispatch -> modal -> admission release")

    if errors:
        print("ERROR: MCP OAuth/CAD interaction preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: OAuth interactive consent is admitted through the CAD mutation/writer gate before UI dispatch, maps busy CAD state to interaction_required, keeps single-flight foreground consent bounded against queued dispatch, and releases admission only after the modal path is complete.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
