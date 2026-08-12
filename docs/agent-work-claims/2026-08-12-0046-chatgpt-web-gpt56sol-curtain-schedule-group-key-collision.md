# Work claim — Curtain wall schedule collision-free grouping identity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:46:00+07:00`
- Completed: `2026-08-12T00:49:00+07:00`
- Baseline main SHA: `6f08b169e50e51d2a401c7d2a45b354049992a9c`
- Claim commit: `ad4f2f304fc449ba7ce59b5b904675a68d1fdc48`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

`CurtainWallScheduleBuilder` grouped rows with `floorId + "\u001f" + familyId`. Accepted floor/family IDs can contain U+001F internally, so distinct tuples such as `(A<US>B, C)` and `(A, B<US>C)` serialized to the same dictionary key and were incorrectly merged.

## Completed scope

Curtain schedule grouping now uses length-prefixed floor/family tokens. Existing case-insensitive grouping semantics, ordering, quantities and provenance behavior remain unchanged, and no accepted ID characters were banned.

## Product/test commits

- `6c7232a6d1ba3c6ee9674771015ba86cc0d7f5ba` — `fix(reporting): make curtain schedule grouping collision-free`
- `15ae81fc9bd825fbd1d817c9892b010014e897ba` — `test(reporting): cover curtain schedule group key collision`
- `9798088227a699deec52139543ec6edbd4d10cda` — `test(reporting): register curtain schedule group key smoke`

## Validation

- Re-fetched the target blob after claim publication before the product write.
- Product diff only replaces the ambiguous delimiter grouping with a length-prefixed `GroupKey` helper and adds invariant integer formatting support.
- Regression creates two elements with tuple `(A<US>B, C)`, proving identical tuples still group and sum `LengthM`, plus one `(A, B<US>C)` element that formerly collided but now remains independent.
- Registration uses a dedicated module initializer.
- After registration, observed `main` at `b8075871e6ebd406f2ca7e64c42c5bff4aeed6ac`; comparison from `9798088227a699deec52139543ec6edbd4d10cda` reported `status=ahead`, `behind_by=0`, merge base equal to the registration commit. The concurrent change touched an unrelated selection inspector.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25 runtime PASS is claimed from this hosted session.

## Excluded scope

- No curtain geometry/layout/fingerprint/regeneration changes.
- No schedule field/business-rule or XLSX export changes.

## Completion

Distinct accepted curtain schedule grouping tuples no longer alias through delimiter injection on current `main`; claim released as completed.