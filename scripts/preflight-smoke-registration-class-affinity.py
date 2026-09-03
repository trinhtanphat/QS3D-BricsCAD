#!/usr/bin/env python3
"""Regression guard for smoke Run() owner/class affinity."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
TARGET = HERE / "preflight-smoke-registration.py"


def load_target():
    spec = spec_from_file_location("qs3d_smoke_registration_preflight", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load smoke-registration preflight")
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    target = load_target()
    sources = {
        Path("HelperFirstSmoke.cs"): (
            "internal static class Helper { internal static void Touch() { } } "
            "internal static class ActualSmoke { internal static void Run() { } }"
        ),
        Path("HelperFirstRegistration.cs"): (
            "internal static class HelperFirstRegistration { "
            "internal static void Register() { ActualSmoke.Run(); } }"
        ),
    }

    checked, errors, source_scans = target.find_registration_errors(sources)
    if checked != 1:
        print("ERROR: expected exactly one runnable smoke class, got", checked)
        return 1
    if source_scans != len(sources):
        print("ERROR: owner-affinity regression changed one-pass source indexing")
        return 1
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("ERROR: smoke registration must bind to the class that owns static Run(), not the first class in the file")
        return 1

    print("PASS: smoke registration resolves the lexical owner of static Run()")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
