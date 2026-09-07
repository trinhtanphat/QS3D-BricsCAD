#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODEL = (ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs").read_text(encoding="utf-8")
SAVE = (ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs").read_text(encoding="utf-8")
STATUS = (ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs").read_text(encoding="utf-8")
COORDINATOR = (ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs").read_text(encoding="utf-8")

required_model = (
    "Region.CreateFromCurves(new DBObjectCollection { source })",
    "model.AppendEntity(region)",
    "target.BooleanOperation(operation, operand)",
    "kernelSource=database-resident-region",
    "kernelOperand=database-resident",
)
for token in required_model:
    if token not in MODEL:
        raise SystemExit(f"#5969 direct V25 kernel residency contract missing: {token}")

for forbidden in (
    "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
    "target.BooleanOperation(operation, operandClone)",
    "kernelSource=transient-region",
    "kernelOperand=transient-clone",
):
    if forbidden in MODEL:
        raise SystemExit(f"#5969 stale live-failing kernel route remains: {forbidden}")

required_save = (
    "QueueNativeCommand(",
    "document.SendStringToExecute(\"_.QSAVE\\n\", true, false, true)",
    "CommandEnded += OnCommandEnded",
    "CommandCancelled += OnCommandCancelled",
    "CommandFailed += OnCommandFailed",
)
for token in required_save:
    if token not in SAVE:
        raise SystemExit(f"#5969 native QSAVE terminal ownership contract missing: {token}")
if 'document.Editor.Command("_.QSAVE")' in SAVE:
    raise SystemExit("#5969 live-failing ExecuteInCommandContextAsync/Editor.Command QSAVE route remains")

if "var nextStep = currentAction.Length > 0" not in STATUS:
    raise SystemExit("#5969 inactive agent_status must suppress historical nextStep")
if 'lifecycle.UpdatedUtc == DateTime.MinValue ? "null"' not in STATUS:
    raise SystemExit("#5969 cad_command_state must emit JSON null before the first lifecycle timestamp")

# Live -PURGE reproduced a durable writer quarantine because BricsCAD lifecycle events can
# report PURGE while the queued dispatch token is -PURGE. Lifecycle identity must remove
# command-line/global prefixes without mutating the actual command string sent to BricsCAD.
required_coordinator = (
    "NormalizeLifecycleCommand",
    "PendingMatchesLocked",
    "NormalizeLifecycleCommand(e == null ? string.Empty : e.GlobalCommandName)",
    "NormalizeLifecycleCommand(_pending.Command)",
    "value[index] == '-'",
)
for token in required_coordinator:
    if token not in COORDINATOR:
        raise SystemExit(f"#5969 hyphenated native-command lifecycle/barrier contract missing: {token}")

print("PASS #5969 live V25 CAD kernel/save/status/native-barrier residual contracts")
