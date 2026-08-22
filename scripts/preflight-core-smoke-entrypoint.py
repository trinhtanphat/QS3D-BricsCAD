#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QS3D.Core.SmokeTests.csproj"
ENTRY = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestEntryPoint.cs"
PROGRAM = ROOT / "tests" / "QS3D.Core.SmokeTests" / "Program.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing smoke-runner contract file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"missing {label}: {token}")


def main() -> int:
    project = read(PROJECT)
    entry = read(ENTRY)
    program = read(PROGRAM)

    require(
        project,
        "<StartupObject>QS3D.Core.SmokeTests.SmokeTestEntryPoint</StartupObject>",
        "guarded executable startup object",
    )
    require(entry, "typeof(Program).GetMethod(", "legacy runner invocation")
    require(entry, "BindingFlags.NonPublic | BindingFlags.Static", "private Main lookup")
    require(entry, "catch (TargetInvocationException ex)", "reflection exception containment")
    require(entry, "var actual = ex.InnerException ?? ex;", "original exception unwrapping")
    require(entry, '"FAIL smoke runner: " + actual.GetType().FullName + ": " + actual.Message', "original failure diagnostics")
    require(entry, "return 1;", "non-zero failure exit")
    require(program, "private static int Main()", "legacy smoke runner remains unchanged")
    require(program, "SmokeTestRegistration.RunAll();", "registered smoke execution remains enabled")

    if "throw actual" in entry or "throw ex" in entry:
        raise AssertionError("guarded entry point must not rethrow the contained smoke exception")

    print("PASS: Core smoke executable uses a guarded entry point that converts escaping smoke exceptions into deterministic console diagnostics and exit code 1 instead of an unhandled Windows application error.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
