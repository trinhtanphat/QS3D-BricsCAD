#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RECT = ROOT / "src/QS3D.BricsCAD.V25/RectangularGridCommands.cs"
RADIAL = ROOT / "src/QS3D.BricsCAD.V25/RadialGridCommands.cs"
REPEAT = ROOT / "src/QS3D.BricsCAD.V25/GridRepeatCommands.cs"
STATE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GridAuthoringRepeatState.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"{label}: missing {needle!r}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"{label}: forbidden {needle!r}")


def main() -> int:
    rect = RECT.read_text(encoding="utf-8")
    radial = RADIAL.read_text(encoding="utf-8")
    repeat = REPEAT.read_text(encoding="utf-8")
    state = STATE.read_text(encoding="utf-8")

    # A template becomes repeatable only after the canonical native builder returns.
    require(rect, "RectangularGridNativeSourceBuilder.Build(document, project, request);\n                Cad.GridAuthoringRepeatState.RememberRectangular(document, request);", "rectangular commit ordering")
    require(radial, "RadialGridNativeSourceBuilder.Build(document, project, request);\n                Cad.GridAuthoringRepeatState.RememberRadial(document, request);", "radial commit ordering")

    # State is weakly keyed by the BricsCAD Document so closed DWGs are not retained and
    # separate open documents cannot read one another's templates.
    require(state, "ConditionalWeakTable<Document, State>", "weak per-document repeat state")
    require(state, "States.TryGetValue(document, out var state)", "same-document lookup")
    require(state, "TryCreateRectangularRequest", "rectangular template reconstruction")
    require(state, "TryCreateRadialRequest", "radial template reconstruction")

    # Repeat commands ask for new semantic identity/placement and reuse canonical builders.
    require(repeat, '[CommandMethod("QS3DGRIDRECTREPEAT")]', "rectangular repeat command")
    require(repeat, '[CommandMethod("QS3DGRIDRADIALREPEAT")]', "radial repeat command")
    require(repeat, "TryCreateRectangularRequest", "rectangular missing-state gate")
    require(repeat, "TryCreateRadialRequest", "radial missing-state gate")
    require(repeat, "RectangularGridNativeSourceBuilder.Build(document, project, request)", "canonical rectangular builder reuse")
    require(repeat, "RadialGridNativeSourceBuilder.Build(document, project, request)", "canonical radial builder reuse")
    require(repeat, "chưa có rectangular Grid template đã commit", "rectangular fail-closed missing state")
    require(repeat, "chưa có radial Grid template đã commit", "radial fail-closed missing state")

    # Failure reporting must remain sanitized; native exception details are not exposed.
    forbid(repeat, "ex.Message", "repeat exception redaction")
    forbid(repeat, "StackTrace", "repeat exception redaction")
    forbid(repeat, "Exception.ToString", "repeat exception redaction")

    # The carrier must not bypass canonical builders with direct DB mutation.
    forbid(repeat, "StartTransaction", "repeat canonical builder boundary")
    forbid(repeat, "AppendEntity", "repeat canonical builder boundary")
    forbid(repeat, ".Erase()", "repeat canonical builder boundary")

    print("PASS Grid repeat authoring per-DWG/commit-order/canonical-builder contract")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: Grid repeat authoring preflight failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
