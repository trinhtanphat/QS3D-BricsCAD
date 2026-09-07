#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / "src/QS3D.Core/Reporting/CurtainWallSchedule.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallScheduleReplacementGenerationFenceSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"


def fail(message: str) -> None:
    print(f"[FAIL] {message}")
    sys.exit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label}: missing required token: {token}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    first_index = text.find(first)
    second_index = text.find(second)
    if first_index < 0 or second_index < 0 or first_index >= second_index:
        fail(f"{label}: expected '{first}' before '{second}'")


def main() -> int:
    production = PRODUCTION.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    require(production, "project.Elements.ToList().AsReadOnly()", "production source-generation snapshot")
    require(production, "IReadOnlyList<ProjectElement> SourceElements", "production source-generation snapshot")
    require(production, "SameElementInstances(project.Elements, snapshot.SourceElements)", "production revision fence")
    require(production, "ReferenceEquals(current[index], sourceElements[index])", "production instance identity")
    require(production, "snapshot.Elements.Where", "production detached aggregation")
    require_before(
        production,
        "SameElementInstances(project.Elements, snapshot.SourceElements)",
        "SameElements(project.Elements, snapshot.Elements)",
        "production revision-fence ordering")

    require(smoke, "EquivalentElementReplacementWithoutTouchFailsClosed", "regression smoke")
    require(smoke, "project.Elements[0] = replacement", "regression replacement reproduction")
    require(smoke, "Equal(originalVersion, project.ChangeVersion)", "regression no-touch proof")
    require(smoke, "ReferenceEquals(original, replacement)", "regression distinct-instance proof")
    require(smoke, "Project changed while the curtain wall schedule was being built", "regression fail-closed assertion")

    registration_token = "CurtainWallScheduleReplacementGenerationFenceSmoke.Run();"
    if registration.count(registration_token) != 1:
        fail("smoke registration: expected exactly one Curtain Wall replacement generation fence registration")

    print("[PASS] Curtain Wall schedule replacement generation fence preflight")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
