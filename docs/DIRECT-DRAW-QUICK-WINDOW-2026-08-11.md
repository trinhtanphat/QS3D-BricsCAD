# QS3D Direct Draw — Quick Window

Updated: 2026-08-11 (UTC+7)

## Goal

Keep the newly-added Window workflow consistent with the rest of Quick Direct Draw: normal authoring should require only authoritative geometry, while one-off dimensions remain available through an explicit advanced command.

QS3D continues to represent a Window through the canonical `WallOpening` semantic category with `OpeningUsage=Window`; this change does not introduce a parallel Window model.

## Quick command

Primary command: `QS3DDRAWWINDOW`

```text
Vẽ Cửa Sổ
-> pick edge 1
-> pick edge 2
-> WidthM from picked geometry
-> use compatible WallOpening Family / Type WindowHeightM / WindowSillHeightM / BooleanClearanceM
-> semantic WallOpening + OpeningUsage=Window
-> Auto Host only the new Window
-> verified HostWallId
```

The primary command does not require Height / Sill / BooleanClearance prompts after the second point.

Use `QS3DDRAWWINDOWADV` when those values must be entered for one Window.

## Host and physical-cut boundary

Quick and advanced Window paths retain the same host behavior:

- the new source LINE remains authoritative for width;
- only the newly-created source is selected before Auto Host;
- a unique `HostWallId` is required;
- no-host/ambiguous-host authoring rolls back the new source and semantic state;
- semantic regeneration still runs before and after host linking.

Those regeneration passes are now **operation-scoped**: the pre-host pass targets only the new Window semantic, Auto Host targets only changed opening(s) plus affected live host(s), and the deterministic post-host pass targets only the new Window + resolved host. A Window authoring action must not regenerate or mark clean unrelated dirty project elements. Auto Host also treats a same-host relation as unchanged only when the stored host property and matching dependency are already canonical; otherwise it repairs the relation through the canonical `HostLinkService` path.

The **physical boolean remains explicit** through the established selected-opening cut workflow. Window creation does not silently cut unrelated hosts/openings.

## UI behavior

`QuickWorkflowRibbonAugmenter` already exposes **Vẽ Cửa Sổ** through `QS3DDRAWWINDOW`, so the existing primary button becomes the quick path automatically. The advanced command stays secondary and does not add another primary Ribbon button.

The common workflow is therefore:

```text
choose compatible WallOpening Family / Type once
-> Vẽ Cửa Sổ
-> two points per Window
-> Auto Host
```

## Safety

The interaction reduction keeps the existing source/native lifecycle:

- Model Space and planar-UCS checks;
- finite/unit-aware two-point width;
- fail-closed Family numeric validation;
- `ProjectStateSnapshot` before mutation;
- canonical `ProjectElement.SetProperty()` writes including `OpeningUsage=Window`;
- active-DWG checks around Auto Host;
- deterministic operation-scoped semantic regeneration;
- exact command-owned source cleanup before project rollback;
- explicit physical-cut boundary;
- best-effort post-commit UI synchronization.

## Runtime qualification boundary

This is source/static-contract work. Exact BricsCAD V25 behavior remains under `LOCAL-008`.

Local qualification should cover:

1. `QS3DDRAWWINDOW`: cancel at either point leaves no project/source/semantic/native residue; accepted geometry proceeds directly with compatible Family values and no numeric prompt sequence;
2. `QS3DDRAWWINDOWADV`: cancel independently at Height, Sill and BooleanClearance prompts and verify no residue;
3. valid-host, no-host and ambiguous-host outcomes, including repair of a deliberately non-canonical same-host relation;
4. malformed Window Family values failing closed before source creation;
5. with an unrelated semantic element already dirty, create one Window and verify only the Window + affected host(s) participate in the authoring regeneration scope while the unrelated element stays dirty;
6. Ribbon **Vẽ Cửa Sổ** still invokes the quick command;
7. Door/Opening schedule/XLSX/Locate distinguishes `OpeningUsage=Window` correctly and explicit selected-opening cut remains compatible;
8. save/reopen, multi-DWG and document-switch safety.

The source-level scope is locked by `scripts/preflight-auto-host-scoped-regeneration.py`; this does not replace exact V25 interaction evidence.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize workflow dispatch.
