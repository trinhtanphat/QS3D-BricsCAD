#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/BrcPublicProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-brc-probe.ps1"
WINDOW_INTEROP = ROOT / "scripts/bricscad-runner-window-interop.ps1"
errors = []


def require_tokens(path, text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(label + " missing contract token: " + token)


def forbid_tokens(path, text, tokens, label):
    lowered = text.lower()
    for token in tokens:
        if token.lower() in lowered:
            errors.append(label + " contains forbidden private/unsafe token: " + token)


for path in (COMMAND, RUNNER, WINDOW_INTEROP):
    if not path.is_file():
        errors.append("missing BRC public-probe contract file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    require_tokens(COMMAND, text, (
        '[CommandMethod("QS3DBRCPROBE", CommandFlags.Modal)]',
        '"QS3D_BRC_PROBE_RESULT"',
        '"QS3D_BRC_PROBE_NONCE"',
        '"schema=QS3D_BRC_PUBLIC_PROBE_V1"',
        '"status=PASS"',
        '"command=QS3DBRCPROBE"',
        '"nonce="',
        "entity is ProxyEntity",
        "StartOpenCloseTransaction()",
        "OpenMode.ForRead",
        "entity.GeometricExtents",
        "entity.Explode(exploded)",
        '"entity_attempted_count="',
        '"entity_opened_count="',
        '"proxy_entity_count="',
        '"proxy_direct_metric_ready_count="',
        '"proxy_extents_available_count="',
        '"proxy_explode_success_count="',
        '"public_volume_entity_count="',
        '"proxy_exploded_surface_area_ready_count="',
        "face.GetVertexAt(0)",
        "MaxProxyExplosions",
        "MaxExplodedParts",
        "catch (ProbeLimitExceededException) { throw; }",
        "WriteMarkerAtomic(resultPath, lines)",
    ), "BrcPublicProbeCommands.cs")

    if "OpenMode.ForWrite" in text or ".Erase(" in text or "AppendEntity(" in text:
        errors.append("BRC public probe must remain read-only against the reference-copy DWG")
    for token in (
        'private const string ResultFileName = "brc-public-probe-result.txt";',
        "!Directory.Exists(directory)",
        "if (File.Exists(fullPath))",
        "File.Move(tempPath, fullPath)",
    ):
        if token not in text:
            errors.append("BRC public probe result marker must use a pre-existing directory and refuse overwrite: " + token)
    if "Directory.CreateDirectory" in text or "File.Replace(" in text:
        errors.append("BRC public probe command must not create arbitrary result directories or replace an existing result file")

    # Only fixed automation envelope fields and aggregate public-API counts may be
    # persisted. Dynamic type/name keys can disclose proprietary class identities.
    allowed_non_count_keys = {
        "status", "command", "process", "nonce", "error_code", "schema",
        "is_64bit", "scan_complete", "drawing_unit_code", "tile_mode",
    }
    literal_report_keys = set(re.findall(r'"([a-z][a-z0-9_]*)=', text))
    for key in sorted(literal_report_keys):
        if key not in allowed_non_count_keys and not key.endswith("_count"):
            errors.append("BRC public probe report key is not an aggregate count/control field: " + key)

    for forbidden_report in (
        '"error_message="',
        '"entity_type_"',
        '"snapshot_entity_type_"',
        '"proxy_exploded_type_"',
        "AppendCounts(",
        "GetType().Name",
        "Database.Filename",
        "document.Name",
        ".Handle",
        "Handle.ToString",
        ".Layer",
        "LayerId",
        "TextString",
        "DBText",
        "MText",
        ".XData",
        "ExtensionDictionary",
        '"proxy_xdata_present_count="',
        '"proxy_extension_dictionary_present_count="',
    ):
        if forbidden_report in text:
            errors.append("BRC public probe must not report handles/layers/text/metadata/database identity: " + forbidden_report)
    if "EntitySnapshotReader" in text:
        errors.append("BRC public probe must use its metrics-only reader and must not materialize normal snapshot handles/layers/text metadata")

    forbid_tokens(COMMAND, text, (
        "C:\\BLT", "C:/BLT", "D:\\1. PM", "D:/1. PM", "Tool BLT",
        "BLT3D", "BLT.dll", "BLT.brx", ".dbx", ".arx",
        "System.Reflection", "Assembly.Load", "DllImport", "LoadLibrary",
        "GetProcAddress", "Process.Modules", "Microsoft.Win32.Registry",
    ), "BrcPublicProbeCommands.cs")

    commands = []
    source_root = ROOT / "src/QS3D.BricsCAD.V25"
    if source_root.is_dir():
        for path in source_root.rglob("*.cs"):
            commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
    if sum(1 for command in commands if command.upper() == "QS3DBRCPROBE") != 1:
        errors.append("QS3DBRCPROBE must be registered exactly once")

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    require_tokens(RUNNER, text, (
        "Set-StrictMode -Version Latest",
        "$ErrorActionPreference = \"Stop\"",
        "$ConfirmReferenceCopy",
        '".reference-copy.dwg"',
        "Get-FileHash",
        "SHA256",
        "[IO.Path]::GetFullPath($PluginDll)",
        '"NETLOAD"',
        '"QS3DBRCPROBE"',
        "QS3D_BRC_PROBE_RESULT",
        "QS3D_BRC_PROBE_NONCE",
        "Start-Process",
        "-PassThru",
        "-WorkingDirectory $ArtifactDir",
        "finally",
        "Stop-Qs3dLaunchedProcess -Process $process",
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        'proxy_information_dialogs_dismissed = $proxyInformationDialogsDismissed',
        "Stop-Process -Id $Process.Id",
        "$Process.WaitForExit",
    ), "test-bricscad-v25-brc-probe.ps1")

    if "[guid]::newguid()" not in text.lower():
        errors.append("BRC runner must generate a fresh nonce with Guid.NewGuid")
    for variable in ("QS3D_BRC_PROBE_RESULT", "QS3D_BRC_PROBE_NONCE"):
        direct_cleanup = "Remove-Item Env:" + variable
        restored_cleanup = 'Restore-EnvironmentValue -Name "' + variable + '"'
        if direct_cleanup not in text and restored_cleanup not in text:
            errors.append("BRC runner must clean up or restore process environment variable " + variable)

    if text.count("Get-FileHash") < 2:
        errors.append("BRC runner must calculate reference-copy SHA256 both before and after BricsCAD")
    variables = re.findall(r"\$([A-Za-z][A-Za-z0-9_]*)", text)
    if not any(("before" in name.lower() or "original" in name.lower()) and
               ("sha" in name.lower() or "hash" in name.lower()) for name in variables):
        errors.append("BRC runner must retain an explicit before/original SHA256 value")
    if not any(("after" in name.lower() or "final" in name.lower()) and
               ("sha" in name.lower() or "hash" in name.lower()) for name in variables):
        errors.append("BRC runner must retain an explicit after/final SHA256 value")
    if "[StringComparison]::OrdinalIgnoreCase" not in text and "[StringComparison]::Ordinal" not in text:
        errors.append("BRC runner must compare before/after SHA256 values explicitly")

    guard_pos = text.find('".reference-copy.dwg"')
    before_hash_pos = text.find("Get-FileHash")
    start_pos = text.find("Start-Process")
    netload_pos = text.find('"NETLOAD"')
    dll_pos = text.find("$PluginDll", netload_pos + 1)
    probe_pos = text.find('"QS3DBRCPROBE"', netload_pos + 1)
    if min(guard_pos, before_hash_pos, start_pos) < 0 or not guard_pos < before_hash_pos < start_pos:
        errors.append("BRC runner must confirm *.reference-copy.dwg before hashing and launching BricsCAD")
    if min(netload_pos, dll_pos, probe_pos) < 0 or not netload_pos < dll_pos < probe_pos:
        errors.append("BRC runner script must NETLOAD the exact normalized PluginDll before QS3DBRCPROBE")
    marker_read_pos = text.find("$marker = Read-Qs3dProbeMarker")
    stop_before_hash_pos = text.find("Stop-Qs3dLaunchedProcess -Process $process", marker_read_pos)
    after_hash_pos = text.find("$drawingHashAfter =", marker_read_pos)
    if min(marker_read_pos, stop_before_hash_pos, after_hash_pos) < 0 or not marker_read_pos < stop_before_hash_pos < after_hash_pos:
        errors.append("BRC runner must validate the marker, stop only its launched BricsCAD process, then calculate the after-hash")

    for key, expected in (("status", "PASS"), ("command", "QS3DBRCPROBE")):
        if key not in text or expected not in text:
            errors.append("BRC runner must verify result marker " + key + "=" + expected)
    if not re.search(r'(?is)(marker|result).*nonce|nonce.*(marker|result)', text):
        errors.append("BRC runner must bind the result marker to the generated nonce")

    forbid_tokens(RUNNER, text, (
        "C:\\BLT", "C:/BLT", "D:\\1. PM", "D:/1. PM", "Tool BLT",
        "BLT3D", "BLT.dll", "BLT.brx", ".dbx", ".arx",
        "System.Reflection", "Assembly.Load", "DllImport", "LoadLibrary",
        "GetProcAddress", "Process.Modules", "Microsoft.Win32.Registry",
        "Database.Filename", "TextString", "ExtensionDictionary", ".XData",
    ), "test-bricscad-v25-brc-probe.ps1")

    if "Copy-Item" in text:
        errors.append("BRC runner must consume an owner-confirmed reference copy, not copy a private fixture itself")
    if re.search(r"(?i)(handle|layer|text|string_value|database[_-]?filename)\s*=", text):
        errors.append("BRC runner must not add handles/layers/text/metadata/database filename to its report")

if WINDOW_INTEROP.is_file():
    text = WINDOW_INTEROP.read_text(encoding="utf-8")
    require_tokens(WINDOW_INTEROP, text, (
        "CloseProxyInformationDialogs(int processId)",
        'string.Equals(title.ToString(), "Proxy Information", StringComparison.Ordinal)',
        'string.Equals(className.ToString(), "#32770", StringComparison.Ordinal)',
        "ownerProcessId != (uint)processId",
        "PostMessage(window, WmClose",
        "function Close-Qs3dProxyInformationDialog",
    ), "bricscad-runner-window-interop.ps1")
    for forbidden in ("FindWindow(", "SendKeys", "SetForegroundWindow", "Process.GetProcesses"):
        if forbidden in text:
            errors.append("BRC runner dialog helper must target only windows owned by the launched PID: " + forbidden)

print("QS3D BRC public-probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DBRCPROBE and its V25 runner use aggregate public-API counts only, dismiss only the launched PID's exact Proxy Information dialog, require an explicit *.reference-copy.dwg, verify before/after SHA256 immutability, NETLOAD the exact plugin DLL, bind a nonce/result marker, clean up BricsCAD, and exclude private BLT/binary/path plus handle/layer/text/metadata/database identity data.")
