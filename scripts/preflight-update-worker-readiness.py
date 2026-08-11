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

    require(launcher, "private const int WorkerReadyTimeoutMilliseconds = 5000;", "bounded readiness timeout")
    require(launcher, 'var readyEventName = mutexName + "-Ready-" + Guid.NewGuid().ToString("N");', "unique readiness channel")
    require(launcher, "new EventWaitHandle(false, EventResetMode.ManualReset, readyEventName)", "parent readiness event creation")
    require(launcher, 'script.AppendLine("$readyEventName = " + PsLiteral(readyEventName));', "readiness name handoff")
    require(launcher, '[System.Threading.EventWaitHandle]::OpenExisting($readyEventName)', "worker readiness channel open")
    require(launcher, "$readyEvent.Set() | Out-Null", "worker readiness signal")
    require(launcher, "readyEvent.WaitOne(WorkerReadyTimeoutMilliseconds)", "parent readiness wait")
    require(launcher, "TryTerminateUnreadyWorker(updater);", "timeout child cleanup")
    require(launcher, "Updater worker không xác nhận readiness trong 5 giây", "timeout user-facing failure")
    require(launcher, "updater.Kill();", "detached worker timeout termination")
    require(launcher, "updater.WaitForExit(WorkerReadyTimeoutMilliseconds);", "bounded worker termination join")

    worker_mutex_open = launcher.find('[System.Threading.Mutex]::new($false, $mutexName)')
    worker_ready_open = launcher.find('[System.Threading.EventWaitHandle]::OpenExisting($readyEventName)')
    worker_ready_signal = launcher.find("$readyEvent.Set() | Out-Null")
    worker_mutex_wait = launcher.find("$updateMutex.WaitOne()")
    if min(worker_mutex_open, worker_ready_open, worker_ready_signal, worker_mutex_wait) < 0 or not (
        worker_mutex_open < worker_ready_open < worker_ready_signal < worker_mutex_wait
    ):
        raise AssertionError("worker must open mutex handle -> open/signal readiness -> wait for mutex ownership")

    process_start = launcher.find("var updater = Process.Start(startInfo)")
    parent_ready_wait = launcher.find("readyEvent.WaitOne(WorkerReadyTimeoutMilliseconds)", process_start)
    success_return = launcher.find("return true;", parent_ready_wait)
    if process_start < 0 or parent_ready_wait < 0 or success_return < 0 or not (process_start < parent_ready_wait < success_return):
        raise AssertionError("TrySchedule must await child readiness after Process.Start and before returning success")

    timeout_cleanup = launcher.find("TryTerminateUnreadyWorker(updater);", parent_ready_wait)
    outer_release = launcher.find("ReleaseCrossProcessReservation();", timeout_cleanup)
    scheduled_reset = launcher.find("Interlocked.Exchange(ref _scheduled, 0);", timeout_cleanup)
    if min(timeout_cleanup, outer_release, scheduled_reset) < 0 or not (timeout_cleanup < outer_release and timeout_cleanup < scheduled_reset):
        raise AssertionError("readiness timeout must terminate child before reservation release / scheduled reset")

    worker_wait_all_cad = launcher.find("while (Get-Process -Name bricscad")
    worker_updater_call = launcher.find("& $updater -ManifestUri $manifest")
    if min(worker_ready_signal, worker_mutex_wait, worker_wait_all_cad, worker_updater_call) < 0 or not (
        worker_ready_signal < worker_mutex_wait < worker_wait_all_cad < worker_updater_call
    ):
        raise AssertionError("worker readiness signal must precede mutex ownership; install remains after all-BricsCAD wait")

    require(launcher, "CloseMainWindow()", "graceful BricsCAD close contract")
    kill_lines = [line.strip() for line in launcher.splitlines() if ".Kill(" in line]
    if kill_lines != ["updater.Kill();"]:
        raise AssertionError("readiness timeout may kill only detached updater child: " + repr(kill_lines))
    if "Stop-Process" in launcher or "taskkill" in launcher or "process.Kill(" in launcher:
        raise AssertionError("BricsCAD/current-process force termination is forbidden")

    require(launcher, 'if ($readyEvent) { try { $readyEvent.Dispose() } catch { } }', "worker readiness event finally cleanup")
    require(launcher, 'if ($ownsUpdateMutex -and $updateMutex)', "worker mutex ownership cleanup")

    print("PASS: detached updater must open the cross-process mutex handle and acknowledge readiness before scheduling succeeds; timeout kills only the blocked updater child while BricsCAD remains open.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
