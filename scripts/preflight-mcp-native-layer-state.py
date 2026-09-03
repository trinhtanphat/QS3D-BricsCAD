#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadLayerStateRuntime.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL mcp native layer state: missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL mcp native layer state: forbidden {label}: {needle}")


def main() -> None:
    direct = DIRECT.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")

    # Published direct-runtime extension: reads do not enter mutation admission, writes do.
    require(direct, "McpCadLayerStateRuntime.IsTool(tool)", "layer-state routing")
    require(direct, "McpCadLayerStateRuntime.RequiresMutation(tool)", "per-tool mutation classification")
    require(direct, "McpCadLayerStateRuntime.ToolDescriptors()", "tool descriptor publication")
    require(direct, "McpCadLayerStateRuntime.CallInCadContext(tool, body)", "CAD-context dispatch")

    for tool in (
        "cad_layer_state",
        "cad_layer_set_state",
        "cad_layer_snapshot",
        "cad_layer_restore",
    ):
        require(runtime, f'"{tool}"', f"{tool} registration")

    require(runtime, '"cad_layer_set_state",\n            "cad_layer_restore"', "mutation-only tool set")
    forbid(runtime, '"cad_layer_state",\n            "cad_layer_snapshot"', "read tools in mutation-only tool set")

    # V26 intentionally consumes the V25 adapter source tree. Lock that parity so the new
    # layer-state runtime cannot silently disappear from V26 while V25 remains green.
    require(v26_project, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 linked V25 source wildcard")
    forbid(v26_project, "McpCadLayerStateRuntime.cs", "V26 layer-state source exclusion")

    # Native writes are document locked and transactional; complete restore validation occurs
    # before the first layer is opened for write, so stale snapshots cannot partially apply.
    require(runtime, "using (document.LockDocument())", "document locking")
    require(runtime, "StartTransaction()", "atomic native transaction")
    require(runtime, "Validate the complete restore set before opening any layer for write", "restore prevalidation")
    validation = runtime.index("Validate the complete restore set before opening any layer for write")
    first_restore_write = runtime.index("OpenMode.ForWrite", validation)
    commit = runtime.index("transaction.Commit();", first_restore_write)
    if not (validation < first_restore_write < commit):
        raise SystemExit("FAIL mcp native layer state: restore validation/write/commit order regressed")

    # Current-layer invariants must fail closed both for direct set and restore.
    require(runtime, "The current layer cannot be turned off or frozen", "direct current-layer safety")
    require(runtime, "Snapshot would turn off or freeze the current layer", "restore current-layer safety")
    require(runtime, "Snapshot current-layer identity does not match the active current layer", "current-layer identity safety")
    require(runtime, "Layer does not exist:", "invalid direct layer rejection")
    require(runtime, "Snapshot layer no longer exists:", "stale snapshot layer rejection")

    # Snapshot format stays bounded, versioned and opaque; layer names are encoded rather than
    # delimiter-concatenated so unusual legal names cannot corrupt the restore grammar.
    require(runtime, 'SnapshotVersion = "QS3D-LAYER-STATE-V1"', "snapshot version")
    require(runtime, "MaxSnapshotLayers = 4096", "snapshot layer bound")
    require(runtime, "MaxSnapshotTokenLength = 512 * 1024", "snapshot token bound")
    require(runtime, "ToBase64(entry.Name)", "layer-name-safe snapshot encoding")
    require(runtime, "names.Add(name)", "case-insensitive duplicate snapshot rejection")

    print("PASS mcp native layer state")


if __name__ == "__main__":
    main()
