# Work claim — Preview Review change-domain integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:46:00+07:00`
- Completed: `2026-08-12T10:54:00+07:00`
- Baseline main SHA observed: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`
- Claim commit: `3a4ef1312cf99a0d3c4af08a7bd176d5aa440190`
- Source fix: `c7891ab936e13441e685f354ac6ef29d3a55224c`
- Regression smoke: `ceeea6782db3d191c3afe13e6144cdaf96fbc7fa`
- Priority: P1 persisted Preview Review artifact integrity
- Task Key: `CORE-PREVIEW-REVIEW-CHANGE-DOMAIN-INTEGRITY`

## Confirmed defect

`PreviewReviewSnapshotService` and `PreviewReviewSnapshotStore.Load(...)` required every entry `change` value to be nonblank/canonical text, but did not require it to belong to the producer-owned change vocabulary. Quantity-rule previews emit only `Added`, `Changed`, or `Removed`, and regeneration reviews are sourced from `RevisionService`, which emits the same three values. A persisted artifact could therefore carry a self-consistent but unsupported token such as `Renamed`; query facets and snapshot comparison would treat that invalid state as ordinary review data instead of failing closed.

Recent Preview Review field/category canonicality fixes were complete and did not cover the `change` value domain. No overlapping change-domain claim existed before reservation.

## Implemented contract

- `PreviewReviewSnapshotService.IsCanonicalReviewChange(...)` accepts only exact ordinal `Added`, `Changed`, or `Removed` values.
- In-memory snapshot invariant verification rejects unsupported change values before fingerprint acceptance/query/comparison use.
- Persisted XML load rejects unsupported change values immediately after canonical attribute parsing.
- Unknown tokens and case aliases fail closed; padded values continue to fail the existing canonical attribute check.
- Category/field portability checks, fingerprint format/computation, query/filter semantics, comparison ordering, snapshot schema/version and producer behavior are unchanged.

## Regression coverage

`PreviewReviewChangeDomainIntegritySmoke` is auto-registered and uses real Quantity Rule preview producers plus the real Preview Review store:

- producer-generated `Added` round-trips through Save/Load;
- producer-generated `Changed` round-trips through Save/Load;
- stale managed output generates `Removed` and round-trips through Save/Load;
- persisted `Renamed` and lowercase `added` are rejected as unsupported;
- padded ` Added ` is rejected by the canonical change boundary.

## Validation

- Exact source commit readback shows only 10 additions / 1 deletion in `PreviewReviewSnapshot.cs`: one shared helper plus the Verify and Load checks.
- Exact regression commit readback shows one new focused 154-line smoke source.
- Compared source fix `c7891ab936e13441e685f354ac6ef29d3a55224c` to observed current `main` `9c6164ff89456280f6a17ea4a831849f1e14e1c5`: `ahead_by=28`, `behind_by=0`, with the source fix as merge base; no later commit in that range modified `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`.
- No GitHub Actions were dispatched. The smoke source was committed/read back but not executed from this connector-only session. No executable .NET/full build PASS and no licensed BricsCAD V25/V26 runtime PASS are claimed.

## Completion

`COMPLETED`: Preview Review artifacts now fail closed when an entry carries a change value outside the exact producer-owned `Added` / `Changed` / `Removed` domain.
