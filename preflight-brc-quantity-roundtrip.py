#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/BrcQuantityRoundTripProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-brc-quantity-roundtrip.ps1"
WINDOW_INTEROP = ROOT / "scripts/bricscad-runner-window-interop.ps1"
errors = []


def require(text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(label + " missing contract token: " + token)


for path in (COMMAND, RUNNER, WINDOW_INTEROP):
    if not path.is_file():
        errors.append("missing BRC quantity round-trip file: " + str(path.relative_to(ROOT)))

command = COMMAND.read_text(encoding="utf-8") if COMMAND.is_file() else ""
runner = RUNNER.read_text(encoding="utf-8") if RUNNER.is_file() else ""

require(command, (
    '[CommandMethod("QS3DBRCROUNDTRIPPROBE", CommandFlags.Modal)]',
    '"QS3D_BRC_ROUNDTRIP_RESULT"',
    '"QS3D_BRC_ROUNDTRIP_WORKBOOK"',
    '"QS3D_BRC_ROUNDTRIP_NONCE"',
    'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
    'EntitySnapshotReader.ReadCurrentSpace(document)',
    'string.Equals(x.EntityType, "ProxyEntity"',
    'new ProjectRecognitionService().SuggestBatch(project, proxySnapshots)',
    'proxyBatch.Results.Count(x => x.IsCaptureReady)',
    'proxyBatch.AutoAccepted.Count',
    'proxyCapturedOwnerCount != 0',
    'ProjectStateSnapshot.CreateDetachedCopy(project)',
    'RegenerateDirty(preview)',
    'ProjectQuantityReportBuilder.Detail(preview)',
    'ProjectQuantityReportBuilder.Group(preview)',
    'CadHandleService.Resolve(document, exportHandles)',
    'XlsxQuantityExporter.ExportEd2(workbookPath, detailRows, summaryRows)',
    'XlsxHandleReader.ReadHandleLookup(workbookPath, 2)',
    'lookup.IsModernSchema',
    'lookup.IsEd2Detail',
    'SourceHandleResolver.Resolve(project, lookup.ElementIds)',
    'projectHandles.SequenceEqual(workbookHandles',
    'document.Editor.SetImpliedSelection(locatedIds.ToArray())',
    '"schema=QS3D_BRC_QUANTITY_ROUNDTRIP_V1"',
    '"proxy_capture_ready_count="',
    '"proxy_autoaccepted_count="',
    '"proxy_captured_owner_count="',
    '"element_handle_provenance_matched=true"',
), "BrcQuantityRoundTripProbeCommands.cs")

require(runner, (
    'Set-StrictMode -Version Latest',
    '$ErrorActionPreference = "Stop"',
    '$ConfirmReferenceCopy',
    '".reference-copy.dwg"',
    '$projectSidecar = [IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")',
    'Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256',
    '"NETLOAD"',
    '"QS3DB4D"',
    '"QS3DBRCROUNDTRIPPROBE"',
    'QS3D_BRC_ROUNDTRIP_RESULT',
    'QS3D_BRC_ROUNDTRIP_WORKBOOK',
    'QS3D_BRC_ROUNDTRIP_NONCE',
    'Start-Process',
    '-PassThru',
    'Stop-Qs3dLaunchedProcess -Process $process',
    '. $windowInteropPath',
    'Close-Qs3dProxyInformationDialog -Process $process',
    'Stop-Process -Id $Process.Id',
    'Require-Qs3dValue -Marker $marker -Key "proxy_capture_ready_count" -Expected "0"',
    'Require-Qs3dValue -Marker $marker -Key "proxy_autoaccepted_count" -Expected "0"',
    'Require-Qs3dValue -Marker $marker -Key "proxy_captured_owner_count" -Expected "0"',
    'drawing_copy_sha256_before',
    'drawing_copy_sha256_after',
    'workbook_sha256',
    'proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed',
), "test-bricscad-v25-brc-quantity-roundtrip.ps1")

if command:
    forbidden = (
        "OpenMode.ForWrite", "AppendEntity(", ".Erase(", "Database.Filename",
        "TextString", ".XData", "ExtensionDictionary", "System.Reflection",
        "Assembly.Load", "DllImport", "LoadLibrary", "GetProcAddress",
        "C:\\BLT", "D:\\1. PM", "Tool BLT", "BLT.dll", "BLT.brx",
    )
    for token in forbidden:
        if token.lower() in command.lower():
            errors.append("BRC quantity probe contains forbidden write/private token: " + token)
    if "Directory.CreateDirectory" in command or "File.Replace(" in command:
        errors.append("BRC quantity command must not create arbitrary directories or overwrite qualification outputs")
    allowed_non_count_keys = {
        "status", "command", "process", "nonce", "error_code", "schema", "is_64bit",
        "modern_ed2_schema", "detail_sheet_resolved", "drawing_fingerprint_matched",
        "element_handle_provenance_matched",
    }
    for key in sorted(set(re.findall(r'"([a-z][a-z0-9_]*)=', command))):
        if key not in allowed_non_count_keys and not key.endswith("_count"):
            errors.append("BRC quantity marker key is not an aggregate count/control field: " + key)

if runner:
    netload = runner.find('"NETLOAD"')
    plugin = runner.find('$PluginDll', netload + 1)
    b4d = runner.find('"QS3DB4D"', netload + 1)
    probe = runner.find('"QS3DBRCROUNDTRIPPROBE"', netload + 1)
    if min(netload, plugin, b4d, probe) < 0 or not netload < plugin < b4d < probe:
        errors.append("runner must NETLOAD the exact plugin, then run QS3DB4D, then the round-trip probe")
    if runner.count("Get-FileHash -LiteralPath $DrawingCopy") < 2:
        errors.append("runner must hash the disposable DWG before and after BricsCAD")
    if "Copy-Item" in runner:
        errors.append("runner must consume an explicit owner-confirmed reference copy")
    for variable in (
        "QS3D_BRC_ROUNDTRIP_RESULT", "QS3D_BRC_ROUNDTRIP_WORKBOOK", "QS3D_BRC_ROUNDTRIP_NONCE"
    ):
        if 'Restore-EnvironmentValue -Name "' + variable + '"' not in runner:
            errors.append("runner must restore process environment variable " + variable)

if WINDOW_INTEROP.is_file():
    helper = WINDOW_INTEROP.read_text(encoding="utf-8")
    require(helper, (
        'CloseProxyInformationDialogs(int processId)',
        'string.Equals(title.ToString(), "Proxy Information", StringComparison.Ordinal)',
        'string.Equals(className.ToString(), "#32770", StringComparison.Ordinal)',
        'ownerProcessId != (uint)processId',
        'PostMessage(window, WmClose',
    ), "bricscad-runner-window-interop.ps1")
    for token in ("FindWindow(", "SendKeys", "SetForegroundWindow", "Process.GetProcesses"):
        if token in helper:
            errors.append("runner dialog helper must not target windows outside the exact launched PID: " + token)

print("QS3D BRC B4D/ED2/Excel-Locate round-trip preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: local-only qualification runs exact NETLOAD -> QS3DB4D -> ED2 workbook -> CHI_TIET lookup -> full CAD selection on an immutable reference copy, while metricless proxy entities remain review-only and no private BLT API/data is persisted.")
