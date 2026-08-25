#!/usr/bin/env python3
import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "scripts" / "export-local-v25-sanitized-summary.py"


def fail(message: str) -> None:
    print(f"::error::{message}")
    raise SystemExit(1)


def require(text: str, needle: str) -> None:
    if needle not in text:
        fail(f"sanitized-summary I/O contract missing: {needle}")


def load_exporter():
    spec = importlib.util.spec_from_file_location("qs3d_sanitized_summary", EXPORTER)
    if spec is None or spec.loader is None:
        fail("could not load sanitized-summary exporter module")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run_main(module, source: Path, destination: Path):
    stdout = io.StringIO()
    stderr = io.StringIO()
    with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
        code = module.main(["--input", str(source), "--output", str(destination)])
    return code, stdout.getvalue(), stderr.getvalue()


def valid_report():
    return {
        "status": "PASS",
        "sourceBuildStatus": "PASS",
        "runtimeSmokeStatus": "NOT_RUN",
        "fullInteractiveMatrixStatus": "NOT_RUN",
        "qualificationScope": "source-build",
        "exactSha": "1" * 40,
        "pluginSha256": "2" * 64,
        "branch": "agent/private-machine-name",
        "releaseTag": "v1.2.3-preview.4",
        "runtimeSkipped": True,
        "packageRequested": False,
        "customerReleaseQualified": False,
        "steps": [
            {"name": "Core Release build", "status": "PASS"},
            {"name": "private local fixture name", "status": "FAIL"},
        ],
    }


def assert_failure_preserves(module, source: Path, destination: Path, label: str):
    destination.write_text("existing-summary\n", encoding="utf-8")
    code, _stdout, _stderr = run_main(module, source, destination)
    if code != 2:
        fail(f"{label}: expected exporter failure, got exit {code}")
    if destination.read_text(encoding="utf-8") != "existing-summary\n":
        fail(f"{label}: failed export modified the existing sanitized summary")


def test_static_contract(source_text: str) -> None:
    for needle in (
        "MAX_QUALIFICATION_JSON_BYTES = 1024 * 1024",
        "path.lstat()",
        "stat.S_ISREG",
        "stat.S_ISDIR",
        "_is_reparse_point",
        "stream.read(MAX_QUALIFICATION_JSON_BYTES + 1)",
        "payload.decode(\"utf-8-sig\")",
        "before.st_size != after.st_size",
        "before.st_dev != final_info.st_dev",
        "before.st_ino != final_info.st_ino",
        "tempfile.mkstemp",
        "os.fsync",
        "os.replace(temp_path, destination)",
        "temp_path.unlink(missing_ok=True)",
    ):
        require(source_text, needle)
    if "source.read_text(" in source_text:
        fail("qualification input must not regress to unbounded Path.read_text")
    if "destination.write_text(" in source_text:
        fail("sanitized summary publication must not regress to direct Path.write_text")


def test_runtime_contract(module) -> None:
    with tempfile.TemporaryDirectory(prefix="qs3d-sanitized-summary-") as raw_root:
        root = Path(raw_root)
        source = root / "qualification.json"
        destination = root / "out" / "qualification-summary.md"
        source.write_text(json.dumps(valid_report()), encoding="utf-8")

        code, stdout, stderr = run_main(module, source, destination)
        if code != 0:
            fail(f"valid sanitized-summary export failed: {stderr.strip()}")
        if "Sanitized V25 handoff written." not in stdout:
            fail("valid sanitized-summary export lost bounded success diagnostic")
        summary = destination.read_text(encoding="utf-8")
        if "`1111111111111111111111111111111111111111`" not in summary:
            fail("valid sanitized-summary export lost exact SHA")
        if "(redacted non-main branch)" not in summary:
            fail("valid sanitized-summary export leaked a non-main branch")
        if "private local fixture name" in summary:
            fail("valid sanitized-summary export leaked a non-allowlisted step label")

        oversize = root / "oversize.json"
        oversize.write_bytes(b" " * (module.MAX_QUALIFICATION_JSON_BYTES + 1))
        assert_failure_preserves(module, oversize, destination, "oversized input")

        invalid_utf8 = root / "invalid-utf8.json"
        invalid_utf8.write_bytes(b"\xff\xfe\xfa")
        assert_failure_preserves(module, invalid_utf8, destination, "invalid UTF-8")

        invalid_json = root / "invalid-json.json"
        invalid_json.write_text("{", encoding="utf-8")
        assert_failure_preserves(module, invalid_json, destination, "invalid JSON")

        alias = root / "alias.json"
        original = json.dumps(valid_report())
        alias.write_text(original, encoding="utf-8")
        code, _stdout, _stderr = run_main(module, alias, alias)
        if code != 2 or alias.read_text(encoding="utf-8") != original:
            fail("input/output alias must fail without modifying the qualification report")

        existing = root / "atomic.md"
        existing.write_text("atomic-sentinel\n", encoding="utf-8")
        with mock.patch.object(module.os, "replace", side_effect=OSError("injected replace failure")):
            try:
                module.write_summary_atomically(existing, "replacement\n")
            except ValueError:
                pass
            else:
                fail("injected atomic replace failure did not fail closed")
        if existing.read_text(encoding="utf-8") != "atomic-sentinel\n":
            fail("atomic replace failure modified the existing destination")
        leftovers = list(root.glob(f".{existing.name}.*.tmp"))
        if leftovers:
            fail("atomic replace failure left sibling temporary files behind")

        real_input = root / "real-input.json"
        real_input.write_text(json.dumps(valid_report()), encoding="utf-8")
        input_link = root / "input-link.json"
        try:
            input_link.symlink_to(real_input)
        except (OSError, NotImplementedError):
            input_link = None
        if input_link is not None:
            assert_failure_preserves(module, input_link, destination, "symlink input")

        real_output = root / "real-output.md"
        real_output.write_text("target-sentinel\n", encoding="utf-8")
        output_link = root / "output-link.md"
        try:
            output_link.symlink_to(real_output)
        except (OSError, NotImplementedError):
            output_link = None
        if output_link is not None:
            code, _stdout, _stderr = run_main(module, source, output_link)
            if code != 2:
                fail("symlink output must fail closed")
            if real_output.read_text(encoding="utf-8") != "target-sentinel\n":
                fail("symlink output failure modified the symlink target")


def main() -> int:
    source_text = EXPORTER.read_text(encoding="utf-8")
    test_static_contract(source_text)
    module = load_exporter()
    test_runtime_contract(module)
    print("Local V25 sanitized-summary I/O safety preflight PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
