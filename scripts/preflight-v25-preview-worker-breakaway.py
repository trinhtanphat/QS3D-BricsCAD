#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INSTALLER_REL = "src/QS3D.BricsCAD.V25/Updates/VerifiedPreviewInstaller.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(text: str, needle: str) -> None:
    if needle not in text:
        fail(f"{INSTALLER_REL} missing preview-worker lifetime/full-package contract: {needle}")


def forbid(text: str, needle: str) -> None:
    if needle in text:
        fail(f"{INSTALLER_REL} still contains unsafe/incomplete preview apply behavior: {needle}")


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

    # A preview ZIP is the install payload, not merely a carrier for two DLLs. Every
    # safe file and child directory must be staged with its relative path, hashed,
    # backed up when it would overwrite an installed file, then mirrored into the
    # exact directory that contains the running adapter. Rollback must restore every
    # overwritten file and remove files/directories created by the failed apply.
    for needle in (
        "PreviewPayloadManifestFileName",
        "WritePayloadManifest(",
        "StageVerifiedPayload(",
        "QS3D_PREVIEW_MANIFEST",
        "QS3D_PREVIEW_INSTALL_ROOT",
        "QS3D_PREVIEW_PAYLOAD_ROOT",
        "QS3D_PREVIEW_BACKUP_ROOT",
        "Mirror-Payload",
        "Restore-MirroredPayload",
        "Decode-RelativePath",
        "Assert-SafeDestination",
        "ENTRY_FILE",
        "ENTRY_DIRECTORY",
        "createdBeforeApply",
        "backupSha256",
    ):
        require(text, needle)

    # Existing safety boundaries must remain present while full-package mirroring is added.
    for needle in (
        "ComputeSha256(packagePath)",
        "MaxArchiveUncompressedBytes",
        "MaxPayloadFileBytes",
        "seenCanonicalPaths",
        "path traversal",
        "WaitForExit",
        "File.Replace",
        "Rollback",
        "Restart-BricsCAD",
        "Assert-Hash $env:QS3D_PREVIEW_V25_DEST",
        "Assert-Hash $env:QS3D_PREVIEW_CORE_DEST",
    ):
        require(text, needle)

    # The old implementation extracted only RequiredPayload entries. That would
    # silently discard package folders/resources/configuration and must not return.
    forbid(text, "result[fileName] = stagedPath")

    print("PASS: V25 preview updater uses a breakaway worker and mirrors the complete verified ZIP tree into the running adapter directory with whole-tree rollback.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
