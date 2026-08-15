# Work claim — Semantic Schedule load capacity boundary

- Status: `COMPLETED`
- Agent: `Codex /root/audit_performance_gap`
- Registered: `2026-08-15T08:30:00+07:00`
- Completed: `2026-08-15T08:41:00+07:00`
- Baseline main SHA: `bbf44df6f5440566758122866289ea60973e155c`
- Branch: `agent/codex/issue81-schedule-load-bound-20260815`
- Closeout branch: `agent/codex/issue81-schedule-load-bound-closeout-20260815`
- Issue: `#81`
- Authoritative reservation: <https://github.com/trinhtanphat/QS3D-BricsCAD/issues/81#issuecomment-5299827935>

## Defect

`SemanticScheduleCatalog.Load(ProjectState)` validates every persisted `<schedule>` node and then executes `Select(ReadDefinition).ToList()` before `ValidateCatalog(...)` finally enforces the existing `MaxSchedules = 128` capacity. A persisted catalog can therefore drive detailed schema and semantic parsing beyond its declared capacity instead of stopping when the first excess node is observed.

The deterministic counterexample is 128 canonical schedules followed by a malformed 129th schedule. Current source enters the malformed excess node's detailed schema path; the capacity policy should reject that 129th node first.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs`: load-side schedule-node materialization and validation ordering only.
- `tests/QS3D.Core.SmokeTests/SemanticScheduleCatalogSmoke.cs`: extend the already registered catalog smoke for exact-cap acceptance, capacity-first excess rejection, and within-cap schema non-regression.
- `scripts/preflight-semantic-schedule-catalog.py`: pin the bounded load contract and reject the legacy unbounded definition materialization.
- `scripts/preflight-semantic-schedule-catalog-schema.py`: align the existing strict XML gate with the bounded node snapshot while preserving every within-cap schema allowlist assertion. Added after aggregate preflight exposed the superseded literal `ValidateSchema(root);` requirement; reservation amendment: <https://github.com/trinhtanphat/QS3D-BricsCAD/issues/81#issuecomment-5299845645>.
- This claim record.

## Exclusions

`Save`, `Upsert`, `Remove`, `Build`, definition/category/filter/column capacities, XML grammar outside the count-boundary validation order, native BricsCAD/UI/runtime, LOCAL-only automation, release/CI/workflows, private data, active/blocking claims, and open-PR surfaces are excluded. Issue `#81` remains broad and open after this bounded source correction.

## Intended contract and validation

- Reuse `MaxSchedules = 128`; do not introduce a second capacity.
- Accept and parse exactly 128 canonical schedules.
- Stop on the 129th schedule before detailed schema or definition parsing of that excess node.
- Preserve detailed schema rejection for malformed nodes inside the accepted capacity.
- Run Core Release build, full Core smoke, focused Semantic Schedule catalog/save-bound/definition-bound preflights, and aggregate remote-safe preflight.
- Do not operate GitHub Actions.

## Completion evidence

- Claim commits: `b9c61d56b1e29f07e43468bd66ccd0983e02b5a3` and amendment `8d5f98f96dbd74e19532d991d46b6fc60333346a`.
- Implementation commit: `c522472ade3484cf6fb76e8b90b5b40a10204833`; current-main integration candidate: `b09afb68805d9413d5d61a7b5f47104326e0b872`.
- PR: <https://github.com/trinhtanphat/QS3D-BricsCAD/pull/1453>; squash merge: `e9faeedbf251e5a012168cbb2c964d9f74812fa3`.
- Exact merge read-back proves `Load` materializes the bounded node snapshot before schema/definition parsing, and schema validation iterates that snapshot.
- On exact merge `e9faeedbf251e5a012168cbb2c964d9f74812fa3`: Core Release build passed with 0 warnings / 0 errors; full Core smoke reported `ALL PASS`; all four focused Semantic Schedule catalog schema/catalog/save-bound/definition-bound preflights passed; aggregate preflight reported all 808 discovered gates passed.
- No GitHub Actions, BricsCAD/native runtime, release, UI, LOCAL-only automation, or private data was operated. Broad performance issue `#81` intentionally remains open.
