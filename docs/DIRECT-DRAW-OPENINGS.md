# QS3D Direct Draw Door / Opening — implementation handoff

Updated: 2026-08-10 (UTC+7)

## Status

`QS3DDRAWDOOR` and `QS3DDRAWOPENING` are implemented in source, exposed through the BricsCAD-hosted `TẠO MỚI` Ribbon and Full Domain Hub, and protected by `scripts/preflight-direct-draw-openings.py`.

This is **source implementation / static contract coverage**, not licensed BricsCAD V25 runtime proof.

## Product flow

Both commands keep BricsCAD as the CAD host and create a real DWG source LINE. The picked two plan-view edge points define the opening width; QS3D does not create a fake semantic-only opening.

```text
QS3DDRAWDOOR / QS3DDRAWOPENING
-> pick edge 1
-> pick edge 2
-> source LINE in Model Space
-> WidthM from plan length
-> prompt/inherit HeightM
-> prompt/inherit SillHeightM / BottomOffsetM
-> prompt/inherit BooleanClearanceM
-> semantic capture
-> deterministic regeneration
-> selection-scoped Auto Host
-> verify HostWallId
-> deterministic post-link regeneration
-> commit project/source state
-> best-effort UI sync
```

## Safety contract

- Model Space only.
- Shared 5 mm unit-aware planarity tolerance.
- Source coordinates and derived width must be finite.
- Explicit invalid/non-finite/negative Family numeric configuration fails closed; it is not silently masked by a fallback.
- Instance writes use `ProjectElement.SetProperty()` so dirty/stale/quantity/geometry lifecycle remains on the canonical Core path.
- The active DWG is revalidated before nested Auto Host and immediately after it.
- Only the newly created source is selected before `QS3DAUTOLINKHOSTS`, preventing this authoring operation from re-hosting unrelated Door/Opening elements.
- Unmatched or ambiguous Auto Host leaves no orphan Door/Opening: source CAD is erased by exact `ObjectId`, then the project snapshot is restored.
- Post-commit Palette/UI synchronization is best-effort and cannot roll back otherwise valid source/project state.

## Physical boolean boundary

Direct Draw **does not automatically call** `QS3DCUTOPENINGS` or `OpeningBooleanService.CutLinkedOpenings(...)`.

The current physical-cut service is intentionally a broader linked-opening operation grouped by host. Calling it implicitly after creating one new Door/Opening could mutate other already-linked pending openings on the same project. Therefore the safe current workflow is:

```text
Direct Draw Door/Opening
-> review Host / dimensions / sill / clearance
-> explicitly run QS3DCUTOPENINGS when physical host mutation is intended
```

Do not bypass this boundary by queuing the global cut command from Direct Draw. A future one-shot cut first needs an explicit-target opening subset API plus proven host-Solid3d rollback semantics.

## Legacy compatibility

These existing commands remain valid and must not be removed:

- `QS3DDOOR`, `QS3DOPENING` — capture pre-existing CAD;
- `QS3DAUTOLINKHOSTS` — selection-scoped automatic host matching;
- `QS3DLINKHOST` — explicit manual host linking;
- `QS3DCUTOPENINGS` / `QS3DCUTOPENINGSCURVED` — guarded physical host cutting;
- `QS3DDOORSCHEDULE` / `QS3DDOORXLSX` — schedule/export.

Direct Draw is the creation path; capture remains the conversion path.

## Runtime qualification required

A local agent with licensed BricsCAD V25 must validate the exact current/release SHA for:

1. Release/x64 compile against exact installed V25 managed assemblies;
2. `NETLOAD` / DemandLoad unique command registration;
3. Ribbon and Domain Hub invocation;
4. Door and WallOpening creation in millimeter and meter drawings;
5. exact picked LINE plan length -> `WidthM` behavior;
6. height, sill/bottom offset and boolean clearance persistence;
7. Model Space success and PaperSpace/Layout fail-closed behavior;
8. valid host matching against ArchitecturalWall, GlassWall, WallPier and StructuralWall where the existing matcher permits;
9. no-host and ambiguous-host rollback with no source/semantic orphan;
10. Floor/Zone/elevation/gap/ambiguity tolerance behavior;
11. World UCS and representative rotated UCS behavior;
12. save/reopen, `QS3DREGEN`, `QS3DHEALTHALL`, schedule and XLSX;
13. explicit `QS3DCUTOPENINGS` after Direct Draw and host fingerprint/rebuild behavior;
14. supported curved/open-POLYLINE host cutting paths;
15. a private copy of owner-provided `MB MONG.dwg` without committing the drawing;
16. Unicode/HiDPI and real runtime screenshots.

GitHub Actions remain manual-only; source/docs implementation does not authorize workflow dispatch.
