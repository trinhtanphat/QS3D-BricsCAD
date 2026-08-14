# Work claim — Drawing fingerprint public-mutation canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T20:39:11+07:00`
- Baseline main SHA: `ce29bc89113961a4cd3874f5b5352ca50af5e260`
- Implementation branch: `agent/chatgpt-web-gpt56sol/drawing-fingerprint-mutation-canonicality-20260814`
- Implementation commit: `d0e1531e9db15d55c1f14501b63c011ebc0da12f`
- Integration batch: `integration/chatgpt-web-gpt56sol-drawing-fingerprint-mutation-canonicality-20260814`
- Priority: Core P1 persistence integrity; public domain setters can currently admit a non-canonical drawing fingerprint that the canonical QSDB schema rejects on publication/readback.

## Reserved scope

Close the public-mutation gap for project/element `DrawingFingerprint` only. Public assignments must normalize optional fingerprint identity consistently with the existing QSDB canonical attribute contract, reject control-character values before mutation, and preserve the existing valid value/revision state on rejection. Keep canonical/empty assignments round-trippable.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectState.cs` — `ProjectState.DrawingFingerprint` canonical optional identity mutation; preserve exact-once project persistence version semantics after normalization.
- `src/QS3D.Core/Domain/ProjectElement.cs` — `ProjectElement.DrawingFingerprint` canonical optional identity mutation.
- `tests/QS3D.Core.SmokeTests/QsdbDrawingFingerprintCanonicalitySmoke.cs` — focused public-mutation and QSDB round-trip regressions.

## Excluded scope

- Do not change `src/QS3D.Core/Persistence/QsdbProjectStore.cs` or XML schema validation semantics; the current canonical QSDB validator/load contract is the boundary this lane aligns public mutation with.
- Do not change drawing path semantics.
- Do not change Source Reconcile, cleanup authorization, command-plan freshness, `DESYNCHRONIZED` guards, or issue #1005 behavior.
- Do not change BricsCAD adapter/native drawing identity capture or claim native V25/V26 runtime evidence.
- Do not enter the active `slabOpen`/host-health claim or its diagnostics/BricsCAD surfaces.

## Validation plan

- Add deterministic smoke coverage proving padded project/element fingerprints normalize to the canonical value and round-trip through QSDB.
- Prove project canonical no-op assignment does not increment `ChangeVersion` after normalization.
- Prove control-character assignment rejects before mutation; for project state, rejection must leave both the previous fingerprint and `ChangeVersion` unchanged; for element state, rejection must leave the previous fingerprint unchanged.
- Re-read source/test on the implementation branch and inspect the final diff against the refreshed integration baseline.
- Use the repository's standing automatic post-integration V25 cloud workflow only if/when this source lane is landed to `main`; do not manually dispatch Actions.

## Implementation status

The dedicated implementation branch contains one atomic source/test commit, `d0e1531e9db15d55c1f14501b63c011ebc0da12f`. Its diff is limited to the three reserved files. Source/test blobs were re-read after creation; the control-character fixture uses the C# `\u0001` runtime escape, and no excluded persistence/Source Reconcile/native surfaces were changed. The claim remains `ACTIVE` until integration and current-main reachability are verified.

## Coordination

The historical QSDB drawing-fingerprint canonicality claim completed on 2026-08-12 and added XML validator/load coverage for padded attributes. This lane is deliberately narrower/different: it aligns the still-public in-memory mutation APIs with that already-canonical persistence contract so callers cannot construct self-invalidating persisted state. Current recent-history/claim audit found no active reservation on these exact project/element drawing-fingerprint setter surfaces. The active `slabOpen` claim is non-overlapping and owns separate Direct Draw/host-health surfaces.

## Completion condition

Current `main` contains the public project/element drawing-fingerprint canonicalization and focused regressions, the implementation is represented in the dedicated integration result and final main landing, remote-safe validation evidence is recorded accurately, and this claim is then closed `COMPLETED` with exact implementation/integration/main SHAs. Native BricsCAD runtime evidence remains separate.
