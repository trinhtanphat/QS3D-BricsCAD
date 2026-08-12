#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedCurtainFrameHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"CURTAIN_FRAME_CONFIG_INVALID"',
        "HealthSeverity.Warning",
        '"Không thể kiểm tra curtain-frame config hiện tại vì semantic/family config không hợp lệ."',
        "catch (Exception ex) when (IsConfigDataFailure(ex))",
        "exception is InvalidOperationException || exception is ArgumentException",
        "CurtainWallFrameFingerprint.Compute",
        '"CURTAIN_FRAME_CONFIG_STALE"',
        '"CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT"',
        '"CURTAIN_FRAME_GENERATED_STALE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing curtain-frame config health isolation token: " + token)

    forbidden = (
        '"Không thể kiểm tra curtain-frame config hiện tại: " + ex.Message',
        "+ ex.Message",
        "catch (Exception ex)\n",
        "catch (Exception ex)\r\n",
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "project.Touch(",
        ".Save(",
        ".Erase(",
    )
    for token in forbidden:
        if token in text:
            errors.append("curtain-frame health regressed config redaction/read-only contract: " + token)

print("QS3D curtain-frame Core health config-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: curtain-frame config data failures are bounded, redacted, and read-only.")
