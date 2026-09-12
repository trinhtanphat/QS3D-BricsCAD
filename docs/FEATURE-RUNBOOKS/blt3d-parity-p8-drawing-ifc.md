# BLT3D parity P8 — Drawing Manager and IFC interoperability

## Scope

P8 continues the approved clean-room BLT3D parity program after P7. It records current QS3D source wiring for Drawing Manager/Xref and IFC workflows without copying recovered BLT3D implementation or introducing a second CAD/IFC engine.

The repository manifest remains `# catalog-complete=false`. P8 advances only `drawing-manager` and `ifc` to `CommandWired`; it does not claim `SemanticBehaviorPass`, `SaveReopenPass`, `V25V26ParityPass`, or full BLT3D parity.

## Drawing Manager evidence

Current QS3D exposes the right-panel `QUẢN LÝ BẢN VẼ` surface and an Xref lifecycle backed by current BricsCAD-host code:

- attach through native `_XATTACH`;
- select current-space Xref instances;
- reload through `XrefService.Reload`;
- move selected instances through native `_MOVE`;
- detach through `XrefService.Detach`;
- lock/unlock Xref instance layers through `XrefService.SetInstanceLayersLocked`.

This is sufficient for `CommandWired` routing evidence. P8 does not claim complete rename or clip parity; those remain later behavioral/runtime qualification items.

## IFC evidence

Current QS3D exposes explicit IFC entry points in `ReferenceUiCommands`:

- `QS3DIFCIMPORT` → native BricsCAD `_IMPORT`;
- `QS3DIFCIMPORTLIGHT` → the same native import path with operator-selected IFC/Xref settings;
- `QS3DIFCREMOVE` → native `_XREF` management;
- `QS3DIFCEXPORT` → native BricsCAD `_IFCEXPORT`.

P8 deliberately reuses the installed BricsCAD host engine. It does not assert IFC schema coverage, property/classification mapping fidelity, lightweight-import availability on every edition, or IFC round-trip equivalence.

## Host-neutral parity binding

`ParityDrawingInteroperabilityCatalog` contains exactly two bindings:

- `drawing-manager`
- `ifc`

Both are `Infrastructure` workflows on the `Ui` surface requiring `ActiveDocument`. The catalog is routing/evidence metadata only; it contains no BricsCAD/Teigha/WPF dependency and does not represent semantic project mutation.

## Verification

Run from repository root:

```powershell
python scripts/preflight-blt3d-parity-p8-drawing-ifc.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
git diff --check
```

The focused gate verifies canonical manifest shape, both `CommandWired` rows, exactly two host-neutral bindings, Drawing Manager/Xref source evidence, all four IFC command entry points, and this clean-room/evidence-ceiling contract.

## Evidence ceiling

A later carrier must independently qualify rename, clip, save/reopen, exact V25/V26 runtime behavior, native IFC option/edition availability, IFC identity/schema/property fidelity, and round-trip behavior before advancing beyond `CommandWired`.