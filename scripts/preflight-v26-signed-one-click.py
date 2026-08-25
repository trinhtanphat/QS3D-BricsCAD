#!/usr/bin/env python3
"""Fail-closed source guard for the V26 signed one-click update qualification lane."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    p = ROOT / path
    if not p.is_file():
        raise SystemExit(f"ERROR: missing required source: {path}")
    return p.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: V26 signed one-click guard lost {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"ERROR: V26 signed one-click guard found forbidden {label}: {token}")


def main() -> int:
    release = read("src/QS3D.BricsCAD.V26/Updates/GitHubReleaseClient.cs")
    manifest = read("src/QS3D.BricsCAD.V26/Updates/UpdateManifestProbe.cs")
    launcher = read("src/QS3D.BricsCAD.V26/Updates/SecureUpdateLauncher.cs")
    updater = read("scripts/update-v25.ps1")
    v26_generator = read("scripts/new-v26-script-from-v25.ps1")

    require(release, 'UpdateManifestAssetName = "QS3D-BricsCAD-V26.update.json"', "V26-only manifest asset")
    require(manifest, 'private const string Target = "BricsCAD V26 x64"', "V26 manifest target")
    require(manifest, 'private const int MaxManifestBytes = 64 * 1024', "bounded manifest")
    require(manifest, 'Uri.UriSchemeHttps', "HTTPS manifest/package contract")
    require(manifest, 'QS3D-BricsCAD-V26.zip', "V26 package asset")
    require(manifest, 'SignerThumbprint', "manifest signer binding")
    require(manifest, 'Sha256', "manifest package hash binding")

    require(launcher, 'TryGetCurrentSignerThumbprint', "running-plugin Authenticode gate")
    require(launcher, 'TryVerifyAuthenticode', "Authenticode verification")
    require(launcher, 'Global\\\\QS3D-BricsCAD-V26-Update-', "per-user V26 update mutex")
    require(launcher, 'update-v26.ps1', "V26 updater isolation")
    require(launcher, 'WorkerReadyTimeoutMilliseconds = 5000', "detached-worker readiness bound")
    require(launcher, 'TryResolveRegisteredLoadMode', "registered install identity")
    require(launcher, 'TryRequestGracefulHostClose', "graceful host-close boundary")
    forbid(launcher, 'update-v25.ps1', "V25 updater cross-use")

    require(updater, 'ExpectedSignerThumbprint', "updater signer input")
    require(updater, 'PackageUri', "updater package URI input")
    require(v26_generator, 'update-v26.ps1', "generated V26 updater")

    print("V26 signed one-click update source guard PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
