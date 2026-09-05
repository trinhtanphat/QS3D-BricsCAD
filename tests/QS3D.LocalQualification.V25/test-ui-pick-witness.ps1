$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Execute the actual host-independent witness, not a reimplementation. No CAD
# assembly, UI input, project setter or generated-geometry reader is loaded.
$witnessPath = Join-Path $PSScriptRoot 'Local022UiPhysicalPickWitness.cs'
if (-not (Test-Path -LiteralPath $witnessPath)) { throw 'FAIL: physical pick witness is missing.' }
$source = Get-Content -LiteralPath $witnessPath -Raw
$testNamespace = 'Local022WitnessReplay' + [Guid]::NewGuid().ToString('N')
$source = $source.Replace('namespace QS3D.LocalQualification', 'namespace ' + $testNamespace)
$tests = @"
namespace $testNamespace {
    public static class WitnessTests {
        private static void Reject(System.Action action, string code) {
            try { action(); } catch (System.InvalidOperationException e) {
                if (e.Message != code) throw; return;
            }
            throw new System.Exception("Accepted invalid witness: " + code);
        }
        public static void Run() {
            var witness = new PhysicalPickWitness();
            var target = new PhysicalPickWitness.Point(12, 19, 0);
            Reject(() => witness.Observe(1, target, 3, true, true), "pick_not_armed");
            Reject(() => witness.Arm(0, target, 3), "pick_sequence_invalid");
            Reject(() => witness.Arm(1, new PhysicalPickWitness.Point(double.NaN, 0, 0), 3), "pick_nonfinite_point");
            Reject(() => witness.Arm(1, target, -1), "pick_baseline_invalid");
            witness.Arm(1, target, 3);
            Reject(() => witness.Arm(2, target, 3), "pick_already_armed");
            Reject(() => witness.RequireAccepted(), "pick_not_observed");
            Reject(() => witness.Observe(2, target, 3, true, true), "pick_sequence_mismatch");
            Reject(() => witness.Observe(1, target, 3, false, true), "pick_context_changed");
            Reject(() => witness.Observe(1, target, 3, true, false), "pick_cursor_mismatch");
            Reject(() => witness.Observe(1, target, 4, true, true), "pick_geometry_preexists");
            Reject(() => witness.Observe(1, new PhysicalPickWitness.Point(13, 19, 0), 3, true, true), "pick_target_mismatch");
            Reject(() => witness.Observe(1, new PhysicalPickWitness.Point(12, 19, double.PositiveInfinity), 3, true, true), "pick_nonfinite_point");
            witness.Observe(1, target, 3, true, true);
            var accepted = witness.RequireAccepted();
            if (accepted.X != 12 || accepted.Y != 19 || accepted.Z != 0) throw new System.Exception("Witness changed input");
            Reject(() => witness.Observe(1, target, 3, true, true), "pick_duplicate_result");
            Reject(() => witness.Arm(2, target, 4), "pick_already_armed");
            witness.Reset();
            Reject(() => witness.RequireAccepted(), "pick_not_observed");
            var next = new PhysicalPickWitness.Point(30, 7, 0);
            witness.Arm(2, next, 4);
            Reject(() => witness.Observe(1, target, 3, true, true), "pick_sequence_mismatch");
            witness.Observe(2, next, 4, true, true);
            if (witness.RequireAccepted().X != 30) throw new System.Exception("Previous pick leaked into next pick");
        }
    }
}
"@
Add-Type -TypeDefinition ($source + "`n" + $tests)
([type]($testNamespace + '.WitnessTests'))::Run()
Write-Output 'PASS: actual pick witness requires a pre-armed independent target, exact sequence/context/cursor and pre-geometry baseline; missing, stale, duplicate and mismatched evidence refuses.'

$ui = Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
foreach ($required in @(
    '_context.Document.Editor.PromptedForPoint += OnPromptedForPoint;',
    '_context.Document.Editor.PromptedForPoint -= OnPromptedForPoint;',
    '_pickWitness.Arm(_sequence + 1,',
    '_pickWitness.Observe(_sequence,',
    '_pickWitness.RequireAccepted();',
    'if (_pickObservationError != null) throw new ProbeException(_pickObservationError);'
)) {
    if (-not $ui.Contains($required)) { throw ('FAIL: actual UI controller is missing witness integration: ' + $required) }
}
Write-Output 'PASS: both shared UI hosts connect the witness to real Editor events and require it before placement acceptance.'
