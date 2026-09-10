#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
FILES = {
    "direct": SRC / "McpCadDirectModelRuntime.cs",
    "domain": SRC / "McpQs3dDomainRuntime.cs",
    "agent": SRC / "McpCadAgentRuntime.cs",
    "save": SRC / "McpNativeCurrentDocumentSave.cs",
    "server": SRC / "McpEmbeddedServerV2.cs",
    "popup": SRC / "McpPopupObserver.cs",
    "classifier": SRC / "McpPopupWindowClassifier.cs",
}


def block(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        return ""
    candidates = [text.find(marker, start + len(signature)) for marker in (
        "\n        private static ", "\n        internal static ", "\n        public static ")]
    candidates = [x for x in candidates if x >= 0]
    return text[start:min(candidates) if candidates else len(text)]


def require(errors, text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


def forbid(errors, text, tokens, label):
    for token in tokens:
        if token in text:
            errors.append(f"{label} contains forbidden token: {token}")


def main() -> int:
    missing = [p for p in FILES.values() if not p.is_file()]
    if missing:
        for p in missing:
            print("ERROR: missing", p.relative_to(ROOT))
        return 1
    src = {name: path.read_text(encoding="utf-8") for name, path in FILES.items()}
    errors = []

    extrude = block(src["direct"], "private static string Extrude")
    boolean = block(src["direct"], "private static string Boolean")
    open_entity = block(src["direct"], "private static Entity OpenEntity")
    require(errors, extrude, (
        "source.Closed", "source.IsPlanar", "source.Area",
        "source.Clone() as Curve", "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
        "kernelSource=transient-region", "cad_extrude validation failed",
    ), "cad_extrude production validation")
    forbid(errors, extrude, (
        "model.AppendEntity(region);", "kernelSource=database-resident-region",
    ), "cad_extrude temporary database residency")

    require(errors, boolean, (
        "target.Clone() as Solid3d", "operand.Clone() as Solid3d",
        "ExtentsOverlap", "target.CopyFrom(targetWorking)",
        "sources were preserved", "kernelInputs=detached-clones",
    ), "CAD Boolean atomic detached-kernel boundary")
    forbid(errors, boolean, (
        "target.BooleanOperation(operation, operand);",
        "kernelTarget=database-resident; kernelOperand=database-resident",
    ), "CAD Boolean live-database kernel mutation")
    require(errors, open_entity, (
        "entity.Database", "ReferenceEquals(entity.Database, database)",
    ), "entity database ownership fence")

    require(errors, src["domain"], (
        '"qs3d_project_bind"', '"qs3d_project_reload"',
        "createIfMissing", "ProjectContextCoordinator.GetOrCreate(document)",
        "ProjectContextCoordinator.Reload(document)",
    ), "QS3D persisted project bind/restore")
    require(errors, src["agent"], (
        'case "qs3d_project_bind"', 'case "qs3d_project_reload"',
        "McpQs3dDomainRuntime.Call(tool, args)",
    ), "QS3D project tool dispatch")

    selection = block(src["agent"], "private static string BuildSelectionJson")
    snapshot = block(src["agent"], "private static string BuildDatabaseSnapshotJson")
    stop = block(src["agent"], "private static string EmergencyStop")
    cancel = block(src["agent"], "private static string CancelCurrentCommand")
    forbid(errors, selection, ("catch { }",), "CAD selection silent native-read loss")
    forbid(errors, snapshot, ("catch { continue; }",), "CAD snapshot silent native-read loss")
    forbid(errors, stop, ("ex.Message",), "emergency-stop public exception leakage")
    forbid(errors, cancel, ("ex.Message",), "cancel-command public exception leakage")
    require(errors, src["save"], (
        "CommandCompletionTimeoutMilliseconds", "CommandEnded += OnCommandEnded",
        "CommandCancelled += OnCommandCancelled", "CommandFailed += OnCommandFailed",
        "CMDACTIVE", "DBMOD", "EnsureSameActiveDocumentAndPath",
        "Do not retry automatically",
    ), "native QSAVE fail-closed completion")

    require(errors, src["server"], (
        "TransportErrorKind", "SetLastTransportError", "request:error",
        "structuredContent", "_meta", "OAuth",
    ), "transport structured diagnostics/protocol surface")
    require(errors, src["popup"] + src["classifier"], (
        "warning", "error", "info",
    ), "popup severity classification")

    if errors:
        print("ERROR: MCP production-correctness preflight failed:")
        for error in errors:
            print(" -", error)
        return 1
    print("PASS: MCP production-correctness invariants are source-enforced.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
