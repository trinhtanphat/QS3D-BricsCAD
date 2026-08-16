#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

CONTRACTS = {
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs": (
        'if (result == 0d && numerator != 0d)',
        'label + " underflowed to zero."',
    ),
    "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs": (
        "private void EnsureFiniteBounds()",
        'throw new OverflowException("Curtain opening width is below the representable coordinate resolution.");',
        'throw new OverflowException("Curtain opening horizontal clearance is below the representable coordinate resolution.");',
        'throw new InvalidOperationException("Curtain frame rectangle width is below the representable coordinate resolution.");',
    ),
    "src/QS3D.Core/Geometry/CurtainWallOpeningFramePlanner.cs": (
        'throw new OverflowException(label + " width is below the representable coordinate resolution.");',
        'throw new OverflowException(label + " horizontal clearance is below the representable coordinate resolution.");',
        'throw new OverflowException(label + " vertical clearance is below the representable coordinate resolution.");',
    ),
    "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs": (
        'throw new OverflowException("Curtain rectangle area underflowed to zero.");',
        'throw new OverflowException(label + " width is below the representable coordinate resolution.");',
        'throw new OverflowException(label + " height is below the representable coordinate resolution.");',
        'throw new OverflowException(label + " underflowed to zero.");',
    ),
    "tests/QS3D.Core.SmokeTests/CurtainWallLayoutUnderflowRegistration.cs": (
        "[ModuleInitializer]",
        "CurtainWallLayoutUnderflowSmoke.Run();",
    ),
    "tests/QS3D.Core.SmokeTests/CurtainFrameOpeningCoordinateCollapseRegistration.cs": (
        "[ModuleInitializer]",
        "CurtainFrameOpeningCoordinateCollapseSmoke.Run();",
    ),
    "tests/QS3D.Core.SmokeTests/CurtainWallOpeningFrameCoordinateCollapseRegistration.cs": (
        "[ModuleInitializer]",
        "CurtainWallOpeningFrameCoordinateCollapseSmoke.Run();",
    ),
    "tests/QS3D.Core.SmokeTests/CurtainWallDetailNumericCollapseRegistration.cs": (
        "[ModuleInitializer]",
        "CurtainWallDetailNumericCollapseSmoke.Run();",
    ),
}


def main() -> int:
    for relative, needles in CONTRACTS.items():
        path = ROOT / relative
        if not path.is_file():
            raise SystemExit(f"FAIL: missing curtain numeric safety file: {relative}")
        text = path.read_text(encoding="utf-8")
        for needle in needles:
            if needle not in text:
                raise SystemExit(f"FAIL: {relative} missing curtain numeric safety contract: {needle}")

    print(
        "PASS: curtain layout/detail/opening planners retain fail-closed numeric underflow and "
        "coordinate-resolution guards with deterministic smoke registration."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
