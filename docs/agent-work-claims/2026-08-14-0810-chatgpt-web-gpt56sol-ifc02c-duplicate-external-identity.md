# Agent Work Claim — IFC-02C duplicate external identity ambiguity

- **Agent:** `chatgpt-web-gpt56sol`
- **Date (Asia/Ho_Chi_Minh):** 2026-08-14
- **Status:** `COMPLETED`
- **Completed (Asia/Ho_Chi_Minh):** 2026-08-14 08:12
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02C` — duplicate external identity becomes explicit ambiguity
- **Priority:** P1
- **Baseline main SHA:** `5369b54011bba74d15c739a74c66cc7a482347ff`
- **Dependencies:** completed IFC-02A/IFC-02B and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed contract gap

IFC-01 test-matrix row 2 requires duplicate external identity to be reported as ambiguous. IFC-02B introduced an explicit `InvalidOrAmbiguous` result state, but the prior `IfcRoundTripExchangeResultSet.Create()` threw on duplicate external identities instead of producing a canonical ambiguous result. That left callers responsible for reimplementing the ambiguity transition and could yield inconsistent boundary behavior.

## Implemented scope

- `src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs`
  - `IfcRoundTripExchangeResultSet.Create()` now groups results by canonical external identity.
  - The first unique result is preserved unchanged.
  - Any duplicate identity is collapsed to exactly one `InvalidOrAmbiguous` result with canonical detail `Duplicate external object identity`.
  - Ambiguous duplicate groups retain only the external identity and ambiguity detail; trusted QS3D projection, classification identity, mapping relation identity, and cost relation identity are intentionally discarded rather than selecting one conflicting candidate.
  - Final results retain deterministic canonical ordering.
  - Null collection entries remain fail-closed.
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultSmoke.cs`
  - covers supported-vs-unmapped duplicate candidates;
  - covers forward and reverse duplicate input order;
  - verifies collapse count and canonical ambiguity detail;
  - verifies no trusted projection/classification/mapping/cost evidence leaks from conflicting candidates;
  - verifies a separate unique result is preserved unchanged;
  - preserves existing null-entry failure coverage.

The existing registration file was intentionally unchanged.

## IFC-02 acceptance coverage

- **Row 2:** duplicate external identity is now reported as one explicit ambiguous result instead of generic failure or first-wins behavior.
- **Row 15 / determinism:** reversing duplicate candidate order produces the same canonical ambiguous projection for that external identity and preserves deterministic set ordering.

This remains schema-neutral CAD-independent Core behavior. No IFC schema/runtime/library selection, native import/export, unit/measurement calculation, mapping/cost business rule, persistence, geometry, Rebar, release automation, LOCAL qualification, or BricsCAD source change was made.

## Coordination and publication

- Claim-first commit on `main`: `41d1bbde4b8f5d2cc2eecc62ce2cb36063a17756`.
- Implementation branch: `agent/ifc02c-duplicate-external-identity`.
- Source fix commit: `e768d1db84769692d4beb34426aa073d69886fc5`.
- Focused regression commit / branch head: `5214128c24e5003705bb28577c4e6ba5416cb0cb`.
- Pull request: `#1096` — `fix(ifc): coalesce duplicate external identity as ambiguous`.
- Raw GitHub PR metadata before merge reported `mergeable=true`, `rebaseable=true`, `mergeable_state=clean`, two changed files, 63 additions and 26 deletions.
- Squash merge on `main`: `eeec7895f8bae3dcbbabe85588f4ee697f903f10`.
- Merge used expected head SHA `5214128c24e5003705bb28577c4e6ba5416cb0cb`, so an unexpected branch-head change would have been rejected.
- Concurrent agent commits were preserved through GitHub's clean merge; no force-push or history rewrite was used.

## Validation actually executed

- Refreshed live `main` before claim, after claim, before source work, before PR publication, and after merge.
- GitHub compare before publication showed exactly the two claimed IFC-02C files and no unrelated changes.
- Read raw PR metadata confirming the branch was cleanly mergeable before merge.
- Read back the merged source and focused smoke directly from `main` after merge; blob SHAs matched the implementation branch versions.
- The available execution environment has no `dotnet`, `csc`, or `mcs`, so no managed build/smoke/runtime PASS is claimed.
- No GitHub Actions workflow was dispatched for this lane.

## Completion condition

Satisfied for this bounded Core slice: duplicate external identity is now represented explicitly and deterministically as ambiguity, conflicting trusted evidence is not selected, focused regression covers order-independence and evidence stripping, changes are on remote `main`, and unavailable managed/native execution gates remain explicitly unclaimed.
