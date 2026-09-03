#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelMultiDwgRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-multidwg.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs"
LIFETIME = ROOT / "src/QS3D.BricsCAD.V25/UI/DocumentBoundWindowLifetime.cs"
NATIVE = ROOT / "src/QS3D.BricsCAD.V25/UI/DocumentBoundNativeLifecycleCoordinator.cs"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, WINDOW, LIFETIME, NATIVE, RUNBOOK):
    if not path.is_file():
        errors.append("missing Curtain P12 probe surface: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DCURTAINP12SEEDA", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12CAPTURE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12SEEDB", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12CHECKB", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12ACTIVATEA", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12CHECKA", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12ACTIVATEB", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12CLOSEA", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP12FINAL", CommandFlags.Modal)',
        'QS3D_CURTAIN_PANEL_MULTIDWG_RUNTIME_V1',
        'QS3D_CURTAIN_P12_RESULT',
        'QS3D_CURTAIN_P12_NONCE',
        'QS3D_CURTAIN_P12_DWG_A',
        'QS3D_CURTAIN_P12_DWG_B',
        '.curtain-multidwg-probe-copy.dwg',
        'matches[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent, matches[0]))',
        'PresentationSource.CurrentSources',
        '.OfType<HwndSource>()',
        '.Select(source => source.RootVisual)',
        'string.Equals(button.Tag as string, "QS3DCURTAINFRAMEHEALTH", StringComparison.Ordinal)',
        'BcadApplication.DocumentManager.MdiActiveDocument = documentA',
        'BcadApplication.DocumentManager.MdiActiveDocument = documentB',
        'documentA.CloseAndDiscard()',
        'state.WindowClosedWithA = true;',
        'state.BRemainedActive = true;',
        'state.BUnchangedAfterAClose = true;',
        'ReferenceEquals(Project, current.Project)',
        'ChangeVersion != current.ChangeVersion',
        'UpdatedUtc != current.UpdatedUtc',
        'projects_unchanged_while_b_active=true',
        'reactivated_a_refresh_succeeded=true',
        'a_close_closed_bound_window=true',
        'window_closed_event_observed=true',
        'b_project_unchanged_after_a_close=true',
        'error_code=CURTAIN_PANEL_MULTIDWG_RUNTIME_FAILED',
        'production_local002_qualified=false',
        'p12_qualified=',
        'FileMode.CreateNew',
        'File.Move(tempPath, path)',
    ):
        if token not in text:
            errors.append("Curtain P12 command missing contract token: " + token)

    complete_start = text.find('public void Complete()')
    marker_end = text.find('private static void Run(', complete_start)
    marker_region = text[complete_start:marker_end].lower()
    for forbidden in ("handle=", "element_id=", "project_id=", "family_id=", "drawing_path=", "profile=", "exception=", "message="):
        if forbidden in marker_region:
            errors.append("Curtain P12 final marker leaks a forbidden field: " + forbidden)
    for forbidden in ("DllImport", "dynamic ", "GetType().Get", "BLT"):
        if forbidden in text:
            errors.append("Curtain P12 command crosses the local automation boundary: " + forbidden)
    if "WpfApplication.Current" in text or "System.Windows.Application.Current" in text:
        errors.append("Curtain P12 command must enumerate BricsCAD-hosted WPF sources without Application.Current")
    close_start = text.find('public void CloseA()')
    final_start = text.find('public void Complete()', close_start)
    run_start = text.find('private static void Run(', final_start)
    close_region = text[close_start:final_start]
    final_region = text[final_start:run_start]
    if 'state.BRemainedActive = true;' in close_region or 'state.BUnchangedAfterAClose = true;' in close_region:
        errors.append("Curtain P12 must validate B usability after the native close command boundary, not inside CloseAndDiscard")
    for token in ('var documentB = RequireActive(context.DrawingB);', 'state.SeedB.Ensure(documentB, "B");', 'state.BRemainedActive = true;', 'state.BUnchangedAfterAClose = true;'):
        if token not in final_region:
            errors.append("Curtain P12 final command-boundary validation missing token: " + token)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmDisposableCopies',
        'ArtifactDir must stay outside the repository.',
        'FixtureDwg must be the repository-generated QS3D sample.',
        'curtain-a.curtain-multidwg-probe-copy.dwg',
        'curtain-b.curtain-multidwg-probe-copy.dwg',
        'QS3D_CURTAIN_P12_RESULT',
        'QS3D_CURTAIN_P12_NONCE',
        'QS3D_CURTAIN_P12_DWG_A',
        'QS3D_CURTAIN_P12_DWG_B',
        'rev-parse HEAD',
        'status --porcelain=v1 --untracked-files=all',
        'Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $PluginDll -ExpectedSourceSha $gitHead',
        'Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $bricscadExe',
        'Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 30',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        '-WorkingDirectory $ArtifactDir',
        '"QS3DCURTAINP12SEEDA", "QS3DCURTAIN", "QS3DCURTAINP12CAPTURE"',
        '"_.OPEN", (\'"\' + $drawingB + \'"\')',
        '"QS3DCURTAINP12SEEDB", "QS3DCURTAINP12CHECKB"',
        '"QS3DCURTAINP12ACTIVATEA", "QS3DCURTAINP12CHECKA"',
        '"QS3DCURTAINP12ACTIVATEB", "QS3DCURTAINP12CLOSEA", "QS3DCURTAINP12FINAL"',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Remove-ExactFile -Path $scriptPath',
        'Remove-ExactFile -Path $privatePath',
        '($sidecar + ".bak")',
        '($sidecar + ".lock")',
        '$privateFiles.Count -ne 12',
        'Curtain P12 private-state path escaped the fixture-copy root.',
        'Copy-Item -LiteralPath $FixtureDwg -Destination $drawing -Force',
        'process_cleanup_verified',
        'script_cleanup_verified',
        'private_state_cleanup_verified',
        'drawing_a_restore_verified',
        'drawing_b_restore_verified',
        'Restore-EnvironmentValue -Name $name',
    ):
        if token not in text:
            errors.append("Curtain P12 runner missing contract token: " + token)

    ordered = (
        '"QS3DCURTAINP12SEEDA"', '"QS3DCURTAIN"', '"QS3DCURTAINP12CAPTURE"',
        '"_.OPEN"', '"QS3DCURTAINP12SEEDB"', '"QS3DCURTAINP12CHECKB"',
        '"QS3DCURTAINP12ACTIVATEA"', '"QS3DCURTAINP12CHECKA"',
        '"QS3DCURTAINP12ACTIVATEB"', '"QS3DCURTAINP12CLOSEA"', '"QS3DCURTAINP12FINAL"',
    )
    positions = [text.find(token) for token in ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Curtain P12 runner must preserve the guarded two-DWG/window lifecycle sequence")
    if text.count('Start-Process -FilePath $bricscadExe') != 1:
        errors.append("Curtain P12 runner must launch exactly one isolated BricsCAD process")
    if '$sidecar + ".bak", $sidecar + ".lock"' in text:
        errors.append("Curtain P12 runner must parenthesize sidecar suffix paths so PowerShell does not split them into relative tokens")
    for forbidden in ("Get-Process -Name '*'", 'Get-Process -Name "bricscad"', "$expectedAssemblyRevision", "Process.GetProcesses", "SendKeys", "SetForegroundWindow", "git reset", "git clean"):
        if forbidden in text:
            errors.append("Curtain P12 runner contains a broad/destructive operation: " + forbidden)

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in ('DocumentBoundWindowLifetime.Attach(this, _document);', 'EnsureActive("làm mới Vách Kính Hub")'):
        if token not in text:
            errors.append("Curtain Hub production affinity contract missing token: " + token)

if LIFETIME.is_file():
    text = LIFETIME.read_text(encoding="utf-8")
    for token in (
        '_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(',
        'The shared coordinator has already matched this registration',
        '_window.Close();',
    ):
        if token not in text:
            errors.append("Document-bound H3 lifetime contract missing token: " + token)
    for forbidden in (
        'BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;',
        '_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;',
    ):
        if forbidden in text:
            errors.append("Curtain P12 lifetime must not restore per-window native reactor ownership: " + forbidden)

if NATIVE.is_file():
    text = NATIVE.read_text(encoding="utf-8")
    for token in (
        'BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;',
        'lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;',
        'lifecycleDocument.CloseAborted += OnDocumentCloseAborted;',
        'new WeakReference<Callbacks>(callbacks)',
        'TrySnapshotDestroyByLifecycleDocument',
        'TrySnapshotDestroyByNativeIdentity',
        'if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;',
    ):
        if token not in text:
            errors.append("Curtain P12 shared native lifetime contract missing token: " + token)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in ("P12", "multi-DWG", "Curtain Hub", "test-bricscad-v25-curtain-panel-multidwg.ps1", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain panel runbook is missing P12 boundary token: " + token)

print("QS3D Curtain P12 multi-DWG/modeless runtime preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: P12 probe drives the real A-bound Curtain Hub across two projects, proves wrong-DWG routed-button refusal, reactivation success and A-destroy window closure; H3 keeps per-window lifetime managed-only while the shared native coordinator owns document reactors, and the exact-SHA runner preserves both disposable DWGs and cleanup boundaries.")
