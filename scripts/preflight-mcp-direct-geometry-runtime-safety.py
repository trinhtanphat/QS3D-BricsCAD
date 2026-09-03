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

# Direct extrusion must build exactly one transient Region from the closed planar curve and feed
# that Region to the solid kernel. Do not regress to passing a generic database Entity directly.
for token in (
    "Region.CreateFromCurves(new DBObjectCollection { source })",
    "regions.Count != 1",
    "Source curve must form exactly one closed planar region",
    "solid.CreateExtrudedSolid(region, new Vector3d(0d, 0d, height), new SweepOptions());",
    "region?.Dispose();",
):
    require(direct, token, "McpCadDirectModelRuntime.Extrude")

# Boolean evaluation must use a transient clone and only erase the original tool solid after the
# kernel operation succeeds. This prevents database-resident operand eInvalidInput regressions.
for token in (
    "var operandClone = operand.Clone() as Solid3d;",
    "target.BooleanOperation(operation, operandClone);",
    "if (!operand.IsErased) operand.Erase();",
    "operandClone.Dispose();",
):
    require(direct, token, "McpCadDirectModelRuntime.Boolean")
boolean_start = direct.find("private static string Boolean(")
boolean_end = direct.find("private static string Save()", boolean_start)
boolean_body = direct[boolean_start:boolean_end] if boolean_start >= 0 and boolean_end > boolean_start else ""
if boolean_body:
    kernel_at = boolean_body.find("target.BooleanOperation(operation, operandClone);")
    erase_at = boolean_body.find("if (!operand.IsErased) operand.Erase();")
    if kernel_at < 0 or erase_at < 0 or erase_at < kernel_at:
        errors.append("boolean tool solid must be erased only after transient-clone kernel success")
else:
    errors.append("unable to isolate Boolean implementation")

# Legacy Solid3d extents are bounded: detailed Solid3d inspection defers extents rather than
# evaluating GeometricExtents, and generic extents reads are caught so eNullExtents cannot escape.
for token in (
    'var boundedSolidInspect = extents && details && entity is Solid3d;',
    'if (boundedSolidInspect) builder.Append("null");',
    'else try { builder.Append(ExtentsJson(entity.GeometricExtents)); } catch { builder.Append("null"); }',
    'extentsDeferred',
):
    require(agent, token, "McpCadAgentRuntime.DescribeEntity")

if errors:
    print("FAIL: MCP direct geometry/runtime safety guard")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: REGENALL/modal native dispatch is fail-closed, direct extrusion remains Region-backed, Boolean operands remain transient clones, and Solid3d extents stay bounded against eNullExtents.")
