# Work claim — Model Health duplicate SourceHandles

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-model-health-source-handle-duplicates-20260813`
- Registered: `2026-08-13T19:24:00+07:00`
- Baseline main SHA: `ef2c860935f08d35569adbae78d4daa2988f851e`
- Priority: P1 diagnostic integrity. `ModelHealthService` currently normalizes `ProjectElement.SourceHandles` and immediately applies case-insensitive `Distinct()`, so duplicate source identities inside one semantic element (including `ABCD` + `abcd`) disappear before health diagnostics. This is inconsistent with existing intra-element `DUPLICATE_DEPENDENCY` diagnostics and with cross-element source-handle ownership collision diagnostics.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file for closeout

## Intended change

Normalize nonblank source handles exactly as today, detect case-insensitive duplicate normalized handles within each element before deduplication, emit one deterministic warning per duplicated handle, then retain the existing distinct list for cross-element ownership and orphan/live-handle checks. Preserve all existing cross-element `DUPLICATE_HANDLE` and `ORPHAN_HANDLE` behavior.

## Excluded scope

- no persistence/deserialization/source resolver changes;
- no generated-handle ownership changes;
- no semantic identity/family/floor/zone/dependency diagnostics changes;
- no UI/report/BricsCAD native work, sibling Platform migration, GitHub Actions or native qualification.

## Validation plan

- refresh `main` and recent claims after claim publication before source mutation;
- focused smoke: same element `ABCD` + `abcd` produces one intra-element duplicate-source warning; unique source handle produces none; two elements sharing a handle continue to produce existing cross-element `DUPLICATE_HANDLE`; a valid live handle prevents false `ORPHAN_HANDLE`;
- re-fetch exact pushed source/test/registration and inspect production diff;
- verify ancestry against moving `main` before closeout;
- report only validation actually executed; no managed/native PASS without local tooling/runtime.

## Coordination

Recent exact commit searches found no current ModelHealth/source-handle duplicate claim. Active/recent signed-zero, Cost, formula and native Solid3d work are disjoint from this diagnostics-only lane.

## Completion condition

Model Health no longer silently erases intra-element duplicate source identities, existing cross-element/orphan semantics remain intact under focused regression source, pushed artifacts/ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
