# LOCAL-008 P03 — production repeated Direct Draw and exact V25/V26 lifecycle

Status: `COMPLETED / LOCAL_PASS`

Lane-Key: `issue-3612`

Carrier issue: #3612

Parent issues: #72, #74, #80, #1462

Canonical branch: `agent/interactive-20260823-01a0294a/issue-3612-repeated-directdraw-v25-v26`

Exact qualified source SHA: `2985f13b0f0d680284e915fb81728bbb26a42ffe`

Refreshed baseline represented by the candidate: `origin/main@74f2f26e175451d22b64831937abe4ff22c2b435`

## Boundary delivered

This lane adds production repeated Wall/Beam authoring through `QS3DDRAWWALLREPEAT`, `QS3DDRAWBEAMREPEAT` and the active-Family route `QS3DDRAWACTIVEREPEAT`. A database-free `DrawJig` renders the transient profile strip. Every accepted segment reuses the canonical Direct Draw source, semantic ownership and native `Solid3d` pipeline; the command owns one matching semantic/native Undo group instead of creating a second geometry model.

The repeated loop revalidates active DWG, Model Space, drawing units, planar UCS, editor, project and active Family before every commit. Enter and physical ESC remove only the in-progress transient. Accepted segments remain, a cancelled first segment leaves no project/CAD/semantic residue, and a document switch refuses the pending commit without cross-DWG mutation.

The V26 runner now fails before artifact creation or host launch unless a matching x64 .NET 8 Core and Windows Desktop Runtime is installed under the system runtime root. A portable `DOTNET_ROOT` must be complete and match that installed patch. This closes the opaque V26 managed-bridge failure where `BrxMgd` could not resolve `WindowsBase.dll` and `System.Windows.Forms.dll` before QS3D initialization.

## Exact licensed evidence

Both repeated-mode runs used the same clean exact source SHA and repository-generated disposable fixture SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.

- BricsCAD V25.2.10 loaded the exact x64 Release plugin SHA-256 `C94A5ED5C8EA4CC039EE364B1DA021005AD07514F93BA60D091DFC88519C14E6` by `NETLOAD`. Two segments, database-free DrawJig preview, Enter, exact-process physical ESC, planar UCS, document-switch isolation, whole-command native Undo/Redo, QS3D/DWG save, fresh-process cold reopen and drawing/sidecar persistence all passed. V25 DemandLoad isolation/restoration passed with five registrations observed and `LoadCtrls=2` restored.
- BricsCAD V26.2.07 / CLR 8.0.29 loaded the exact net8.0-windows plugin SHA-256 `5CF8D3BC67D36CFFE6EACEC0013C1E615A21C9A85E17E85A50B3058582A9B2D1`. The same repeated/ESC/UCS/document/Undo/Redo/save/cold-reopen matrix passed with exact candidate identity in every session.
- The standalone V26 identity gate passed on that same binary with native runtime major/label/match true, Ribbon ready, Workspace and Right Panel visible, and the Quantity Insight palette intentionally hidden.
- The enhanced V26 native LINE lifecycle passed on the same SHA/binary: production Direct Draw, native MOVE/ROTATE/top-level STRETCH, source reconcile/rebuild, native Undo/Redo, two-DWG wrong-document refusal/isolation/reactivation/close, save, sidecar persistence and fresh-process cold reopen.

The current-candidate V25 native-edit matrix also passed on the same source SHA and x64 plugin:

- P01: LINE MOVE/ROTATE/top-level STRETCH, reconcile/rebuild, save and cold reopen;
- P02: one closed Slab POLYLINE vertex STRETCH with pre-sync isolation, area/perimeter/quantity reconciliation, invalidation/rebuild, Health and cold reopen;
- P03: Beam native MOVE with host, four longitudinal bars and six stirrups rebuilt coherently;
- P04: Beam endpoint crossing-window STRETCH from 5 m to 8 m with host/rebar invalidation, four longitudinal replacements and stirrup redistribution from six to nine, then cold reopen;
- P05: a real hot endpoint grip entered native STRETCH, physical ESC preserved the 5 m baseline, a second real grip committed 8 m, production reconcile/rebuild passed, and fresh-process reopen reported `prior_session_phases_replayed=false`.

## Exact palette, Excel and CAD selection evidence

A clean disposable DWG was authored by BricsCAD itself with one 5 m LINE on the recognized `beam` layer. Its SHA-256 remained `6D6B7E7EBE11CB23DDD6021C4040F72D7AD30CC1AA4F93A2D34A1F585B4866DC` throughout the qualification and no sidecar was persisted.

On the exact V25 plugin, production `QS3DB4D` created one Beam semantic element; ED2 export created one `CHI_TIET` row and one `TONG_HOP` row; the workbook row retained drawing fingerprint, Element ID and Handle provenance; Excel Locate resolved and selected exactly one live CAD object through PICKFIRST. Wrong fingerprint, unknown Element ID, stale Handle `0/1` and partial Handle resolution `1/2` were all refused while preserving the prior selection and semantic state. The final generated workbook SHA-256 was `C8F2ADA5A3D05F903B6CC57AED14204C92655BC0A49DA6209A8B44E743A6C696`.

An ignored local reflection probe then reused the production workbook reader/resolver and real native selection bridge. The visible Workspace received exactly one matching CAD snapshot, displayed `1 chọn`, resolved the selected semantic Instance and Family, and remained bound to plugin SHA-256 `C94A5ED5C8EA4CC039EE364B1DA021005AD07514F93BA60D091DFC88519C14E6`. The private probe, workbook, scripts, screenshots, drawing and raw command logs remain Git-ignored; only this sanitized aggregate is committed.

## Validation and safety

- V25 and V26 `Release|x64` builds preceding licensed execution completed with zero warnings and zero errors.
- Exact-source/PDB binding, focused repeated/V26/native-edit guards and PowerShell syntax checks passed before runtime.
- Every qualifying runner restored its disposable drawing, scripts, process environment and private state as applicable. Final process count was zero and the installed V25 loader path plus `LoadCtrls=2` were restored.
- No private/customer DWG, proprietary BricsCAD binary, raw Handle, ProjectId, ElementId, machine path, workbook, screenshot or signing material is committed.
- No manual Actions dispatch/rerun/cancel, release publication or direct `main` write was performed.

## Remaining parent scope

Issue #3612 closes this bounded P03 implementation and exact native matrix after its protected task-branch PR integrates. Parent #72 remains open for its broader full interactive/private-DWG/customer-release matrix; the missing historical BRC proxy input was not reconstructed or represented as current proxy parity. Parent #74 remains open for the full quick/advanced per-prompt cancellation and context-drift/Auto Host/reference matrix. Parent #80 remains open for broader topology/category/dependent/failure matrices beyond P01-P05. Parent #1462 remains open for private-DWG, clean-machine packaging/signing and separately authorized release qualification.
