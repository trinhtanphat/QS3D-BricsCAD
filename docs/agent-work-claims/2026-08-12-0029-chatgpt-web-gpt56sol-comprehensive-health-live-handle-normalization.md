# Work claim — comprehensive health live-handle normalization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-comprehensive-health-live-handle-normalization-20260812-0029`
- Registered: `2026-08-12T00:29:00+07:00`
- Baseline main SHA: `40bc857e4e4668548131a792fb853d787dcee649`
- Priority: P2 — avoid false generated-output missing diagnostics caused solely by caller set comparer/formatting.

## Reserved scope

Normalize the optional live source/generated Handle sets once at the `ComprehensiveModelHealthService.Inspect` boundary before forwarding them to its provider fan-out. The aggregate health result must treat CAD Handle identity case-insensitively and ignore surrounding whitespace regardless of the caller's `ISet<string>` comparer, matching existing canonical Handle semantics elsewhere in Core.

## Reserved surfaces

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ComprehensiveModelHealthSmoke.cs`
- this claim file

## Intended fix

- Snapshot non-null input sets into trimmed, non-empty `HashSet<string>` instances using `StringComparer.OrdinalIgnoreCase`.
- Pass the normalized source set to `ModelHealthService` and normalized generated set to every generated-output provider.
- Preserve null meaning "liveness unavailable" rather than converting null to an empty set.
- Do not alter any individual provider's direct-call API/semantics in this lane.
- Extend the existing aggregate generated-family smoke with an intentionally case-sensitive, padded/lowercase live Handle set and verify no false `*_GENERATED_SOLID_MISSING` issue is produced.

## Explicit exclusions

- Individual generated-health service implementations and their direct-call comparer contracts.
- Generated ownership/build-state/count/fingerprint semantics.
- ModelHealth severity work and BOM release guard work owned by other lanes.
- BricsCAD/native/runtime code, persistence, installer/updater, GitHub Actions.

## Validation boundary

Remote source/diff review plus deterministic committed Core smoke coverage. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.

## Completion condition

The aggregate health fan-out uses normalized Handle sets, focused smoke coverage is committed to current `main`, no neighboring ACTIVE claim is overwritten, and this claim is marked `COMPLETED` with exact integration evidence.
