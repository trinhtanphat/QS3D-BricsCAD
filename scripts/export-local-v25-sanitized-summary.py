#!/usr/bin/env python3
import argparse
import json
import os
import re
import stat
import sys
import tempfile
from pathlib import Path

SHA40 = re.compile(r"^[0-9a-fA-F]{40}$")
SHA256 = re.compile(r"^[0-9a-fA-F]{64}$")
SEMVER_IDENTIFIER = re.compile(r"^[0-9A-Za-z-]+$")
ALLOWED_STATUS = {"PASS", "FAIL", "SKIPPED", "NOT_RUN", "FAIL_OR_INCOMPLETE"}
SAFE_PUBLIC_BRANCHES = frozenset({"main", "master", "HEAD"})
SAFE_PRERELEASE_CHANNELS = frozenset({"preview", "alpha", "beta", "rc"})
SAFE_QUALIFICATION_SCOPES = frozenset(
    {
        "incomplete",
        "source-build",
        "source-build+runtime-smoke",
        "source-build+runtime-smoke+package",
        "source-build+runtime-smoke+package+authenticode",
    }
)
SAFE_STEP_NAMES = frozenset(
    {
        "Exact Git SHA / clean tree",
        "Manual-only CI policy",
        "Generic source preflight",
        "Aggregate feature preflights",
        "Core Release build",
        "Core deterministic smoke suite",
        "BricsCAD V25 adapter Release build",
        "Offline WPF theme / Workspace / RightPanel smoke",
        "Licensed V25 NETLOAD / Ribbon / Palette runtime probe",
        "Build local V25 package",
        "Authenticode sign packaged executable payload",
        "Verify Authenticode signer and trusted timestamp",
        "Finalize signed package metadata / hashes / ZIP",
    }
)
MAX_QUALIFICATION_JSON_BYTES = 1024 * 1024
_REPARSE_POINT = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400)


def normalized_status(value, fallback="UNKNOWN"):
    text = str(value or "").strip().upper()
    return text if text in ALLOWED_STATUS else fallback


def sanitized_qualification_scope(value):
    text = str(value or "").strip()
    return text if text in SAFE_QUALIFICATION_SCOPES else "legacy-or-unknown"


def sanitized_branch(value):
    text = str(value or "").strip()
    if not text:
        return "(not recorded)"
    return text if text in SAFE_PUBLIC_BRANCHES else "(redacted non-main branch)"


def sanitized_release_tag(value):
    text = str(value or "").strip()
    if not text:
        return "(none)"
    if not text.startswith("v"):
        return "(redacted release tag)"

    version = text[1:]
    if version.count("+") > 1:
        return "(redacted release tag)"
    version_core, plus, build = version.partition("+")
    if version_core.count("-") > 1:
        return "(redacted release tag)"
    core, dash, prerelease = version_core.partition("-")

    core_parts = core.split(".")
    if len(core_parts) != 3:
        return "(redacted release tag)"
    for part in core_parts:
        if not part.isdigit() or (len(part) > 1 and part.startswith("0")):
            return "(redacted release tag)"

    if dash:
        prerelease_parts = prerelease.split(".")
        if any(not part or not SEMVER_IDENTIFIER.fullmatch(part) for part in prerelease_parts):
            return "(redacted release tag)"
        if any(part.isdigit() and len(part) > 1 and part.startswith("0") for part in prerelease_parts):
            return "(redacted release tag)"
        if prerelease_parts[0] not in SAFE_PRERELEASE_CHANNELS:
            return "(redacted release tag)"
        if any(not part.isdigit() for part in prerelease_parts[1:]):
            return "(redacted release tag)"

    if plus:
        build_parts = build.split(".")
        if any(not part or not SEMVER_IDENTIFIER.fullmatch(part) for part in build_parts):
            return "(redacted release tag)"
        if any(not part.isdigit() for part in build_parts):
            return "(redacted release tag)"

    return text


def sanitized_step_name(value, ordinal):
    text = str(value or "").strip()
    if text in SAFE_STEP_NAMES:
        return text
    return f"Step {ordinal} (redacted label)"


def yes_no(value):
    return "YES" if bool(value) else "NO"


def yes_no_unknown(report, key):
    value = report.get(key)
    return "YES" if value is True else "NO" if value is False else "UNKNOWN"


def _is_reparse_point(info):
    return bool(getattr(info, "st_file_attributes", 0) & _REPARSE_POINT)


def _require_ordinary_file(path, label):
    try:
        info = path.lstat()
    except OSError as exc:
        raise ValueError(f"{label} is unavailable: {exc.__class__.__name__}") from exc
    if path.is_symlink() or _is_reparse_point(info) or not stat.S_ISREG(info.st_mode):
        raise ValueError(f"{label} must be an ordinary non-reparse file")
    return info


def _require_ordinary_directory(path, label):
    try:
        info = path.lstat()
    except OSError as exc:
        raise ValueError(f"{label} is unavailable: {exc.__class__.__name__}") from exc
    if path.is_symlink() or _is_reparse_point(info) or not stat.S_ISDIR(info.st_mode):
        raise ValueError(f"{label} must be an ordinary non-reparse directory")
    return info


def _ensure_safe_output_parent(parent):
    missing = []
    cursor = parent
    while not cursor.exists():
        missing.append(cursor)
        next_cursor = cursor.parent
        if next_cursor == cursor:
            raise ValueError("sanitized summary output has no safe existing directory ancestor")
        cursor = next_cursor
    _require_ordinary_directory(cursor, "sanitized summary output ancestor")

    for directory in reversed(missing):
        directory.mkdir(exist_ok=True)
        _require_ordinary_directory(directory, "sanitized summary output directory")
    _require_ordinary_directory(parent, "sanitized summary output directory")


def _same_file_identity(left, right):
    try:
        if right.exists():
            return left.samefile(right)
    except OSError as exc:
        raise ValueError(f"could not compare qualification input/output identities: {exc.__class__.__name__}") from exc
    left_key = os.path.normcase(os.path.abspath(os.fspath(left)))
    right_key = os.path.normcase(os.path.abspath(os.fspath(right)))
    return left_key == right_key


def read_bounded_qualification_report(source):
    path_info = _require_ordinary_file(source, "qualification report")
    if path_info.st_size > MAX_QUALIFICATION_JSON_BYTES:
        raise ValueError("qualification report exceeds the 1 MiB input limit")

    try:
        with source.open("rb", buffering=0) as stream:
            before = os.fstat(stream.fileno())
            if _is_reparse_point(before) or not stat.S_ISREG(before.st_mode):
                raise ValueError("qualification report must be an ordinary non-reparse file")
            if before.st_size > MAX_QUALIFICATION_JSON_BYTES:
                raise ValueError("qualification report exceeds the 1 MiB input limit")
            payload = stream.read(MAX_QUALIFICATION_JSON_BYTES + 1)
            after = os.fstat(stream.fileno())
    except ValueError:
        raise
    except OSError as exc:
        raise ValueError(f"could not read qualification report safely: {exc.__class__.__name__}") from exc

    if len(payload) > MAX_QUALIFICATION_JSON_BYTES:
        raise ValueError("qualification report exceeds the 1 MiB input limit")
    if before.st_size != after.st_size or after.st_size != len(payload):
        raise ValueError("qualification report size changed while it was being read")

    try:
        final_info = source.lstat()
    except OSError as exc:
        raise ValueError(f"qualification report changed while it was being read: {exc.__class__.__name__}") from exc
    if source.is_symlink() or _is_reparse_point(final_info) or not stat.S_ISREG(final_info.st_mode):
        raise ValueError("qualification report changed to an unsafe file type while it was being read")
    if final_info.st_size != after.st_size:
        raise ValueError("qualification report size changed while it was being read")
    if before.st_dev != final_info.st_dev or before.st_ino != final_info.st_ino:
        raise ValueError("qualification report identity changed while it was being read")

    try:
        return payload.decode("utf-8-sig")
    except UnicodeDecodeError as exc:
        raise ValueError("qualification report is not strict UTF-8") from exc


def write_summary_atomically(destination, text):
    parent = destination.parent
    _ensure_safe_output_parent(parent)
    if destination.exists() or destination.is_symlink():
        _require_ordinary_file(destination, "sanitized summary output")

    encoded = text.encode("utf-8")
    descriptor = None
    temp_path = None
    try:
        descriptor, temp_name = tempfile.mkstemp(
            prefix=f".{destination.name}.",
            suffix=".tmp",
            dir=parent,
        )
        temp_path = Path(temp_name)
        with os.fdopen(descriptor, "wb") as stream:
            descriptor = None
            stream.write(encoded)
            stream.flush()
            os.fsync(stream.fileno())
        _require_ordinary_file(temp_path, "sanitized summary temporary file")
        if destination.exists() or destination.is_symlink():
            _require_ordinary_file(destination, "sanitized summary output")
        os.replace(temp_path, destination)
        temp_path = None
    except ValueError:
        raise
    except OSError as exc:
        raise ValueError(f"could not publish sanitized summary atomically: {exc.__class__.__name__}") from exc
    finally:
        if descriptor is not None:
            os.close(descriptor)
        if temp_path is not None:
            try:
                temp_path.unlink(missing_ok=True)
            except OSError:
                pass


def build_summary(report):
    automated_status = normalized_status(report.get("automatedGateStatus") or report.get("status"))
    source_build_status = normalized_status(report.get("sourceBuildStatus") or report.get("status"))
    runtime_smoke_status = normalized_status(
        report.get("runtimeSmokeStatus"),
        "NOT_RUN" if bool(report.get("runtimeSkipped")) else "UNKNOWN",
    )
    interactive_status = normalized_status(report.get("fullInteractiveMatrixStatus"), "NOT_RUN")
    qualification_scope = sanitized_qualification_scope(report.get("qualificationScope"))

    exact_sha = str(report.get("exactSha") or "").strip()
    if not SHA40.fullmatch(exact_sha):
        exact_sha = "UNRESOLVED"

    plugin_hash = str(report.get("pluginSha256") or "").strip().lower()
    if not SHA256.fullmatch(plugin_hash):
        plugin_hash = "NOT AVAILABLE"

    branch = sanitized_branch(report.get("branch"))
    release_tag = sanitized_release_tag(report.get("releaseTag"))
    runtime_skipped = bool(report.get("runtimeSkipped"))
    package_requested = bool(report.get("packageRequested"))
    customer_release_qualified = yes_no_unknown(report, "customerReleaseQualified")

    lines = [
        "# QS3D local V25 qualification — sanitized summary",
        "",
        "> Safe handoff generated from local qualification metadata. This summary intentionally omits local usernames, machine names, absolute paths, private DWG names/content, screenshots, credentials, and raw error messages.",
        "",
        f"- Automated gate status: **{automated_status}**",
        f"- Source/build status: **{source_build_status}**",
        f"- Runtime smoke status: **{runtime_smoke_status}**",
        f"- Full interactive/private-DWG matrix: **{interactive_status}**",
        f"- Customer release qualified: **{customer_release_qualified}**",
        f"- Qualification scope: `{qualification_scope}`",
        f"- Exact Git SHA: `{exact_sha}`",
        f"- Branch: `{branch}`",
        f"- Plugin SHA-256: `{plugin_hash}`",
        f"- Runtime skipped: **{yes_no(runtime_skipped)}**",
        f"- Package requested: **{yes_no(package_requested)}**",
        f"- Release tag: `{release_tag}`",
        "- Manual/private-DWG checklist: **NOT PROVED BY THIS SUMMARY** — execute `docs/LOCAL-V25-QUALIFICATION.md` on the same exact SHA.",
    ]

    if runtime_skipped:
        lines.extend([
            "",
            "**Release qualification warning:** runtime was explicitly skipped. This result cannot qualify a customer release.",
        ])
    if customer_release_qualified != "YES":
        lines.extend([
            "",
            "**Scope warning:** this automated/sanitized evidence is not proof of customer-release qualification. Complete and record the full interactive/private-DWG/product gates for the same SHA/package.",
        ])

    steps = report.get("steps")
    if isinstance(steps, list):
        lines.extend(["", "## Automated steps", "", "| Step | Status |", "|---|---|"])
        for ordinal, item in enumerate(steps, start=1):
            if not isinstance(item, dict):
                continue
            name = sanitized_step_name(item.get("name"), ordinal)
            step_status = normalized_status(item.get("status"))
            lines.append(f"| {name} | **{step_status}** |")

    lines.extend([
        "",
        "## Local agent completion fields",
        "",
        "Fill these manually only after testing the **same exact SHA**. Keep fixture names anonymized and do not paste private paths or raw customer drawing content.",
        "",
        "- BricsCAD V25 edition/build: `NOT RECORDED`",
        "- NETLOAD / runtime probe: `PASS | FAIL | SKIPPED`",
        "- DemandLoad: `PASS | FAIL | NOT TESTED`",
        "- Direct Draw: `PASS | FAIL | NOT TESTED`",
        "- Door / Opening booleans: `PASS | FAIL | NOT TESTED`",
        "- Room / HT_PHÒNG: `PASS | FAIL | NOT TESTED`",
        "- Curtain host + frame: `PASS | FAIL | NOT TESTED`",
        "- Curtain panel-by-panel: `PASS | FAIL | NOT IMPLEMENTED`",
        "- Physical L/T/X wall junction: `PASS | FAIL | NOT IMPLEMENTED`",
        "- Rebar geometry / atomicity: `PASS | FAIL | NOT TESTED`",
        "- Rebar governing standard/revision: `EXPLICIT VALUE | NOT QUALIFIED`",
        "- Rebar fabrication qualification: `PASS | FAIL | NOT QUALIFIED`",
        "- Save/reopen + multi-DWG: `PASS | FAIL | NOT TESTED`",
        "- Unicode / HiDPI: `PASS | FAIL | NOT TESTED`",
        "- Private-DWG regression: `PASS | FAIL | NOT TESTED`",
        "- Clean install / upgrade / uninstall: `PASS | FAIL | NOT TESTED`",
        "- Authenticode + timestamp: `PASS | FAIL | NOT SIGNED`",
        "- Known blockers: `SANITIZED TEXT ONLY`",
        "",
        "Do not change `FAIL`, `SKIPPED`, `NOT_RUN`, `NOT TESTED`, `NOT IMPLEMENTED`, or `NOT QUALIFIED` to PASS from source review alone.",
        "",
    ])
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description="Create a shareable, sanitized Markdown summary from QS3D local V25 qualification.json.")
    parser.add_argument(
        "--input",
        default="artifacts/local-v25-qualification/qualification.json",
        help="Path to local qualification.json (default: artifacts/local-v25-qualification/qualification.json)",
    )
    parser.add_argument(
        "--output",
        default="artifacts/local-v25-qualification/qualification-summary.md",
        help="Sanitized Markdown output path (default: artifacts/local-v25-qualification/qualification-summary.md)",
    )
    args = parser.parse_args(argv)

    source = Path(args.input)
    destination = Path(args.output)
    try:
        _require_ordinary_file(source, "qualification report")
        _ensure_safe_output_parent(destination.parent)
        if _same_file_identity(source, destination):
            raise ValueError("sanitized summary output must not alias the input qualification report")
        if destination.exists() or destination.is_symlink():
            _require_ordinary_file(destination, "sanitized summary output")
        report_text = read_bounded_qualification_report(source)
        report = json.loads(report_text)
        if not isinstance(report, dict):
            raise ValueError("qualification report root must be a JSON object")
        write_summary_atomically(destination, build_summary(report))
    except json.JSONDecodeError:
        print("ERROR: qualification report contains invalid JSON.", file=sys.stderr)
        return 2
    except ValueError as exc:
        print(f"ERROR: {exc}.", file=sys.stderr)
        return 2

    print("Sanitized V25 handoff written.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
