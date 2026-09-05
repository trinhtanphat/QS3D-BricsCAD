$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$source = Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$methods = foreach ($method in @('OperatorPauseEnabled', 'AwaitObservedOperator', 'OnTick')) {
    $match = [regex]::Match($source, "(?ms)^            private (?:static )?(?:bool|void) $method\([^\r\n]*\)\r?\n            \{.*?^            \}").Value
    if (-not $match) { throw "FAIL: actual $method implementation missing." }
    $match.Replace('DateTime.UtcNow', 'Now')
}
$name = 'Local022OperatorPause' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
#nullable enable
using System;
public sealed class $name {
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
    private DateTime Now = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);
    private DateTime _deadlineUtc;
    private DateTime? _operatorPauseStartedUtc;
    private bool _pauseForOperator = true;
    private bool _requestWritten = true;
    private int _sequence = 1;
    private string? _pickObservationError;
    private string _stage = "InputH2";
    private object _context = new object();
    private bool ack, invalidAck, invalidContext;
    private int ticks, contextChecks;
    private string? failure;
    private void RequireUiContextStable(object context) { contextChecks++; if (invalidContext) throw new ProbeException("ui_document_changed"); }
    private bool HasExactUiAck(object context, int sequence) { if (invalidAck) throw new ProbeException("ui_ack_identity_mismatch"); return ack; }
    private void Tick() { ticks++; }
    private void Fail(Exception e) { failure = e.Message; }
$($methods -join "`n")
    private $name() { _deadlineUtc = Now.AddSeconds(600); _operatorPauseStartedUtc = Now.AddSeconds(10); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run() {
        Check(OperatorPauseEnabled(true, "1"), "explicit observed pause refused");
        Check(!OperatorPauseEnabled(true, "0") && !OperatorPauseEnabled(false, null), "wall clock default changed");
        foreach (var bad in new[] { "true", "01", "1 ", "", "UNKNOWN" }) {
            bool rejected = false; try { OperatorPauseEnabled(true, bad); } catch (ProbeException) { rejected = true; }
            Check(rejected, "invalid pause mode admitted");
        }
        bool nativeRejected = false; try { OperatorPauseEnabled(false, "1"); } catch (ProbeException) { nativeRejected = true; }
        Check(nativeRejected, "native pause silently enabled");
        var p = new $name();
        p.Now = p.Now.AddSeconds(4000); p.OnTick(null, EventArgs.Empty);
        Check(p.failure == null && p.ticks == 0 && p.contextChecks == 1, "unacked operator wait expired or advanced");
        p.Now = p.Now.AddSeconds(1000); p.OnTick(null, EventArgs.Empty);
        Check(p.failure == null && p.ticks == 0 && p.contextChecks == 2, "paused host not revalidated");
        p.ack = true; p.OnTick(null, EventArgs.Empty);
        Check(p.failure == null && p.ticks == 1 && p._deadlineUtc == p.Now.AddSeconds(590), "resume did not retain remaining budget");
        p.Now = p.Now.AddSeconds(590); p.OnTick(null, EventArgs.Empty);
        Check(p.failure == null && p.ticks == 2, "boundary expired early");
        p.Now = p.Now.AddSeconds(1); p.OnTick(null, EventArgs.Empty);
        Check(p.failure == "ui_timeout_InputH2" && p.ticks == 2, "ACK polls kept extending active product deadline");
        foreach (var mode in new[] { "context", "ack", "pick", "clock" }) {
            var f = new $name(); f.Now = f.Now.AddSeconds(mode == "clock" ? 5 : 5000);
            f.invalidContext = mode == "context"; f.invalidAck = mode == "ack"; f._pickObservationError = mode == "pick" ? "pick_failed" : null;
            f.OnTick(null, EventArgs.Empty);
            Check(f.failure != null && f.ticks == 0, "invalid " + mode + " admitted during pause");
        }
        var legacy = new $name(); legacy._pauseForOperator = false; legacy.Now = legacy.Now.AddSeconds(601); legacy.OnTick(null, EventArgs.Empty);
        Check(legacy.failure == "ui_timeout_InputH2", "non-pause deadline relaxed");
        var preparation = new $name(); preparation._requestWritten = false; preparation.Now = preparation.Now.AddSeconds(601); preparation.OnTick(null, EventArgs.Empty);
        Check(preparation.failure == "ui_timeout_InputH2", "unpublished preparation paused");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual UI tick pauses only unacknowledged operator time; exact ACK resumes remaining budget once; context, malformed ACK, witness and active deadlines still fail closed.'
