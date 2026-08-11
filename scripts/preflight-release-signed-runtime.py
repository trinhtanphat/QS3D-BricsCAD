#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def main() -> int:
    if not WORKFLOW.is_file():
        raise AssertionError("missing release-v25.yml")
    text = WORKFLOW.read_text(encoding="utf-8")

    require(text, "Stable release requires run_runtime=true.", "stable runtime requirement")
    require(text, "Stable release requires sign_package=true.", "stable signing requirement")
    require(text, "- name: Real V25 runtime validation for unsigned preview payload", "unsigned runtime branch")
    require(text, "if: ${{ inputs.run_runtime && !inputs.sign_package }}", "unsigned runtime condition")
    require(text, "- name: Real V25 runtime validation for signed release payload", "signed runtime branch")
    require(text, "if: ${{ inputs.run_runtime && inputs.sign_package }}", "signed runtime condition")
    require(text, 'Resolve-Path "dist\\QS3D-BricsCAD-V25\\QS3D.BricsCAD.V25.dll"', "exact signed staged plugin runtime target")
    require(text, "artifacts\\bricscad-v25-runtime-signed", "signed runtime evidence folder")
    require(text, "artifacts\\bricscad-v25-runtime-unsigned", "unsigned runtime evidence folder")

    sign = text.find("- name: Authenticode-sign V25 executable payload")
    verify = text.find("- name: Verify Authenticode publisher and timestamp")
    finalize = text.find("- name: Finalize signed V25 package")
    signed_runtime = text.find("- name: Real V25 runtime validation for signed release payload")
    manifest = text.find("- name: Create signed auto-update manifest")
    publish = text.find("- name: Publish GitHub Release")
    ordered = (sign, verify, finalize, signed_runtime, manifest, publish)
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        raise AssertionError("signed release must sign -> verify -> finalize -> runtime-test signed dist payload -> create manifest -> publish")

    signed_block = text[signed_runtime:manifest]
    if "src\\QS3D.BricsCAD.V25\\bin" in signed_block:
        raise AssertionError("signed runtime branch must not test the pre-sign build output")
    if "dist\\QS3D-BricsCAD-V25\\QS3D.BricsCAD.V25.dll" not in signed_block:
        raise AssertionError("signed runtime branch must test the finalized staged plugin")

    unsigned_runtime = text.find("- name: Real V25 runtime validation for unsigned preview payload")
    package = text.find("- name: Build V25 release package")
    if unsigned_runtime < 0 or package < 0 or unsigned_runtime >= package:
        raise AssertionError("unsigned preview runtime branch must remain before package/signing")

    require(text, "passed required V25 runtime gate on the exact signed release plugin payload", "release-note signed-runtime identity")

    print("PASS: signed releases runtime-test the exact finalized QS3D plugin payload before manifest generation/publication; unsigned preview runtime remains a separate pre-sign path.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
