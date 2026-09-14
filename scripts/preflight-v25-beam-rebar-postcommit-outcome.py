#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
builder = (ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs").read_text(encoding="utf-8")
command = (ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs").read_text(encoding="utf-8")
probe = (ROOT / "src/QS3D.BricsCAD.V25/LevelZRuntimeProbeCommands.cs").read_text(encoding="utf-8")

required_builder = [
    "internal struct BeamRebarBuildOutcome",
    "public int Count { get; }",
    "public bool PostCommitCleanupWarning { get; }",
    "public static BeamRebarBuildOutcome BuildSelected",
    "if (cadCommitted)",
    "return new BeamRebarBuildOutcome(totalBars, postCommitCleanupWarning: true);",
    "rollback.Restore(project);",
    "new AggregateException(operationError, restoreError)",
    "return new BeamRebarBuildOutcome(totalBars, postCommitCleanupWarning: false);",
]
required_command = [
    "var outcome = BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds);",
    "var count = outcome.Count;",
    "outcome.PostCommitCleanupWarning",
    "FinalizeUi(document, nativeDatabaseIdentity, message, outcome.PostCommitCleanupWarning);",
]

missing = [token for token in required_builder if token not in builder]
missing += [token for token in required_command if token not in command]
required_probe = [
    "var rebarOutcome = BeamRebarSolidBuilder.BuildSelected(document, project, new[] { sources.Beam.ObjectId });",
    "var rebarCount = rebarOutcome.Count;",
    "observedBeamRebarCount = rebarCount;",
    "Require(rebarCount == 4, \"Beam longitudinal rebar count\");",
    "\"beam_rebar_count=\" + rebarCount.ToString(CultureInfo.InvariantCulture)",
]
missing += [token for token in required_probe if token not in probe]

# Keep the compatibility form immutable from callers even though the struct itself is not marked readonly.
if "public int Count { get; set; }" in builder or "public bool PostCommitCleanupWarning { get; set; }" in builder:
    missing.append("BeamRebarBuildOutcome properties must remain getter-only")

committed_index = builder.find("if (cadCommitted)")
warning_index = builder.find("return new BeamRebarBuildOutcome(totalBars, postCommitCleanupWarning: true);", committed_index)
rollback_index = builder.find("rollback.Restore(project);", committed_index)
rethrow_index = builder.find("throw;", rollback_index)
normal_index = builder.find("return new BeamRebarBuildOutcome(totalBars, postCommitCleanupWarning: false);", rollback_index)
if min(committed_index, warning_index, rollback_index, rethrow_index, normal_index) < 0 or not (
    committed_index < warning_index < rollback_index < rethrow_index < normal_index
):
    missing.append("post-commit warning must exit before pre-commit rollback/rethrow; normal outcome follows catch")

# Fail closed if the command regresses to exposing cleanup as the generic mutation failure path.
call_index = command.find("var outcome = BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds);")
finalize_index = command.find("FinalizeUi(document, nativeDatabaseIdentity, message, outcome.PostCommitCleanupWarning);", call_index)
catch_index = command.find("catch (Exception)", finalize_index)
if min(call_index, finalize_index, catch_index) < 0 or not call_index < finalize_index < catch_index:
    missing.append("committed outcome must reach warning-aware UI finalization before generic mutation-failure catch")

if missing:
    print("ERROR: V25 Beam Rebar post-commit outcome preflight failed")
    for token in missing:
        print(f" - missing contract: {token}")
    sys.exit(1)

print("OK: V25 Beam Rebar distinguishes post-commit cleanup warning from mutation failure")
