#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LAUNCHER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates" / "SecureUpdateLauncher.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing updater source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def main() -> int:
    launcher = read(LAUNCHER)

    require(launcher, 'script.AppendLine("$hostClosed = $false");', "worker host-closed state initialization")
    require(launcher, 'script.AppendLine("  $hostClosed = $true");', "post-wait host-closed transition")
    require(launcher, 'script.AppendLine("  $updateFailure = $_");', "original update failure capture")
    require(launcher, 'script.AppendLine("  Write-Error $updateFailure -ErrorAction Continue");', "non-terminating failure logging under ErrorActionPreference Stop")
    require(
        launcher,
        'script.AppendLine("  if ($hostClosed -and (Test-Path -LiteralPath $bricscad -PathType Leaf) -and -not (Get-Process -Name bricscad -ErrorAction SilentlyContinue)) {");',
        "failure restart guard",
    )
    require(launcher, 'script.AppendLine("    try { Start-Process -FilePath $bricscad | Out-Null }");', "failure recovery restart")
    require(launcher, "QS3D update failed and BricsCAD recovery restart also failed", "recovery restart warning")
    require(launcher, 'script.AppendLine("  exit 1");', "original failure exit code")

    host_false = launcher.find('$hostClosed = $false')
    cad_wait = launcher.find("while (Get-Process -Name bricscad")
    cancel_during_wait = launcher.find("cancelled while waiting for BricsCAD to close")
    host_true = launcher.find('$hostClosed = $true')
    updater_signature = launcher.find("Get-AuthenticodeSignature -LiteralPath $updater")
    updater_call = launcher.find("& $updater -ManifestUri $manifest")
    success_restart = launcher.find("Start-Process -FilePath $bricscad | Out-Null", updater_call)
    catch_marker = launcher.find('script.AppendLine("catch {")', success_restart)
    failure_capture = launcher.find('$updateFailure = $_', catch_marker)
    failure_log = launcher.find("Write-Error $updateFailure -ErrorAction Continue", catch_marker)
    failure_guard = launcher.find("if ($hostClosed -and (Test-Path -LiteralPath $bricscad -PathType Leaf)", catch_marker)
    failure_restart = launcher.find("try { Start-Process -FilePath $bricscad | Out-Null }", failure_guard)
    transcript_stop = launcher.find("try { Stop-Transcript | Out-Null } catch { }", failure_restart)
    failure_exit = launcher.find('script.AppendLine("  exit 1");', transcript_stop)

    ordered = (
        host_false,
        cad_wait,
        cancel_during_wait,
        host_true,
        updater_signature,
        updater_call,
        success_restart,
        catch_marker,
        failure_capture,
        failure_log,
        failure_guard,
        failure_restart,
        transcript_stop,
        failure_exit,
    )
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        raise AssertionError(
            "host-closed transition, update work, success restart, and guarded failure recovery must remain ordered"
        )

    if host_true <= cad_wait:
        raise AssertionError("hostClosed must not become true before the all-BricsCAD wait")
    if failure_guard <= failure_capture:
        raise AssertionError("catch must preserve/log the original update failure before recovery restart")

    require(launcher, "if ($cancelEvent.WaitOne(0)) { throw 'QS3D updater was cancelled while waiting for BricsCAD to close.' }", "pre-close cancellation remains active")
    require(launcher, "if ($cancelEvent.WaitOne(0)) { throw 'QS3D updater was cancelled before installer execution.' }", "pre-install cancellation remains active")
    require(launcher, "CloseMainWindow()", "graceful host-close request")

    kill_lines = [line.strip() for line in launcher.splitlines() if ".Kill(" in line]
    if kill_lines != ["updater.Kill();"]:
        raise AssertionError("only detached updater child may be killed: " + repr(kill_lines))
    if "Stop-Process" in launcher or "taskkill" in launcher or "process.Kill(" in launcher:
        raise AssertionError("BricsCAD/current-process force termination is forbidden")

    print(
        "PASS: updater restarts the exact captured BricsCAD executable after post-close failure only when the host was actually closed and remains absent; pre-close cancellation cannot create a duplicate host."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
