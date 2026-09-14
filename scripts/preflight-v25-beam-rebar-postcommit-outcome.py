#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
builder = (ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs").read_text(encoding="utf-8")
command = (ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs").read_text(encoding="utf-8")

required_builder = [
    "internal readonly struct BeamRebarBuildOutcome",
    "public int Count { get; }",
    "public bool PostCommitCleanupWarning { get; }",
    "public static BeamRebarBuildOutcome BuildSelected",
    "if (cadCommitted)",
    "return new BeamRebarBuildOutcome(totalBars, postCommitCleanupWarning: true);",
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

# The old defect is specifically a committed branch that falls through to `throw;`.
old_tail = "if (!cadCommitted)\n                {"
if old_tail not in builder:
    missing.append("pre-commit rollback branch remains explicit")

if missing:
    print("ERROR: V25 Beam Rebar post-commit outcome preflight failed")
    for token in missing:
        print(f" - missing contract: {token}")
    sys.exit(1)

print("OK: V25 Beam Rebar distinguishes post-commit cleanup warning from mutation failure")
