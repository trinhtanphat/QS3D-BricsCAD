#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterCommands.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterPaletteCoordinator.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"{label} missing token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise AssertionError(f"{label} must not contain: {token}")


def slice_method(text: str, start_token: str, end_token: str, label: str) -> str:
    start = text.find(start_token)
    if start < 0:
        raise AssertionError(f"{label} missing start token: {start_token}")
    end = text.find(end_token, start)
    if end < 0:
        raise AssertionError(f"{label} missing end token: {end_token}")
    return text[start:end]


def main() -> int:
    command = COMMAND.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")

    show = slice_method(command, "public void ShowStartCenter()", "    }\n}", "QS3DSTART command")
    require(show, "StartCenterPaletteCoordinator.Show();", "QS3DSTART dispatch")
    require(show, "catch (System.Exception)", "QS3DSTART containment")
    require(show, '"\\nQS3DSTART could not open the Start Center."', "QS3DSTART stable failure text")
    forbid(show, "ex.Message", "QS3DSTART command")
    forbid(show, ".Message", "QS3DSTART command")

    activation = slice_method(
        coordinator,
        "private static void OnDocumentActivated",
        "    }\n}",
        "Start Center document activation",
    )
    require(activation, "_panel.RefreshFromActiveDocument();", "activation refresh")
    require(activation, "catch (Exception)", "activation containment")
    require(
        activation,
        '"\\nQS3DSTART refresh could not update the Start Center."',
        "activation stable failure text",
    )
    forbid(activation, "ex.Message", "activation callback")
    forbid(activation, ".Message", "activation callback")

    # Preserve the native lifecycle safety contract while changing only diagnostics.
    require(coordinator, "var wasVisible = palette.Visible;", "visibility rollback snapshot")
    require(coordinator, "var wasSubscribed = _documentActivatedSubscribed;", "subscription rollback snapshot")
    require(coordinator, "if (!wasVisible)", "visibility rollback gate")
    require(coordinator, "if (!wasSubscribed)\n                    UnsubscribeFromDocumentActivation();", "subscription rollback")
    require(coordinator, "Application.DocumentManager.DocumentActivated += OnDocumentActivated;", "activation subscribe")
    require(coordinator, "Application.DocumentManager.DocumentActivated -= OnDocumentActivated;", "activation unsubscribe")

    print("PASS: Start Center command/activation failures are stable-redacted while native PaletteSet lifecycle rollback remains pinned.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, ValueError) as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
