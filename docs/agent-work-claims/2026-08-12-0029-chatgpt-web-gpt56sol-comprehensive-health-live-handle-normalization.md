# Work claim — comprehensive health live-handle normalization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-comprehensive-health-live-handle-normalization-20260812-0029`
- Registered: `2026-08-12T00:29:00+07:00`
- Baseline main SHA: `40bc857e4e4668548131a792fb853d787dcee649`
- Priority: P2 — avoid false generated-output missing diagnostics caused solely by caller set comparer/formatting.

## Reserved scope

Normalize the optional live source/generated Handle sets once at the `ComprehensiveModelHealthService.Inspect` boundary before forwarding them to its provider fan-out. The aggregate health result treats CAD Handle identity case-insensitively and ignores surrounding whitespace regardless of the caller's `ISet<string>` comparer, matching existing canonical Handle semantics elsewhere in Core.

## Delivered surfaces

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ComprehensiveModelHealthSmoke.cs`
- this claim file

## Delivered behavior

- Non-null input sets are snapshotted into trimmed, non-empty `HashSet<string>` instances using `StringComparer.OrdinalIgnoreCase`.
- The normalized source set is passed to `ModelHealthService`; the normalized generated set is passed to every generated-output provider in the aggregate fan-out.
- Null continues to mean "liveness unavailable" and is not converted to an empty set.
- Individual provider direct-call APIs/semantics are unchanged.
- Existing aggregate generated-family smoke now supplies padded lowercase handles through a case-sensitive set and rejects any false `*_GENERATED_SOLID_MISSING` result.

## Integration evidence

- PR: #576 (`fix(health): normalize aggregate live handle sets`).
- Squash merge SHA: `d235dc6033f217ea7a16d2521968d34c835f0bab`.
- GitHub reported the PR mergeable with exactly two changed product/test files before merge.
- No GitHub Actions workflow was dispatched by this lane.
- No licensed BricsCAD V25 runtime PASS is claimed. This remote browser session validates source/diff and commits deterministic smoke coverage; it does not claim an executed local build/runtime qualification.

## Explicit exclusions preserved

- Individual generated-health service implementations and their direct-call comparer contracts.
- Generated ownership/build-state/count/fingerprint semantics.
- ModelHealth severity work and BOM release guard work owned by other lanes.
- BricsCAD/native/runtime code, persistence, installer/updater, GitHub Actions.

## Completion condition

Satisfied: aggregate health fan-out uses canonical Handle sets, focused regression coverage is on `main`, no neighboring ACTIVE claim was overwritten, and exact integration evidence is recorded here.
