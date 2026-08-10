# QS3D Direct Draw Door / Opening — implementation handoff

Updated: 2026-08-10 (UTC+7)

## Status

`QS3DDRAWDOOR` and `QS3DDRAWOPENING` are implemented in source, exposed through the BricsCAD-hosted `TẠO MỚI` Ribbon and Full Domain Hub, and protected by `scripts/preflight-direct-draw-openings.py`.

A targeted physical-cut path is also source-implemented:

- `QS3DCUTSELECTEDOPENINGS` resolves the current CAD/semantic selection to Door/WallOpening elements and passes only those semantic ids to `OpeningBooleanService`;
- the original `QS3DCUTOPENINGS` remains available for the broader all-linked workflow.

This is **source implementation / static contract coverage**, not licensed BricsCAD V25 runtime proof.

## Product flow

Both Direct Draw commands keep BricsCAD as the CAD host and create a real DWG source LINE. The picked two plan-view edge points define the opening width; QS3D does not create a fake semantic-only opening.

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
-> leave the new source selected
-> best-effort UI sync
```

The new source staying selected makes the explicit targeted follow-up natural:

```text
Direct Draw Door/Opening
-> review Host / dimensions / sill / clearance
-> QS3DCUTSELECTEDOPENINGS
-> only the selected semantic Door/WallOpening set is eligible for physical cut
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

## Targeted physical boolean boundary

Direct Draw still **does not automatically call** `QS3DCUTOPENINGS`, `QS3DCUTSELECTEDOPENINGS` or `OpeningBooleanService.CutLinkedOpenings(...)`.

Physical host mutation remains an explicit user action. The difference is that source now has a safe selection-scoped option instead of forcing every explicit cut through the broader all-linked operation.

`OpeningBooleanService` keeps backward compatibility:

```text
CutLinkedOpenings(document, project)
-> all currently linked Door/WallOpening elements
```

and adds the targeted overload:

```text
CutLinkedOpenings(document, project, openingIds)
-> validates every requested id
-> requires each target to be Door/WallOpening with HostWallId
-> groups and cuts only that requested subset
```

The existing host fingerprint/idempotency guard is intentionally preserved. If the same generated host solid was already cut with a **different** opening set or changed geometry/configuration, a targeted retry does not layer an untracked second state onto that solid: it fails closed and requires `QS3DBUILD3D`/host rebuild first. Repeating the same target/fingerprint remains an idempotent no-op.

This means `QS3DCUTSELECTEDOPENINGS` solves the previous “global command may mutate unrelated pending openings” problem without pretending incremental boolean history is fully journaled. It is not yet permission to auto-cut from Direct Draw.

## Legacy compatibility

These existing commands remain valid and must not be removed:

- `QS3DDOOR`, `QS3DOPENING` — capture pre-existing CAD;
- `QS3DAUTOLINKHOSTS` — selection-scoped automatic host matching;
- `QS3DLINKHOST` — explicit manual host linking;
- `QS3DCUTOPENINGS` — guarded broad all-linked physical host cutting;
- `QS3DCUTOPENINGSCURVED` — dedicated guarded curved-host workflow;
- `QS3DDOORSCHEDULE` / `QS3DDOORXLSX` — schedule/export.

Direct Draw is the creation path; capture remains the conversion path. Targeted cut is an explicit physical-mutation option after either path.

## Runtime qualification required

A local agent with licensed BricsCAD V25 must validate the exact current/release SHA for:

1. Release/x64 compile against exact installed V25 managed assemblies;
2. `NETLOAD` / DemandLoad unique command registration, including `QS3DCUTSELECTEDOPENINGS`;
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
13. `QS3DCUTSELECTEDOPENINGS` after Direct Draw with exactly one selected opening and with multiple selected openings;
14. target selection spanning multiple hosts;
15. selection containing unrelated CAD plus a valid opening;
16. same selected target rerun is idempotent;
17. different target/fingerprint on an already-cut generated host fails closed until rebuild;
18. legacy `QS3DCUTOPENINGS` behavior remains unchanged;
19. supported curved/open-POLYLINE host cutting paths;
20. a private copy of owner-provided `MB MONG.dwg` without committing the drawing;
21. Unicode/HiDPI and real runtime screenshots.

GitHub Actions remain manual-only; source/docs implementation does not authorize workflow dispatch.
