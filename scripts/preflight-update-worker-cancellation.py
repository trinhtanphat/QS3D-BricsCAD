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

    require(launcher, 'var cancelEventName = mutexName + "-Cancel-" + handoffId;', "unique cancellation event name")
    require(launcher, "new EventWaitHandle(false, EventResetMode.ManualReset, cancelEventName)", "parent cancellation event")
    require(launcher, "cancelEvent.Set();", "parent timeout cancellation signal")
    require(launcher, 'script.AppendLine("$cancelEventName = " + PsLiteral(cancelEventName));', "cancellation name handoff")
    require(launcher, '[System.Threading.EventWaitHandle]::OpenExisting($cancelEventName)', "worker cancellation event open")
    require(launcher, 'if ($cancelEvent) { try { $cancelEvent.Dispose() } catch { } }', "worker cancellation handle cleanup")

    parent_wait = launcher.find("readyEvent.WaitOne(WorkerReadyTimeoutMilliseconds)")
    parent_cancel = launcher.find("cancelEvent.Set();", parent_wait)
    parent_kill = launcher.find("TryTerminateUnreadyWorker(updater);", parent_wait)
    outer_release = launcher.find("ReleaseCrossProcessReservation();", parent_kill)
    if min(parent_wait, parent_cancel, parent_kill, outer_release) < 0 or not (
        parent_wait < parent_cancel < parent_kill < outer_release
    ):
        raise AssertionError("readiness timeout must signal cancellation before best-effort child kill and before parent mutex release")

    mutex_open = launcher.find('[System.Threading.Mutex]::new($false, $mutexName)')
    cancel_open = launcher.find('[System.Threading.EventWaitHandle]::OpenExisting($cancelEventName)')
    ready_signal = launcher.find("$readyEvent.Set() | Out-Null")
    cancel_before_wait = launcher.find("if ($cancelEvent.WaitOne(0)) { throw 'QS3D updater was cancelled before mutex ownership.' }")
    mutex_wait = launcher.find("$updateMutex.WaitOne()")
    cancel_after_mutex = launcher.find("if ($cancelEvent.WaitOne(0)) { throw 'QS3D updater was cancelled after mutex ownership.' }")
    cad_wait = launcher.find("while (Get-Process -Name bricscad")
    cancel_during_cad = launcher.find("if ($cancelEvent.WaitOne(0)) { throw 'QS3D updater was cancelled while waiting for BricsCAD to close.' }")
    signature_check = launcher.find("Get-AuthenticodeSignature -LiteralPath $updater")
    cancel_before_install = launcher.find("if ($cancelEvent.WaitOne(0)) { throw 'QS3D updater was cancelled before installer execution.' }")
    updater_call = launcher.find("& $updater -ManifestUri $manifest")

    ordered = (
        mutex_open,
        cancel_open,
        ready_signal,
        cancel_before_wait,
        mutex_wait,
        cancel_after_mutex,
        cad_wait,
        cancel_during_cad,
        signature_check,
        cancel_before_install,
        updater_call,
    )
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        raise AssertionError(
            "worker cancellation must gate mutex wait, post-acquire BricsCAD wait, and final installer invocation in order"
        )

    require(launcher, "catch [System.Threading.AbandonedMutexException] { $ownsUpdateMutex = $true }", "abandoned parent mutex recovery")
    require(launcher, "if (-not $ownsUpdateMutex)", "mutex ownership fail-closed gate")
    require(launcher, "updater.Kill();", "best-effort detached child cleanup")
    kill_lines = [line.strip() for line in launcher.splitlines() if ".Kill(" in line]
    if kill_lines != ["updater.Kill();"]:
        raise AssertionError("only detached updater child may be killed: " + repr(kill_lines))
    if "Stop-Process" in launcher or "taskkill" in launcher or "process.Kill(" in launcher:
        raise AssertionError("BricsCAD/current-process termination remains forbidden")

    print(
        "PASS: readiness timeout signals a shared cancellation event before best-effort child termination; "
        "a surviving detached worker is cancellation-gated before mutex wait, during CAD wait, and immediately before install."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
