#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
AGENT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"

errors = []
coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.is_file() else ""
direct = DIRECT.read_text(encoding="utf-8") if DIRECT.is_file() else ""
agent = AGENT.read_text(encoding="utf-8") if AGENT.is_file() else ""


def require(text, token, where):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")


require(coordinator, "RejectUnsafeNativeCommand(command);", "McpCadMutationCoordinator")
require(coordinator, 'string.Equals(normalized, "REGENALL", StringComparison.Ordinal)', "McpCadMutationCoordinator")
require(coordinator, 'Application.GetSystemVariable("CMDACTIVE")', "McpCadMutationCoordinator")
require(coordinator, "(commandActive & 8) != 0", "McpCadMutationCoordinator")

prepare_start = coordinator.find("internal static NativeCommandReservation? PrepareNativeCommand")
prepare_end = coordinator.find("internal static void QueueNativeCommand", prepare_start)
prepare = coordinator[prepare_start:prepare_end] if prepare_start >= 0 and prepare_end > prepare_start else ""
if not prepare:
    errors.append("unable to isolate PrepareNativeCommand")
else:
    reject_at = prepare.find("RejectUnsafeNativeCommand(command);")
    gate_at = prepare.find("MutationGate.Wait(")
    if reject_at < 0 or gate_at < 0 or reject_at > gate_at:
        errors.append("REGENALL rejection must occur before MutationGate acquisition")

extrude_start = direct.find("private static string Extrude(")
extrude_end = direct.find("private static string Boolean(", extrude_start)
extrude_body = direct[extrude_start:extrude_end] if extrude_start >= 0 and extrude_end > extrude_start else ""
if not extrude_body:
    errors.append("unable to isolate Extrude implementation")
else:
    for token in (
        "var profileClone = source.Clone() as Curve;",
        "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
        "solid.Extrude(region, height, 0d);",
        "region?.Dispose();",
        "profileClone.Dispose();",
        "kernelSource=transient-region",
    ):
        require(extrude_body, token, "McpCadDirectModelRuntime.Extrude V25 transient-region topology")
    for forbidden in (
        "model.AppendEntity(profileClone);",
        "solid.CreateExtrudedSolid(profileClone",
        "solid.CreateExtrudedSolid(sourceClone",
        "kernelSource=database-resident-profile-clone",
        "kernelSource=transient-curve-clone",
    ):
        if forbidden in extrude_body:
            errors.append("V25 extrusion topology retains forbidden live-regression token: " + forbidden)

boolean_start = direct.find("private static string Boolean(")
boolean_end = direct.find("private static string Save()", boolean_start)
boolean_body = direct[boolean_start:boolean_end] if boolean_start >= 0 and boolean_end > boolean_start else ""
if not boolean_body:
    errors.append("unable to isolate Boolean implementation")
else:
    for token in (
        "var operandClone = operand.Clone() as Solid3d;",
        "target.BooleanOperation(operation, operandClone);",
        "if (!operand.IsErased) operand.Erase();",
        "operandClone.Dispose();",
        "kernelTarget=database-resident; kernelOperand=transient-clone",
    ):
        require(boolean_body, token, "McpCadDirectModelRuntime.Boolean V25 target/transient-operand topology")
    kernel_at = boolean_body.find("target.BooleanOperation(operation, operandClone);")
    erase_at = boolean_body.find("if (!operand.IsErased) operand.Erase();")
    if kernel_at < 0 or erase_at < 0 or kernel_at > erase_at:
        errors.append("boolean ordering must be target kernel success -> tool erase")
    for forbidden in (
        "model.AppendEntity(targetWorking);",
        "model.AppendEntity(operandWorking);",
        "targetWorking.BooleanOperation(operation, operandWorking);",
        "target.HandOverTo(resultClone",
        "targetClone.BooleanOperation(operation, operandClone);",
        "kernelInputs=database-resident-working-clones",
    ):
        if forbidden in boolean_body:
            errors.append("V25 boolean topology retains forbidden live-regression token: " + forbidden)

for token in (
    'var boundedSolidExtents = extents && entity is Solid3d;',
    'if (boundedSolidExtents) builder.Append("null");',
    'else try { builder.Append(ExtentsJson(entity.GeometricExtents)); } catch { builder.Append("null"); }',
    'if (boundedSolidExtents) builder.Append(",\\"extentsDeferred\\":true");',
):
    require(agent, token, "McpCadAgentRuntime.DescribeEntity")

if errors:
    print("FAIL: MCP direct geometry/runtime safety guard")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: REGENALL/modal native dispatch is fail-closed, V25 direct kernels use transient Region/operand safety boundaries, and Solid3d extents stay bounded against eNullExtents.")
