#!/usr/bin/env python3
import argparse
import json
import re
import sys
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
    if not source.is_file():
        print(f"ERROR: qualification report does not exist: {source}", file=sys.stderr)
        return 2

    try:
        report = json.loads(source.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"ERROR: could not read qualification report: {exc}", file=sys.stderr)
        return 2
    if not isinstance(report, dict):
        print("ERROR: qualification report root must be a JSON object.", file=sys.stderr)
        return 2

    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(build_summary(report), encoding="utf-8")
    print(f"Sanitized V25 handoff written: {destination}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
