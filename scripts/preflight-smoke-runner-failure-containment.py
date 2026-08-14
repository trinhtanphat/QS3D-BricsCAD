#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "Program.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")
    run = "SmokeTestRegistration.RunAll();"
    run_index = text.find(run)
    require(run_index >= 0, "Core smoke runner must execute registered smoke tests.")

    try_index = text.rfind("try", 0, run_index)
    catch_index = text.find("catch (Exception ex)", run_index)
    require(try_index >= 0 and catch_index > run_index,
            "Registered smoke execution must be wrapped by a top-level try/catch boundary.")

    catch_end = text.find("Test(\"PolylineMetrics rectangle\"", catch_index)
    require(catch_end > catch_index,
            "Core smoke runner structure changed; update failure-containment preflight intentionally.")
    catch_block = text[catch_index:catch_end]
    require('FAIL registered smoke phase: ' in catch_block,
            "Registered smoke failures must identify the failing phase in console output.")
    require("ex.GetType().FullName" in catch_block and "ex.Message" in catch_block,
            "Registered smoke failures must retain original exception type and message.")
    require("return 1;" in catch_block,
            "Registered smoke failures must exit with a non-zero process code.")
    require("throw;" not in catch_block,
            "Registered smoke failures must not be rethrown into a Windows application-error popup.")
    require("private static void Test(string name,Action action)" in text,
            "Existing per-test smoke failure collection must remain intact.")
    print("Smoke runner failure-containment preflight passed.")


if __name__ == "__main__":
    main()
