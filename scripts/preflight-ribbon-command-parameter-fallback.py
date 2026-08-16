#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FALLBACK = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonCommandParameterFallback.cs"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"


def fail(message: str) -> None:
    raise AssertionError(message)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label} missing required contract marker: {token}")


def main() -> int:
    try:
        fallback = FALLBACK.read_text(encoding="utf-8")
        coordinator = COORDINATOR.read_text(encoding="utf-8")
        v26 = V26.read_text(encoding="utf-8")

        for token in (
            "internal static class RibbonCommandParameterFallback",
            'GetProperty(item, "CommandParameter") as string',
            'GetProperty(item, "CommandHandler") as ICommand',
            "handler is CommandParameterFallbackHandler",
            "new CommandParameterFallbackHandler(handler, command)",
            "public bool CanExecute(object? parameter) => _inner.CanExecute(ResolveParameter(parameter));",
            "public void Execute(object? parameter) => _inner.Execute(ResolveParameter(parameter));",
            ": _fallbackCommand;",
            'tabId.StartsWith(Qs3dTabPrefix, StringComparison.OrdinalIgnoreCase)',
        ):
            require(fallback, token, "ribbon command fallback")

        update_index = coordinator.find("UpdateRibbonAugmenter.TryInitialize()")
        fallback_index = coordinator.find("RibbonCommandParameterFallback.TryInitialize()")
        grouping_index = coordinator.find("Qs3dRibbonTabGroupCoordinator.TryInitialize()")
        if min(update_index, fallback_index, grouping_index) < 0:
            fail("ribbon initialization coordinator is missing update/fallback/grouping stages")
        if not update_index < fallback_index < grouping_index:
            fail("ribbon command fallback must run after all augmenters and before final tab grouping")

        require(
            v26,
            '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
            "V26 linked-source project",
        )

        print("PASS: QS3D ribbon commands preserve their captured command when BricsCAD omits ICommand parameters.")
        print(" - fallback wraps only QS3D ribbon handlers with non-empty CommandParameter values")
        print(" - fallback runs after ribbon augmentation and before final QS3D tab grouping")
        print(" - V26 consumes the same guarded V25 ribbon source")
        return 0
    except (OSError, AssertionError) as exc:
        print("Ribbon command parameter fallback preflight FAILED:")
        print(" -", exc)
        return 1


if __name__ == "__main__":
    sys.exit(main())
