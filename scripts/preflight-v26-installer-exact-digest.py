#!/usr/bin/env python3
"""Fail closed if V26 installer acquisition is not bound to one exact SHA-256."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"


def installer_cache_job(text: str) -> str:
    match = re.search(r"(?ms)^  installer-cache:\s*\n(.*?)(?=^  [A-Za-z0-9_-]+:\s*$|\Z)", text)
    return "" if match is None else match.group(1)


def validate(text: str) -> list[str]:
    errors: list[str] = []
    job = installer_cache_job(text)
    if not job:
        return ["V26 cloud workflow must retain the installer-cache job"]

    gate = re.search(
        r"(?ms)^      - name: Validate exact BricsCAD V26\.2\.07 installer digest\s*$"
        r".*?^        run: \|\s*$"
        r"(?P<body>(?:^          .*\n?)+)",
        job,
    )
    if gate is None:
        errors.append("installer-cache must validate an exact V26 installer digest before cache restore/acquisition")
    else:
        body = gate.group("body")
        if "BRICSCAD_V26_PINNED_MSI_SHA256" not in body:
            errors.append("V26 installer digest gate must validate the effective pinned digest environment value")
        if "^[0-9A-Fa-f]{64}$" not in body and "^[0-9A-F]{64}$" not in body:
            errors.append("V26 installer digest gate must require exactly 64 hexadecimal SHA-256 characters")

    gate_pos = job.find("- name: Validate exact BricsCAD V26.2.07 installer digest")
    restore_pos = job.find("- name: Restore BricsCAD V26.2.07 installer cache")
    acquire_pos = job.find("- name: Acquire and admit BricsCAD V26.2.07 installer")
    if gate_pos < 0 or restore_pos < 0 or acquire_pos < 0 or not (gate_pos < restore_pos < acquire_pos):
        errors.append("exact digest admission must run before installer cache restore and acquisition")

    restore = re.search(
        r"(?ms)^      - name: Restore BricsCAD V26\.2\.07 installer cache\s*$.*?(?=^      - name:|\Z)",
        job,
    )
    if restore is None:
        errors.append("V26 installer cache restore step is missing")
    else:
        restore_text = restore.group(0)
        if "|| 'mirror'" in restore_text or '|| "mirror"' in restore_text:
            errors.append("V26 installer cache key must not fall back to an unbound mirror identity")
        if re.search(r"(?m)^          restore-keys:\s*\|?\s*$", restore_text):
            errors.append("V26 installer cache restore must not use a broad prefix fallback across installer digests")

    save = re.search(
        r"(?ms)^      - name: Save digest-bound BricsCAD V26\.2\.07 installer cache\s*$.*?(?=^      - name:|\Z)",
        job,
    )
    if save is None or "steps.acquire.outputs.sha256" not in save.group(0):
        errors.append("V26 installer cache save key must remain bound to the admitted installer digest")

    return errors


def safe_baseline(text: str) -> str:
    job = installer_cache_job(text)
    if not job:
        return text
    if "- name: Validate exact BricsCAD V26.2.07 installer digest" not in job:
        marker = "      - name: Restore BricsCAD V26.2.07 installer cache\n"
        gate = (
            "      - name: Validate exact BricsCAD V26.2.07 installer digest\n"
            "        if: ${{ inputs.prime_installer_cache || inputs.confirm_release == 'RELEASE' }}\n"
            "        shell: powershell\n"
            "        run: |\n"
            "          $expected = ([string]$env:BRICSCAD_V26_PINNED_MSI_SHA256).Trim()\n"
            "          if ($expected -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Exact V26 installer SHA-256 is required.' }\n\n"
        )
        text = text.replace(marker, gate + marker, 1)
    text = text.replace(" || 'mirror'", "", 1)
    text = re.sub(
        r"(?m)^          restore-keys:\s*\|\s*\n(?:            .*\n)+",
        "",
        text,
        count=1,
    )
    return text


def require_mutation_rejection(source: str, mutated: str, label: str) -> None:
    if mutated == source:
        raise AssertionError("mutation probe could not modify safe digest baseline: " + label)
    if not validate(mutated):
        raise AssertionError("mutation probe was not rejected: " + label)


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    errors = validate(text)

    safe = safe_baseline(text)
    if validate(safe):
        errors.append("mutation harness could not synthesize a safe exact-digest baseline")
    else:
        require_mutation_rejection(
            safe,
            safe.replace("^[0-9A-Fa-f]{64}$", ".*", 1),
            "non-exact digest admission",
        )
        require_mutation_rejection(
            safe,
            safe.replace("-${{ inputs.installer_sha256 || vars.BRICSCAD_V26_MSI_SHA256 }}", "-${{ inputs.installer_sha256 || vars.BRICSCAD_V26_MSI_SHA256 || 'mirror' }}", 1),
            "unbound cache-key fallback",
        )
        restore_marker = "          key: bricscad-v26.2.07-x64-en-us-${{ inputs.installer_sha256 || vars.BRICSCAD_V26_MSI_SHA256 }}\n"
        require_mutation_rejection(
            safe,
            safe.replace(restore_marker, restore_marker + "          restore-keys: |\n            bricscad-v26.2.07-x64-en-us-\n", 1),
            "cross-digest cache prefix fallback",
        )

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("PASS: V26 installer publication is bound to one exact SHA-256 before cache restore and acquisition.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
