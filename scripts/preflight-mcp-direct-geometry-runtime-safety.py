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


# Native command safety: REGENALL must be rejected before the process-global writer gate is
# acquired, and all native command arming must fail closed while CMDACTIVE modal bit 8 is set.
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

# Direct extrusion must detach the database-resident Curve before entering the BricsCAD solid
# kernel. Region.CreateFromCurves is deliberately forbidden because the licensed V25 regression
# reproduced on that conversion path even when native EXTRUDE accepted the same profile.
extrude_start = direct.find("private static string Extrude(")
extrude_end = direct.find("private static string Boolean(", extrude_start)
extrude_body = direct[extrude_start:extrude_end] if extrude_start >= 0 and extrude_end > extrude_start else ""
if not extrude_body:
    errors.append("unable to isolate Extrude implementation")
else:
    for token in (
        "var sourceClone = source.Clone() as Curve;",
        "solid.CreateExtrudedSolid(sourceClone, new Vector3d(0d, 0d, height), new SweepOptions());",
        "sourceClone.Dispose();",
        "kernelSource=transient-curve-clone",
    ):
        require(extrude_body, token, "McpCadDirectModelRuntime.Extrude")
    if "Region.CreateFromCurves" in extrude_body:
        errors.append("cad_extrude must not route the profile through Region.CreateFromCurves")
    if "solid.CreateExtrudedSolid(source," in extrude_body:
        errors.append("cad_extrude must not feed the database-resident Curve directly to CreateExtrudedSolid")

# Boolean evaluation must detach BOTH kernel inputs. The successful transient target clone then
# hands its body back to the original target identity before the original tool solid is erased.
boolean_start = direct.find("private static string Boolean(")
boolean_end = direct.find("private static string Save()", boolean_start)
boolean_body = direct[boolean_start:boolean_end] if boolean_start >= 0 and boolean_end > boolean_start else ""
if not boolean_body:
    errors.append("unable to isolate Boolean implementation")
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
        require(boolean_body, token, "McpCadDirectModelRuntime.Boolean")
    if "target.BooleanOperation(operation" in boolean_body:
        errors.append("direct boolean must not execute the kernel against the database-resident target Solid3d")
    kernel_at = boolean_body.find("targetClone.BooleanOperation(operation, operandClone);")
    handover_at = boolean_body.find("target.HandOverTo(targetClone, true, true);")
    erase_at = boolean_body.find("if (!operand.IsErased) operand.Erase();")
    if kernel_at < 0 or handover_at < 0 or erase_at < 0 or not (kernel_at < handover_at < erase_at):
        errors.append("boolean ordering must be transient kernel success -> target identity handover -> tool erase")

# Solid3d extents are bounded for every extents request, including database snapshots where
# details=false. Generic extents reads remain caught so eNullExtents cannot escape.
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

print("PASS: REGENALL/modal native dispatch is fail-closed, direct extrusion evaluates a transient Curve clone without Region conversion, Boolean kernels evaluate detached target/tool clones before target identity handover and tool consumption, and Solid3d extents stay bounded against eNullExtents across detailed inspection and database snapshots.")
