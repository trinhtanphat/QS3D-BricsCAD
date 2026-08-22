# Cubicost-style MEP review workspace — BricsCAD V25

Updated: 2026-08-15 (UTC+7)  
Issue: #1666

## Product boundary

The full Cubicost-style feature family is deliberately split by responsibility:

- shared MEP recognition/takeoff/clash contracts plus BQ/cost/tender/progress/4D-5D contracts belong in `QS3D-Platform`;
- this repository owns BricsCAD-native selection, transactions, geometry, commands, view state, modeless UI and licensed V25/V26 qualification;
- Autodesk-native equivalents belong in `QS3D-AutoCAD`;
- standalone Windows CAD/BIM/QS host behavior belongs in `QS3D-CAD`.

The authoritative cross-product feature/ownership inventory is `QS3D-Platform/docs/CUBICOST-QS3D-FEATURE-MASTER-PLAN.md`. Existing `QS3D-BricsCAD/src/QS3D.Core` code remains valid during compatibility-first convergence; this native lane does not delete or bulk-rewrite it.

## Existing native MEP commands

The review workspace unifies the already-delivered BricsCAD V25 MEP flow:

- `QS3DMEPTAKEOFF` — selected-entity MEP quantity aggregation using real captured metrics and drawing-unit conversion;
- `QS3DMEPCLASH` — broad-phase hard/clearance clash detection from read-only native extents;
- `QS3DMEPCLASHLOCATE` — bounded clash-pair review and atomic two-handle implied selection;
- `QS3DMEPEXACTCLASH` — read-only native `Solid3d.CheckInterference` hard-clash narrow phase;
- `QS3DMEPEXACTCLASHHIGHLIGHT` — transient exact-pair highlight review with best-effort cleanup.

## New command: `QS3DMEPZOOMSELECTION`

The command fits the current selected live entities into the active BricsCAD view while leaving DWG/project data untouched.

Flow:

1. consume PICKFIRST or interactive selection through `EntitySnapshotReader`;
2. resolve live handles through `CadHandleService`;
3. open entities only with `OpenMode.ForRead` and aggregate finite `GeometricExtents`;
4. get the active `ViewTableRecord` through `Editor.GetCurrentView()`;
5. transform all eight WCS bounding-box corners into the current display coordinate system using the current view direction, target and twist;
6. preserve current aspect ratio, apply a bounded 15% margin, then update only `CenterPoint`, `Width` and `Height`;
7. commit the transient editor view through `Editor.SetCurrentView()`.

No entity is opened `ForWrite`; no geometry is appended, erased, transformed or boolean-modified.

## New command: `QS3DMEPREVIEW`

`QS3DMEPREVIEW` opens a modeless WPF workspace using the BricsCAD application modeless-window API. The window intentionally does **not** retain `Document`, `ObjectId`, `DBObject` or `Solid3d` references.

Every button resolves `Application.DocumentManager.MdiActiveDocument` at click time and queues the existing command through the active `Document.SendStringToExecute` path. The workspace exposes:

- Takeoff;
- Broad Clash;
- Clash Locate;
- Exact Clash;
- Exact Highlight;
- Zoom Selection;
- active-document refresh/status.

This design avoids cross-document native-object leakage while allowing the workspace to stay open when the user changes drawings.

## Persistent recognition profile

All MEP takeoff/broad-clash/exact-clash/exact-highlight commands now read `MepRecognitionProfileProvider.Current` instead of holding separate process-lifetime default-profile instances.

The user profile is stored under the Windows roaming application-data directory:

`QS3D/BricsCAD/V25/mep-recognition-profile.xml`

The modeless workspace includes a rule editor for:

- stable rule ID;
- priority;
- MEP / Structure / Architecture discipline;
- semantic category;
- Layer / BlockName / combined source scope;
- MEP kind when applicable;
- comma-separated recognition tokens.

### Profile integrity rules

- maximum serialized size: 512 KiB;
- maximum 500 rules;
- maximum 100 tokens per rule;
- XML DTD processing is prohibited and `XmlResolver` is disabled;
- invalid/corrupt profile content fails closed to the built-in default profile and reports the load error;
- duplicate rule IDs, invalid enums, missing tokens and invalid MEP-kind combinations are rejected by the existing Core recognition contracts;
- save writes a temporary file in the same directory and uses atomic replace/move semantics;
- profile persistence is user configuration only and does not create a QS3D project, sidecar or DWG mutation.

`Reset Default` writes the canonical default profile through the same atomic store so the configured state is explicit and reloadable.

## Read-only/native safety boundary

This lane must retain all of the following:

- no `OpenMode.ForWrite` in zoom/profile/workspace code;
- no `AppendEntity`, erase, transform or boolean mutation;
- no project bootstrap or sidecar/QSDB write from profile/workspace code;
- no background/thread-pool use of BricsCAD native objects;
- modeless UI resolves the active document only when an action is requested;
- recognition parsing is host-neutral Core behavior; only persistence/editor/view wiring is native-host code.

## LOCAL_ONLY V25 qualification

Source/static checks are remote-safe, but native view/modeless/runtime truth remains `LOCAL_ONLY` on licensed BricsCAD V25.

Validate the exact integrated SHA on disposable drawings:

1. open `QS3DMEPREVIEW`, switch between two DWGs, run each action and prove the click targets the current active document rather than the document that was active when the window opened;
2. run `QS3DMEPZOOMSELECTION` in World Top and at least two rotated 3D views; verify all selected geometry fits with margin and view direction/twist remain unchanged;
3. validate model-space and layout/paper-space behavior separately and record any BricsCAD viewport constraints rather than masking them;
4. zoom a point-like/tiny extents selection and a very large selection; verify finite nonzero view dimensions;
5. edit a profile rule, save, close/reopen BricsCAD, reload and prove takeoff/clash/exact/highlight all use the same persisted recognition result;
6. inject malformed XML, oversized profile, duplicate rule ID, ambiguous rule and invalid enum controls; verify safe refusal/default fallback with no crash;
7. interrupt/cancel each queued command and close the modeless window during normal idle state; verify no stale native reference or process instability;
8. run exact highlight then finish review; verify best-effort cleanup behavior documented by the existing exact-highlight lane;
9. verify no new project sidecar, semantic mutation or DWG entity mutation is attributable to the workspace/profile/zoom commands;
10. record exact plugin SHA/ProductVersion, BricsCAD V25 build, relevant BrxMgd/TD_Mgd versions, fixture descriptions and screenshots/logs without private absolute paths.

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until the exact integrated SHA receives licensed V25 evidence.
