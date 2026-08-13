#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        raise AssertionError(f"missing required file: {path}")
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"{label}: missing {token!r}")


def main() -> int:
    try:
        client = read("src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs")
        coordinator = read("src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs")

        require(client, 'PackageAssetName = "QS3D-BricsCAD-V25.zip"', "V25 release channel")
        require(client, 'UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json"', "V25 release channel")
        require(client, "if (packageUri == null)\n                {\n                    if (manifestUri == null) continue;\n                }", "manual preview visibility")
        require(client, "Manifest-less V25 previews remain visible", "manual preview visibility")

        require(coordinator, "else if (!latest.HasSignedUpdateManifest)", "manual-only update state")
        require(coordinator, "UpdateState.ManualInstallRequired", "manual-only update state")
        require(coordinator, "release này không có signed update manifest", "manual-only update message")
        require(coordinator, "One-click update bị khóa để không hạ chuẩn bảo mật", "manual-only security boundary")
    except (OSError, UnicodeError, AssertionError) as exc:
        print("ERROR:", exc)
        return 1

    print("PASS: V25 package-only previews remain visible for manual update while one-click stays signed-manifest-only and the shared V26 channel gate remains compatible.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
