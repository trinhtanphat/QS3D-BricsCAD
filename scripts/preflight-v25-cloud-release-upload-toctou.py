#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
HELPER = ROOT / "scripts" / "upload-v25-held-release-asset.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def main() -> None:
    workflow = WORKFLOW.read_text(encoding="utf-8")
    require(
        "-InFile $asset" not in workflow,
        "cloud preview release must not hash a pathname and then reopen it through Invoke-RestMethod -InFile",
    )
    require(
        "upload-v25-held-release-asset.ps1" in workflow,
        "cloud preview release must delegate held-asset verification and upload to the single-stream helper",
    )
    require(HELPER.is_file(), "single-stream cloud release upload helper is missing")

    helper = HELPER.read_text(encoding="utf-8")
    for token, message in (
        ("[System.IO.File]::Open(", "helper must explicitly open the held asset once"),
        ("[System.IO.FileMode]::Open", "helper must use FileMode.Open"),
        ("[System.IO.FileAccess]::Read", "helper must open read-only"),
        ("[System.IO.FileShare]::Read", "helper must deny writers/deleters while verification and upload are in flight"),
        ("ComputeHash($stream)", "helper must hash the same open stream"),
        ("$stream.Position = 0", "helper must rewind the verified stream before upload"),
        ("[System.Net.Http.StreamContent]::new($stream)", "helper must upload the same verified stream"),
        ("$client.PostAsync($uploadUri, $content)", "helper must perform publication from the verified StreamContent"),
    ):
        require(token in helper, message)

    require(
        "Invoke-RestMethod" not in helper and "-InFile" not in helper,
        "single-stream helper must not fall back to pathname-reopening upload APIs",
    )
    require(
        "ExpectedSha256" in helper and "ExpectedSize" in helper,
        "single-stream helper must retain expected hash and size admission",
    )
    require(
        "ReadAsStringAsync" not in helper and "$responseBody" not in helper,
        "upload failures must not copy an untrusted GitHub response body into exception/log output",
    )

    print("PASS: cloud preview held assets are verified/uploaded from one writer-blocking stream and failures redact response bodies")


if __name__ == "__main__":
    main()
