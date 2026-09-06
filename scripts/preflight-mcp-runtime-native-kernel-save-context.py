#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
DIRECT = SRC / "McpCadDirectModelRuntime.cs"
NATIVE_SAVE = SRC / "McpNativeCurrentDocumentSave.cs"
DOMAIN = SRC / "McpQs3dDomainRuntime.cs"


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        return ""
    next_method = source.find("\n        private static ", start + len(signature))
    return source[start:] if next_method < 0 else source[start:next_method]


def require(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


def forbid(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    for token in tokens:
        if token in text:
            errors.append(f"{label} still contains forbidden token: {token}")


def main() -> int:
    missing = [path for path in (DIRECT, NATIVE_SAVE, DOMAIN) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    direct = DIRECT.read_text(encoding="utf-8")
    native_save = NATIVE_SAVE.read_text(encoding="utf-8")
    domain = DOMAIN.read_text(encoding="utf-8")
    errors: list[str] = []

    extrude = method_block(direct, "private static string Extrude")
    boolean = method_block(direct, "private static string Boolean")
    status = method_block(domain, "internal static string BuildStatusJson")

    require(errors, extrude, (
        "var profileClone = source.Clone() as Curve;",
        "model.AppendEntity(profileClone);",
        "transaction.AddNewlyCreatedDBObject(profileClone, true);",
        "solid.CreateExtrudedSolid(profileClone, new Vector3d(0d, 0d, height), new SweepOptions());",
        "if (!profileClone.IsErased) profileClone.Erase();",
        "kernelSource=database-resident-profile-clone",
    ), "database-resident extrusion profile")
    forbid(errors, extrude, (
        "kernelSource=transient-curve-clone",
        "solid.CreateExtrudedSolid(sourceClone",
    ), "licensed extrusion regression")

    require(errors, boolean, (
        "var targetWorking = target.Clone() as Solid3d;",
        "var operandWorking = operand.Clone() as Solid3d;",
        "model.AppendEntity(targetWorking);",
        "model.AppendEntity(operandWorking);",
        "targetWorking.BooleanOperation(operation, operandWorking);",
        "var resultClone = targetWorking.Clone() as Solid3d;",
        "target.HandOverTo(resultClone, true, true);",
        "if (!targetWorking.IsErased) targetWorking.Erase();",
        "if (!operandWorking.IsErased) operandWorking.Erase();",
        "if (!operand.IsErased) operand.Erase();",
        "kernelInputs=database-resident-working-clones",
    ), "database-resident boolean working set")
    forbid(errors, boolean, (
        "targetClone.BooleanOperation(operation, operandClone);",
        "target=transient-clone; operand=transient-clone",
    ), "licensed boolean regression")

    require(errors, native_save, (
        "document.Editor.Command(\"_.QSAVE\");",
        "WaitForCleanDbmod",
        "Do not retry automatically",
        "DbmodPersistentContentMask = 1 | 4 | 32",
    ), "synchronous native QSAVE")
    forbid(errors, native_save, (
        "document.SendStringToExecute(",
        "McpCadMutationCoordinator.QueueNativeCommand(",
        "ManualResetEventSlim",
    ), "queued QSAVE regression")
    if "Database.Save();" in native_save or "Database.SaveAs(" in native_save:
        errors.append("current-document QSAVE helper must never write the active path through Database.Save/SaveAs")

    require(errors, status, (
        "ExistingProjectMutationContext.TryGet(document, out project)",
        "No persisted QS3D project context",
    ), "persisted project-context hydration")
    forbid(errors, status, (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "No cached QS3D project context",
    ), "project-context fabrication/cold-cache regression")

    if errors:
        print("ERROR: licensed MCP runtime regression guard failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP direct 3D kernels use database-resident working inputs, current-document save executes one synchronous native QSAVE with DBMOD verification, and QS3D status binds only an existing persisted project on a cold cache.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
