# Work claim — Browser workspace empty metadata fail-closed load

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-20260812-browser-workspace-empty-metadata`
- Registered: `2026-08-12T07:20:00+07:00`
- Completed: `2026-08-12T07:40:00+07:00`
- Baseline main SHA: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- Initial claim commit: `c189e537bdafe296d403188c31b5bd60ee1efc9e`
- Coordination commit: `38493fb44cfba32245d74ea0fed1b9cf292eb70a`
- Implementation PR: `#623`
- Main integration commit: `4cea54ffa1cbc5fffe1c1a6f62759beea69aba09`
- Priority: evidence-driven remote-safe persistence hardening found during owner-requested `continue all`

## Confirmed defect

`ProjectBrowserWorkspaceStateStore.Load(ProjectState)` previously returned the default workspace state when the metadata key was either missing **or present with an empty/whitespace value**. That collapsed two distinct persistence states: `Clear(ProjectState)` represents intentional absence by removing the key, while `Deserialize(...)` already defines empty/whitespace serialized workspace state as invalid persisted data.

The old `Load` path therefore silently hid corrupt persisted metadata instead of applying the store's own fail-closed deserialization contract.

## Implemented scope

`Load(ProjectState)` now returns the default state only when `QS3D.ProjectBrowser.WorkspaceState` is genuinely absent. If the key exists, its value proceeds through the existing size check and canonical `Deserialize(...)`/project validation path; present empty/whitespace values consequently throw `InvalidDataException`.

No `Save()` or `Clear()` behavior changed in this lane.

## Regression source

Added isolated CAD-independent Core smoke source:

- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEmptyMetadataSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEmptyMetadataSmokeRegistration.cs`

The regression source covers:

- missing metadata -> canonical default workspace state with no metadata/freshness mutation;
- present empty metadata -> `InvalidDataException` with raw metadata and project freshness unchanged;
- present whitespace metadata -> `InvalidDataException` with raw metadata and project freshness unchanged;
- canonical serialized metadata -> successful round-trip with persisted text, `UpdatedUtc`, and `ChangeVersion` unchanged.

## Coordination / exclusions preserved

A concurrent workspace revision-atomicity lane registered at `3dc86e27db785071930110dbf710fe91554d8603` owned only `Save()/Clear()` revision ordering and explicitly excluded XML/canonicality. This lane was limited to `Load()` presence-vs-corruption semantics. The concurrent lane completed without changing this `Load` contract, and PR `#623` preserved its `Save()/Clear()` surface.

No selection/query/virtualization behavior, Workspace WPF/UI, BricsCAD V25/V26 adapter/runtime, QSDB schema, XLSX/export, release/package or workflow surface changed.

## Validation evidence

- Claim was committed to `main` before implementation at `c189e537bdafe296d403188c31b5bd60ee1efc9e`.
- Coordination boundary was committed at `38493fb44cfba32245d74ea0fed1b9cf292eb70a` before source edits.
- Immediately before merge, current `main` still had source blob `1e25c3fb92761ef21769175a91419e610bfd9904` with the defective `missing || whitespace` `Load` condition, so no concurrent source overlap existed.
- PR `#623` exact diff contained three intended files, `+116/-1`; the production hunk only removed `|| string.IsNullOrWhiteSpace(serialized)` from the `Load` absence condition.
- Server-side squash merge with exact expected head `282883512771123eda4ee33dd48ab3a2cc1e6e9d` produced `4cea54ffa1cbc5fffe1c1a6f62759beea69aba09`.
- Post-merge readback shows source blob `d80437865953ba28394a96602f77a00aebc37819` on `main` with the corrected missing-key-only default path.
- GitHub Actions were not dispatched. The smoke executable/build was not run or claimed in this connector-only environment, and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion

`COMPLETED`: current `main` distinguishes absent Project Browser workspace metadata from present invalid empty/whitespace persisted state and fails closed on the latter while preserving canonical load non-mutation behavior.