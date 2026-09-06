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
    db_resident = "kernelSource=database-resident-profile-clone" in extrude_body
    if db_resident:
        for token in (
            "var profileClone = source.Clone() as Curve;",
            "model.AppendEntity(profileClone);",
            "transaction.AddNewlyCreatedDBObject(profileClone, true);",
            "solid.CreateExtrudedSolid(profileClone, new Vector3d(0d, 0d, height), new SweepOptions());",
            "if (!profileClone.IsErased) profileClone.Erase();",
            "kernelSource=database-resident-profile-clone",
        ):
            require(extrude_body, token, "McpCadDirectModelRuntime.Extrude database-resident topology")
        for forbidden in ("kernelSource=transient-curve-clone", "solid.CreateExtrudedSolid(sourceClone", "Region.CreateFromCurves"):
            if forbidden in extrude_body:
                errors.append("database-resident extrusion topology retains forbidden token: " + forbidden)
    else:
        for token in (
            "var sourceClone = source.Clone() as Curve;",
            "solid.CreateExtrudedSolid(sourceClone, new Vector3d(0d, 0d, height), new SweepOptions());",
            "sourceClone.Dispose();",
            "kernelSource=transient-curve-clone",
        ):
            require(extrude_body, token, "McpCadDirectModelRuntime.Extrude transient-safe topology")
        if "Region.CreateFromCurves" in extrude_body:
            errors.append("cad_extrude must not route the profile through Region.CreateFromCurves")
        if "solid.CreateExtrudedSolid(source," in extrude_body:
            errors.append("cad_extrude must not feed the original database-resident Curve directly to CreateExtrudedSolid")

boolean_start = direct.find("private static string Boolean(")
boolean_end = direct.find("private static string Save()", boolean_start)
boolean_body = direct[boolean_start:boolean_end] if boolean_start >= 0 and boolean_end > boolean_start else ""
if not boolean_body:
    errors.append("unable to isolate Boolean implementation")
else:
    db_resident = "kernelInputs=database-resident-working-clones" in boolean_body
    if db_resident:
        for token in (
            "var targetWorking = target.Clone() as Solid3d;",
            "var operandWorking = operand.Clone() as Solid3d;",
            "model.AppendEntity(targetWorking);",
            "model.AppendEntity(operandWorking);",
            "targetWorking.BooleanOperation(operation, operandWorking);",
            "resultClone = targetWorking.Clone() as Solid3d;",
            "target.HandOverTo(resultClone, true, true);",
            "if (!targetWorking.IsErased) targetWorking.Erase();",
            "if (!operandWorking.IsErased) operandWorking.Erase();",
            "if (!operand.IsErased) operand.Erase();",
        ):
            require(boolean_body, token, "McpCadDirectModelRuntime.Boolean database-resident topology")
        kernel_at = boolean_body.find("targetWorking.BooleanOperation(operation, operandWorking);")
        handover_at = boolean_body.find("target.HandOverTo(resultClone, true, true);")
        erase_at = boolean_body.find("if (!operand.IsErased) operand.Erase();")
        if kernel_at < 0 or handover_at < 0 or erase_at < 0 or not (kernel_at < handover_at < erase_at):
            errors.append("boolean ordering must be DB-resident kernel success -> target identity handover -> tool erase")
        if "targetClone.BooleanOperation(operation, operandClone);" in boolean_body:
            errors.append("database-resident boolean topology must not evaluate detached transient clones")
    else:
        for token in (
            "var targetClone = target.Clone() as Solid3d;",
            "var operandClone = operand.Clone() as Solid3d;",
            "targetClone.BooleanOperation(operation, operandClone);",
            "target.HandOverTo(targetClone, true, true);",
            "handedOver = true;",
            "if (!operand.IsErased) operand.Erase();",
            "operandClone.Dispose();",
            "if (!handedOver) targetClone.Dispose();",
        ):
            require(boolean_body, token, "McpCadDirectModelRuntime.Boolean transient-safe topology")
        kernel_at = boolean_body.find("targetClone.BooleanOperation(operation, operandClone);")
        handover_at = boolean_body.find("target.HandOverTo(targetClone, true, true);")
        erase_at = boolean_body.find("if (!operand.IsErased) operand.Erase();")
        if kernel_at < 0 or handover_at < 0 or erase_at < 0 or not (kernel_at < handover_at < erase_at):
            errors.append("boolean ordering must be transient kernel success -> target identity handover -> tool erase")

    if "target.BooleanOperation(operation" in boolean_body:
        errors.append("direct boolean must not execute the kernel against the original target Solid3d")

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

print("PASS: REGENALL/modal native dispatch is fail-closed, direct kernel inputs follow the admitted safe topology, and Solid3d extents stay bounded against eNullExtents.")