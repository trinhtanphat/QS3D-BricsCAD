# Work claim — Auto Room family sync global identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auto-room-global-identity-integrity-20260812-0918`
- Registered: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `63ac503d6ef73cb4aa88b8c1c2cf1c4628356704`
- Priority: P1 — prevent Auto Room Family synchronization from mutating a project whose semantic identity sets are already ambiguous.

## Confirmed defect

`AutoRoomLifecycle.SyncFamilyDefaults(...)` validates only the supplied Room and target Family through `ProjectState.FindElement/FindFamily`. Those lookup helpers detect duplicate IDs only when the duplicate matches the requested identity. Consequently unrelated duplicate semantic element IDs or unrelated duplicate Family IDs can coexist while synchronization of a unique Room to a unique target Family still reaches `project.Touch()` and rewrites Room properties, FamilyId and snapshot metadata. QSDB persistence and the canonical Family/Floor/Zone mutation services reject those global identity ambiguities.

## Reserved surfaces

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs` — `SyncFamilyDefaults` preflight only plus a private Family identity helper
- `tests/QS3D.Core.SmokeTests/AutoRoomFamilyGlobalIdentityIntegritySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Before target ownership lookup or synchronization planning, require the entire semantic element collection and Family collection to be non-null and case-insensitively ID-unique.
- Reuse the existing `ResolveProjectElements(project)` global element validation; add only the minimal Family identity preflight needed by this file.
- Preserve dangling previous-Family fail-closed behavior, empty-Family bootstrap, canonical same-Family no-op semantics, override/default snapshot behavior, Auto Room topology/stale logic and BricsCAD commands.
- Focused smoke proves unrelated duplicate element IDs and unrelated duplicate Family IDs fail before Room/metadata/project revision mutation, while valid bootstrap synchronization remains functional.

## Coordination

The completed Auto Room dangling-previous-Family claim remains authoritative for unresolved previous Family semantics; this lane does not alter that behavior. Current active Documentation/Recognition/revision-snapshot claims own different files.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
