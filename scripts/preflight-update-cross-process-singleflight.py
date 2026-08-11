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


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def main() -> int:
    launcher = read(LAUNCHER)

    require(launcher, "using System.Security.Principal;", "Windows SID namespace dependency")
    require(launcher, 'UpdateMutexPrefix = "Global\\\\QS3D-BricsCAD-V25-Update-"', "global per-user mutex namespace")
    require(launcher, "WindowsIdentity.GetCurrent()", "current Windows identity resolution")
    require(launcher, "identity.User?.Value", "Windows SID binding")
    require(launcher, "new Mutex(true, mutexName, out var createdNew)", "parent-owned named mutex reservation")
    require(launcher, "if (!createdNew)", "existing worker reservation rejection")
    require(launcher, "_crossProcessReservation = reservation;", "parent reservation lifetime hold")

    local_singleflight = launcher.find("Interlocked.CompareExchange(ref _scheduled, 1, 0)")
    cross_singleflight = launcher.find("TryAcquireCrossProcessReservation(out var mutexName, out error)")
    worker_start = launcher.find("var updater = Process.Start(startInfo)")
    if min(local_singleflight, cross_singleflight, worker_start) < 0 or not (local_singleflight < cross_singleflight < worker_start):
        raise AssertionError("process-local flag -> cross-process reservation -> worker launch must be ordered")

    require(launcher, "ReleaseCrossProcessReservation();", "parent launch-failure reservation cleanup")
    require(launcher, "Interlocked.Exchange(ref _scheduled, 0);", "parent launch-failure in-process reset")
    require(launcher, "reservation.ReleaseMutex();", "parent reservation release helper")
    require(launcher, "reservation.Dispose();", "parent reservation disposal")

    require(launcher, 'script.AppendLine("$mutexName = " + PsLiteral(mutexName));', "mutex name handoff to detached worker")
    require(launcher, '[System.Threading.Mutex]::new($false, $mutexName)', "explicit worker named-mutex open")
    require(launcher, "$updateMutex.WaitOne()", "worker mutex ownership wait")
    require(launcher, "catch [System.Threading.AbandonedMutexException] { $ownsUpdateMutex = $true }", "normal parent-exit abandoned mutex recovery")
    require(launcher, "if (-not $ownsUpdateMutex)", "worker ownership fail-closed gate")
    require(launcher, "while (Get-Process -Name bricscad", "all-BricsCAD wait remains after reservation")
    require(launcher, "$updateMutex.ReleaseMutex()", "worker mutex release")
    require(launcher, "$updateMutex.Dispose()", "worker mutex disposal")
    require(launcher, 'script.AppendLine("finally {")', "worker mutex finally cleanup")

    worker_mutex = launcher.find('[System.Threading.Mutex]::new($false, $mutexName)')
    worker_wait = launcher.find("while (Get-Process -Name bricscad")
    updater_call = launcher.find("& $updater -ManifestUri $manifest")
    if min(worker_mutex, worker_wait, updater_call) < 0 or not (worker_mutex < worker_wait < updater_call):
        raise AssertionError("worker must own the cross-process reservation before waiting/installing")

    # Keep all previously established safety boundaries.
    require(launcher, "WinVerifyTrust", "running plugin Authenticode trust")
    require(launcher, "Get-AuthenticodeSignature -LiteralPath $updater", "installed updater signature validation")
    require(launcher, "-AllowedPackageHost @('github.com')", "GitHub package host allowlist")
    require(launcher, "CloseMainWindow()", "graceful host close")
    reject(launcher, "Stop-Process", "forced BricsCAD termination")
    reject(launcher, "taskkill", "forced BricsCAD termination")
    reject(launcher, ".Kill(", "forced process termination")

    print("PASS: one-click updater scheduling is serialized across BricsCAD processes for the same Windows user, and the detached worker holds the same reservation through install without force-killing CAD.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
