$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$source = Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$policy = [regex]::Match($source, '(?ms)^        private static bool RequiresPhysicalHover\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $policy) { throw 'FAIL: explicit versioned hover policy is missing.' }
$name = 'Local022ObservedPolicy' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
#nullable enable
using System;
public static class $name {
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
$policy
    public static void Run() {
        if (!RequiresPhysicalHover(null) || !RequiresPhysicalHover("") || !RequiresPhysicalHover("NATIVE_V1"))
            throw new Exception("Legacy hover meaning changed");
        if (RequiresPhysicalHover("OBSERVED_CLICK_V2")) throw new Exception("Observed click driver still requires unsupported hover");
        foreach (var bad in new[] { "native_v1", "OBSERVED_CLICK_V2 ", "UNKNOWN", "OBSERVED_CLICK_V2\0" }) {
            bool rejected = false;
            try { RequiresPhysicalHover(bad); } catch (ProbeException) { rejected = true; }
            if (!rejected) throw new Exception("Unknown driver admitted");
        }
    }
}
"@
([type]$name)::Run()
$writer = [regex]::Match($source, '(?ms)^        private static void WriteUiAction\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $writer) { throw 'FAIL: actual request writer is missing.' }
$serializerName = 'Local022ObservedSerializer' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
public static class $serializerName {
    private const string UiActionSchema = "QS3D_LOCAL022_UI_ACTION_V1";
    private static bool ObservedClickDriver = true;
    private sealed class Context { public string RunId = "0123456789abcdef0123456789abcdef"; }
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
    private static string Serialized;
    private static void CheckedScreenPoint(int x, int y) { }
    private static string UiActionPath(Context context, int sequence) { return "contract-fixture-only"; }
    private static void WriteNewAtomic(string path, string body) { Serialized = body; }
$writer
    public static string GetRequests() {
        var records = new List<string>();
        foreach (var stage in new[] { "SelectTree", "OpenCancelDialog", "OpenCreateDialog", "AcceptCreateDialog",
            "StartFirstDraw", "FirstCentre", "SecondCentre", "OpenFamilyScope", "SelectFamilyScope", "StartSecondDraw", "RepeatCentre" }) {
            WriteUiAction(new Context(), 1, "click", 94, 471, "", stage); records.Add(Serialized);
        }
        foreach (var stage in new[] { "CancelDialog", "EndFirstDraw", "EndSecondDraw" }) {
            WriteUiAction(new Context(), 1, "key", 94, 471, stage == "EndFirstDraw" ? "ENTER" : "ESC", stage); records.Add(Serialized);
        }
        foreach (var item in new[] { "InputL1=2", "InputW1=2", "InputL2=1", "InputW2=1", "InputH1=1", "InputH2=0", "EditH2=1000" }) {
            var pair = item.Split('=');
            WriteUiAction(new Context(), 1, "text", 94, 471, pair[1], pair[0]); records.Add(Serialized);
        }
        foreach (var invalid in new[] { "", "InputH2\n", "InputH2\"", "InputH2\\", "_InputH2" }) {
            bool rejected = false;
            try { WriteUiAction(new Context(), 1, "text", 94, 471, "0", invalid); } catch (ProbeException) { rejected = true; }
            if (!rejected) throw new Exception("Unsafe serializer stage accepted");
        }
        ObservedClickDriver = false;
        WriteUiAction(new Context(), 1, "move", 94, 471, "");
        if (Serialized.Contains("stage") || !Serialized.Contains(UiActionSchema)) throw new Exception("Legacy schema changed");
        return "[" + string.Join(",", records) + "]";
    }
}
"@
([type]$serializerName)::GetRequests() | node (Join-Path $PSScriptRoot 'test-ui-observed-input.mjs') --producer
if ($LASTEXITCODE -ne 0) { throw 'FAIL: actual C# producer / JS consumer protocol interoperability.' }
foreach ($major in @(25,26)) {
    $runner = Get-Content (Join-Path $PSScriptRoot "..\..\scripts\test-bricscad-v$major-single-footing.ps1") -Raw
    if (-not $runner.Contains("if (`$UiDriver -ceq 'NATIVE_V1') { [void](Close-Qs3dProxyInformationDialog -Process `$process) }")) {
        throw 'FAIL: observed driver can run PowerShell dialog input.'
    }
    if (-not $runner.Contains("if (`$UiDriver -ceq 'NATIVE_V1' -and `$InteractiveUi -and -not `$process.HasExited -and `$Phase -ceq 'ui')")) {
        throw 'FAIL: observed driver can run PowerShell physical input.'
    }
    if (-not $runner.Contains("ui_driver = `$UiDriver") -or -not $runner.Contains("'QS3D_LOCAL022_UI_DRIVER'")) {
        throw 'FAIL: driver identity is not recorded/restored.'
    }
    if (-not $runner.Contains("if (`$UiDriver -ceq 'NATIVE_V1') { [void]`$process.CloseMainWindow() }")) {
        throw 'FAIL: observed cleanup can send a PowerShell window message.'
    }
}
Write-Output 'PASS: V1 requires real hover, V2 explicitly does not claim hover, unknown driver fails closed, and external runs cannot enter PowerShell input branches.'
