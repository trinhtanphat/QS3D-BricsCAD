# Work claim — Semantic Selection numeric underflow parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-selection-numeric-underflow-20260814-0830`
- Registered: `2026-08-14T08:30:00+07:00`
- Baseline main SHA: `21730dfb89a7c943d0b95ca0609458979781f82e`
- Priority: `P1 Core semantic-integrity hardening` — semantic multi-selection numeric edits must not silently erase non-zero magnitudes through IEEE-754 underflow

## Confirmed source gap

`BulkEditService.MultiplyNumericProperty(...)` already fails closed on both numeric-literal underflow (a syntactically non-zero token that parses to exact zero) and multiplication underflow (finite non-zero operands whose product collapses to exact zero). `SemanticSelectionBulkEditService.MultiplyNumericProperty(...)` performed the equivalent semantic selection edit, but checked only parse validity and NaN/Infinity. It could therefore materialize an instance override of `0` from non-zero source magnitude, diverging from the established bulk-edit safety contract and losing semantic data.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSelectionBulkEditSmoke.cs`
- this claim file

## Acceptance

1. Reject syntactically non-zero numeric property text that parses to exact zero before any selected element is mutated.
2. Reject finite non-zero multiplication that underflows to exact zero when the factor is non-zero.
3. Preserve legitimate zero input and deliberate multiplication by a zero factor.
4. Preserve representable subnormal values and all existing inherited-property materialization semantics.
5. Keep the semantic multi-selection edit atomic: a failing target leaves all selected elements and project change version unchanged.
6. Add focused smoke regression in the already registered `SemanticSelectionBulkEditSmoke` surface.

## Explicit non-scope

No changes to `BulkEditService`, formula evaluation, quantity arithmetic, family assignment, selection inspection, persistence, mapping, cost, IFC, reporting, UI, BricsCAD adapters, release/update tooling or native qualification. No GitHub Actions dispatch.

## Evidence / history

- `e16e5629b456c92b9ec614d25b422404e645a028` established fail-closed numeric-underflow semantics in `BulkEditService`.
- `bb1a84e78ff490cead56bec10731464a8b2e48f2` added the adjacent bulk-edit regression.
- Targeted commit search found no pre-existing semantic-selection underflow claim/fix before registration.

## Completion record

- Claim-only commit: `ea04043354223dd96d35788a455a78b1065f4c01`.
- Production fix: `8e888ddf371aa7bbd8c7d34e1e1ea84dcb7fef66` (`fix(core): reject semantic selection numeric underflow`).
- Focused regression: `ec94279b6da01b601449307b7cac5523da11696f` (`test(core): cover semantic selection numeric underflow`).
- Remote verification: re-fetched current `main` at `ec94279b6da01b601449307b7cac5523da11696f`, confirmed the production blob still contains both fail-closed guards and the focused smoke contains parse-underflow atomicity, multiplication-underflow atomicity, legitimate zero, representable subnormal, and explicit zero-factor coverage.
- Concurrent reconciliation: unrelated Room Finish and other agent commits were allowed to advance `main`; no reserved source/test overlap was observed before regression push.
- Managed .NET smoke execution: `NOT_RUN` — no executable .NET toolchain was available through the connected repository workflow used in this session.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD/native runtime qualification: `NOT_RUN` / not claimed.

## Completion condition

Satisfied: current `main` contains the minimal semantic-selection underflow fix plus focused regression coverage, the pushed source/diff was re-fetched and verified remotely, and this claim is closed `COMPLETED` with exact commit references and truthful validation status.
