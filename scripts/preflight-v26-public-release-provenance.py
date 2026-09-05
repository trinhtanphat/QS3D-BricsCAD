from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def main() -> None:
    text = PUBLISHER.read_text(encoding="utf-8")
    provenance = "dist\\QS3D-BricsCAD-V26.provenance.json"
    public_name = "QS3D-BricsCAD-V26.provenance.json"

    require(provenance in text, "V26 publisher must map the qualified provenance file into release assets")
    require(
        text.count(public_name) >= 2,
        "V26 provenance must participate in both upload and expected published-asset identity sets",
    )
    require(
        "$verifiedAssetIds[$expectedAsset] = $uploadedAssetId" in text,
        "V26 publisher must retain verified remote asset IDs before publication",
    )
    require(
        "Invoke-WebRequest -Method Get -Uri ([string]$uploadedAsset.url)" in text,
        "V26 publisher must download uploaded assets for byte verification before publication",
    )
    require(
        "Assert-PublishedReleaseMatchesVerifiedTransaction" in text,
        "V26 publisher must reconcile final publication against the verified asset identity set",
    )

    print("PASS V26 public release provenance contract")


if __name__ == "__main__":
    main()
