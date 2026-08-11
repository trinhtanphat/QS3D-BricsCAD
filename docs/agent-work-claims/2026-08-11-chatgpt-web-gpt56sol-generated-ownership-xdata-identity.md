# Work claim — Generated ownership XData identity hardening

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-ownership-xdata-identity`
- Registered: `2026-08-11`
- Baseline main SHA: `8af68ec758825341113d0d70ae3f01e994ff6c2b`
- Priority: make generated native ownership XData identities bounded ASCII tokens while preserving legacy raw-marker compatibility

## Confirmed defect

`ProjectState.ProjectId` and `ProjectElement.Id` accept arbitrary non-empty trimmed strings; they are not constrained to GUID/ASCII identifiers. Generated ownership writers currently place raw `ProjectId` and raw semantic element IDs into `DxfCode.ExtendedDataAsciiString` values. This creates a host-sensitive representation boundary for valid Core identifiers.

The repository already contains a compatibility precedent in `ProjectOwnedNativeTableArtifactService`: write `p1:` + SHA-256(UTF-8(trimmed ProjectId)) and accept either the token or the legacy raw identity when reading ownership. This claim adopts the same strategy for generated native ownership, without changing Core ID semantics or touching the Table/Reporting implementation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/GeneratedOwnershipIdentityToken.cs` (new shared helper; exact name may remain this)
- `src/QS3D.BricsCAD.V25/Cad/GeneratedRebarNativeOwnershipService.cs`
- `src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameNativeOwnershipService.cs`
- `src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainPanelNativeOwnershipService.cs`
- `src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs`
- focused static regression gate under `scripts/`
- this claim file for close-out

## Intended contract

- New generated ownership markers store bounded ASCII SHA-256 identity tokens rather than raw project/element identifiers.
- Project tokens use the existing repository convention `p1:` + lowercase SHA-256 hex of UTF-8(trimmed ID).
- Element tokens use a distinct versioned prefix `e1:` with the same digest construction.
- Readers accept the new token and retain the current raw legacy fallback, preserving existing DWG ownership markers.
- Existing marker RegApp names, ownership version values, purpose/kind semantics, source-handle checks and destructive-operation fail-closed behavior remain unchanged.
- The change does not force Core ProjectId/ProjectElement.Id into GUID or ASCII formats.

## Excluded scope

- Do not modify `ProjectOwnedNativeTableArtifactService.cs`; it is precedent only and may overlap Reporting/Documentation lanes.
- No Core ID validation/schema migration.
- No Floor/Level/vertical-placement changes (`LOCAL-003`).
- No schedule/revision viewer, Quantity, Ribbon/Start Center, Create Similar, Room or Documentation #77 edits in this claim.
- No `docs/LOCAL-AGENT-INBOX.md` edits unless a genuinely runtime-only new blocker is discovered.
- No release, CI dispatch, GitHub Actions, licensed BricsCAD V25 or Windows UI runtime claim.

## Validation plan

- Refresh live `main` and target files after this claim commit before every source write.
- Add a focused `preflight-*.py` source gate that requires token writes, token-or-legacy matching and guards against raw identity writes on the generated ownership slots.
- Read back all committed source/gate files from live `main`.
- Do not claim local Python execution/build/V25 runtime unless actually available in this session.

## Completion condition

All four generated ownership paths write versioned hash tokens for project/element identity, still recognize legacy raw markers, focused static regression coverage is merged, and the claim is marked `COMPLETED` with runtime validation explicitly unclaimed.
