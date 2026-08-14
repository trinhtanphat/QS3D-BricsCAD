#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "scripts" / "test-local-v25-interactive-matrix-evidence.ps1"
CLOSEOUT = ROOT / "scripts" / "complete-local-v25-qualification.ps1"
EXAMPLE = ROOT / "docs" / "LOCAL-V25-INTERACTIVE-MATRIX.example.json"
DOC = ROOT / "docs" / "LOCAL-V25-QUALIFICATION-CLOSEOUT.md"

REQUIRED_SCENARIOS = (
    "pluginShellUi",
    "demandLoad",
    "directDraw",
    "build3dGeneratedOwnershipHealth",
    "doorOpening",
    "roomHtPhong",
    "curtain",
    "rebar",
    "projectSaveReopenMultiDwg",
    "modelessMultiDwgLifecycle",
    "modelessEditorRollbackPostCommit",
    "reportingBqBbsExcel",
    "unicodeHiDpi",
    "cleanInstallUpgradeUninstall",
    "privateDwgRegression",
)


def require(text: str, needle: str, where: str) -> None:
    if needle not in text:
        raise AssertionError(f"{where}: missing required contract token: {needle}")


def main() -> int:
    for path in (VALIDATOR, CLOSEOUT, EXAMPLE, DOC):
        if not path.is_file():
            print(f"ERROR: missing qualification closeout file: {path.relative_to(ROOT)}")
            return 1

    validator = VALIDATOR.read_text(encoding="utf-8")
    closeout = CLOSEOUT.read_text(encoding="utf-8")
    example = EXAMPLE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

    try:
        for scenario in REQUIRED_SCENARIOS:
            require(validator, f'"{scenario}"', "validator")
            require(example, f'"{scenario}": "NOT_TESTED"', "example")

        for token in (
            "ExpectedSha",
            "ExpectedPluginSha256",
            "licensedBricsCadV25",
            "executedOnLicensedV25",
            "sameExactShaAndPlugin",
            "knownBlockers",
            "must be PASS",
            "raw machine/private path",
        ):
            require(validator, token, "validator")

        for token in (
            '"windowsX64": false',
            '"interactive": false',
            '"licensedBricsCadV25": false',
            '"executedOnLicensedV25": false',
            '"sameExactShaAndPlugin": false',
        ):
            require(example, token, "example")

        require(closeout, "run-local-v25-qualification.ps1", "closeout")
        require(closeout, "test-local-v25-interactive-matrix-evidence.ps1", "closeout")
        require(closeout, 'runtimeSmokeStatus -ne "PASS"', "closeout")
        require(closeout, 'fullInteractiveMatrixStatus = "PASS"', "closeout")
        require(closeout, "licensedV25RuntimeQualified", "closeout")
        if "SkipRuntime" in closeout:
            raise AssertionError("closeout: must not expose or pass SkipRuntime")

        validator_pos = closeout.index("test-local-v25-interactive-matrix-evidence.ps1")
        qualified_pos = closeout.index("licensedV25RuntimeQualified")
        if validator_pos >= qualified_pos:
            raise AssertionError("closeout: qualification promotion must occur only after evidence validation")

        for token in (
            "real interactive Windows x64",
            "licensed BricsCAD V25",
            "exact Git SHA",
            "plugin hash mismatch",
            "must not be reused",
        ):
            require(doc, token, "closeout doc")
    except AssertionError as exc:
        print(f"ERROR: {exc}")
        return 1

    print(
        "PASS: licensed V25 qualification closeout is fail-closed, exact-SHA/plugin-bound, "
        "and cannot promote the example/static path."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
