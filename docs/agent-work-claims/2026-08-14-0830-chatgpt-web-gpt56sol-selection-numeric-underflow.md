# Work claim — Semantic Selection numeric underflow parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-numeric-underflow-20260814-0830`
- Registered: `2026-08-14T08:30:00+07:00`
- Baseline main SHA: `21730dfb89a7c943d0b95ca0609458979781f82e`
- Priority: `P1 Core semantic-integrity hardening` — semantic multi-selection numeric edits must not silently erase non-zero magnitudes through IEEE-754 underflow

## Confirmed source gap

`BulkEditService.MultiplyNumericProperty(...)` already fails closed on both numeric-literal underflow (a syntactically non-zero token that parses to exact zero) and multiplication underflow (finite non-zero operands whose product collapses to exact zero). `SemanticSelectionBulkEditService.MultiplyNumericProperty(...)` performs the equivalent semantic selection edit, but currently checks only parse validity and NaN/Infinity. It can therefore materialize an instance override of `0` from non-zero source magnitude, diverging from the established bulk-edit safety contract and losing semantic data.

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
- Current semantic-selection source at the baseline still lacks those two guards; current focused smoke covers invalid numeric preflight but not underflow.
- Targeted commit search found no existing semantic-selection underflow claim/fix.

## Validation plan

Publish this claim alone to `main`; refresh live `main` and recheck overlapping claims/changes for the two reserved files. Then apply the minimal parity guards and focused regression, reconcile current `main`, push without force, re-fetch exact diff/source, and close this claim `COMPLETED`. Managed/native execution is `NOT_RUN` unless a real executable toolchain is available; source inspection is not reported as runtime PASS.

## Completion condition

Current `main` contains the minimal semantic-selection underflow fix plus focused regression coverage, the pushed source/diff is verified remotely, and this claim is updated to `COMPLETED` with the implementation SHA and truthful validation status.
