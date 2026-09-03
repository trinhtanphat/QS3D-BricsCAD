#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INSTALLER_REL = "src/QS3D.BricsCAD.V25/Updates/VerifiedPreviewInstaller.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(text: str, needle: str) -> None:
    if needle not in text:
        fail(f"{INSTALLER_REL} missing preview-worker lifetime contract: {needle}")


def forbid(text: str, needle: str) -> None:
    if needle in text:
        fail(f"{INSTALLER_REL} still contains unsafe preview-worker launch: {needle}")


def main() -> int:
    path = ROOT / INSTALLER_REL
    if not path.is_file():
        fail(f"missing {INSTALLER_REL}")
    text = path.read_text(encoding="utf-8")

    # The updater must outlive BricsCAD. A plain Process.Start child can inherit a
    # kill-on-close Job Object from the CAD host, so a successful 5s startup probe
    # does not prove the worker will survive long enough to perform post-exit replace.
    for needle in (
        "CreateBreakawayFromJob",
        "CreateUnicodeEnvironment",
        "CreateNoWindow",
        "CreateProcess(",
        "TryStartBreakawayWorker",
        "BuildEnvironmentBlock",
        "CloseHandle(processInformation.hThread)",
        "CloseHandle(processInformation.hProcess)",
        "Process.GetProcessById(processInformation.dwProcessId)",
        "Không thể tạo updater worker độc lập khỏi vòng đời BricsCAD",
    ):
        require(text, needle)

    # The verified-preview path must not silently fall back to an ordinary child;
    # if breakaway creation is denied, staging fails closed while BricsCAD stays up.
    forbid(text, "using (var worker = Process.Start(startInfo))")

    # Existing safety boundaries must remain present while the launch primitive changes.
    for needle in (
        "StageVerifiedPayload(",
        "ComputeSha256(packagePath)",
        "File.Copy(sourcePath, backupPath, true)",
        "WaitForExit",
        "File.Replace",
        "Rollback",
        "Restart-BricsCAD",
        "Assert-Hash $env:QS3D_PREVIEW_V25_DEST",
        "Assert-Hash $env:QS3D_PREVIEW_CORE_DEST",
    ):
        require(text, needle)

    print("PASS: V25 verified-preview worker uses an explicit fail-closed Windows breakaway launch and preserves hash/backup/replace/rollback/restart contracts.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
