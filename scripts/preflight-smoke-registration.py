#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TESTS = ROOT / "tests" / "QS3D.Core.SmokeTests"
RUN_PATTERN = re.compile(r"\b(?:public|internal|private)?\s*static\s+void\s+Run\s*\(")
CLASS_PATTERN = re.compile(r"\b(?:public|internal|private)?\s*(?:static\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)")
RUN_CALL_PATTERN = re.compile(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*Run\s*\(")
MODULE_INITIALIZER_METHOD_PATTERN = re.compile(
    r"\[\s*(?:System\.Runtime\.CompilerServices\.)?ModuleInitializer(?:Attribute)?\s*\]"
    r"\s*(?:(?:public|internal|private)\s+)?static\s+void\s+[A-Za-z_][A-Za-z0-9_]*\s*\(\s*\)\s*\{"
)
UNQUALIFIED_RUN_CALL_PATTERN = re.compile(r"(?<![A-Za-z0-9_.])Run\s*\(")
SYNTHETIC_SCALE_SMOKES = 2048


def build_run_reference_index(sources):
    """Index ClassName.Run(...) call sites with one scan of each source file."""
    references = {}
    source_scans = 0
    for path, text in sources.items():
        source_scans += 1
        for match in RUN_CALL_PATTERN.finditer(text):
            references.setdefault(match.group(1), set()).add(path)
    return references, source_scans


def find_matching_brace(text, opening_index):
    depth = 0
    for index in range(opening_index, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return index
    return None


def has_module_initializer_run_call(text, class_name):
    qualified_run = re.compile(r"\b" + re.escape(class_name) + r"\s*\.\s*Run\s*\(")
    for match in MODULE_INITIALIZER_METHOD_PATTERN.finditer(text):
        opening_index = match.end() - 1
        closing_index = find_matching_brace(text, opening_index)
        if closing_index is None:
            continue
        body = text[opening_index + 1 : closing_index]
        if UNQUALIFIED_RUN_CALL_PATTERN.search(body) or qualified_run.search(body):
            return True
    return False


def find_registration_errors(sources):
    references, source_scans = build_run_reference_index(sources)
    errors = []
    checked = 0

    for path, text in sorted(sources.items(), key=lambda item: (item[0].name.casefold(), item[0].name)):
        if not path.name.endswith("Smoke.cs"):
            continue
        if not RUN_PATTERN.search(text):
            continue
        match = CLASS_PATTERN.search(text)
        if not match:
            errors.append(path.name + ": Run() exists but no smoke class could be identified")
            continue
        class_name = match.group(1)
        checked += 1

        # A self-registration exemption is valid only when a ModuleInitializer
        # method in this source actually invokes the smoke Run() method.
        if has_module_initializer_run_call(text, class_name):
            continue
        registration_paths = references.get(class_name, ())
        if not any(other_path != path for other_path in registration_paths):
            errors.append(path.name + ": " + class_name + ".Run() is never registered or invoked")

    return checked, errors, source_scans


def verify_index_regression():
    """Deterministically lock semantics and one-pass behavior at repository scale."""
    small_sources = {
        Path("RegisteredSmoke.cs"): (
            "internal static class RegisteredSmoke { internal static void Run() { } }"
        ),
        Path("RegisteredSmokeRegistration.cs"): (
            "internal static class RegisteredSmokeRegistration { "
            "internal static void Register() { RegisteredSmoke.Run(); } }"
        ),
        Path("InitializerSmoke.cs"): (
            "internal static class InitializerSmoke { [ModuleInitializer] "
            "internal static void Register() { Run(); } internal static void Run() { } }"
        ),
        Path("FalseInitializerSmoke.cs"): (
            "internal static class FalseInitializerSmoke { [ModuleInitializer] "
            "internal static void Register() { } internal static void Run() { } }"
        ),
        Path("MissingSmoke.cs"): (
            "internal static class MissingSmoke { internal static void Run() { } }"
        ),
        Path("SelfOnlySmoke.cs"): (
            "internal static class SelfOnlySmoke { internal static void Run() { SelfOnlySmoke.Run(); } }"
        ),
    }
    checked, errors, source_scans = find_registration_errors(small_sources)
    expected_errors = {
        "FalseInitializerSmoke.cs: FalseInitializerSmoke.Run() is never registered or invoked",
        "MissingSmoke.cs: MissingSmoke.Run() is never registered or invoked",
        "SelfOnlySmoke.cs: SelfOnlySmoke.Run() is never registered or invoked",
    }
    if checked != 5 or set(errors) != expected_errors or source_scans != len(small_sources):
        raise RuntimeError("smoke-registration semantic regression self-check failed")

    # Exercise more detached source records than the current repository while
    # asserting that reference indexing visits each source exactly once. This
    # is a deterministic complexity guard; it does not depend on wall-clock timing.
    scale_sources = {}
    for index in range(SYNTHETIC_SCALE_SMOKES):
        class_name = "Scale" + str(index).zfill(4) + "Smoke"
        scale_sources[Path(class_name + ".cs")] = (
            "internal static class " + class_name + " { internal static void Run() { } }"
        )
        scale_sources[Path("Scale" + str(index).zfill(4) + "Registration.cs")] = (
            "internal static class Scale" + str(index).zfill(4) + "Registration { "
            "internal static void Register() { " + class_name + ".Run(); } }"
        )

    checked, errors, source_scans = find_registration_errors(scale_sources)
    if checked != SYNTHETIC_SCALE_SMOKES or errors or source_scans != len(scale_sources):
        raise RuntimeError("smoke-registration scale/index regression self-check failed")


def main():
    try:
        verify_index_regression()
    except RuntimeError as exc:
        print("ERROR:", exc)
        return 1

    if not TESTS.is_dir():
        print("ERROR: missing tests/QS3D.Core.SmokeTests")
        return 1

    sources = {path: path.read_text(encoding="utf-8") for path in TESTS.glob("*.cs")}
    checked, errors, source_scans = find_registration_errors(sources)

    # Lock the known historical regression that motivated this repository-wide guard.
    beam_registration = TESTS / "BeamRebarSmokeRegistration.cs"
    if not beam_registration.is_file():
        errors.append("missing BeamRebarSmokeRegistration.cs")
    else:
        text = beam_registration.read_text(encoding="utf-8")
        for needle in ("[ModuleInitializer]", "BeamRebarRegressionSmoke.Run()"):
            if needle not in text:
                errors.append("Beam rebar smoke registration missing: " + needle)

    print("QS3D smoke registration preflight")
    print("Indexed Run() call sites from", source_scans, "source file(s) in one pass.")
    print("Checked", checked, "smoke class(es) exposing static Run().")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with", len(errors), "error(s).")
        return 1
    print("PASS: every runnable smoke class is self-registered or referenced by another test registration source.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
