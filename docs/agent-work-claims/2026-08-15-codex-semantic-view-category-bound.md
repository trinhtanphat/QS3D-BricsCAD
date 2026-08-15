# Work claim — Semantic View category bounded snapshot

- Status: `ACTIVE`
- Agent: `codex-audit-docs-gap-next-20260815` (`/root/audit_docs_gap_next`)
- Registered: `2026-08-15T09:30:00+07:00`
- Baseline main SHA: `de7aba1295abbc113cd548a6f86b8c6462172b2a`
- Issue: `#77`
- Branch: `agent/audit-docs-gap-next-20260815`
- Priority: evidence-driven remote-safe Core documentation hardening

## Confirmed defect

`SemanticViewDefinition` snapshots `includeElementIds` and `excludeElementIds` through a bounded helper, but snapshots `categories` with an unbounded `List<T>(IEnumerable<T>)` construction. A very large or non-terminating category enumerable can therefore consume unbounded time or memory inside the public definition constructor before `SemanticViewPlanner.NormalizeCategories(...)` can reject duplicate or undefined values.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`: bound only `SemanticViewDefinition` category enumeration at the planner's existing collection limit while preserving later category validation.
- `tests/QS3D.Core.SmokeTests/SemanticViewDefinitionBoundedSnapshotSmoke.cs`: add deterministic over-bound sentinel and accepted defensive-snapshot coverage.
- one focused static gate under `scripts/` for this exact constructor contract.
- this claim file and issue/PR evidence.

## Exclusions

Semantic Schedule categories, catalog load/save/freshness, Semantic Tags, native BricsCAD adapters/UI/runtime, `docs/LOCAL-AGENT-INBOX.md`, workflows, GitHub Actions, private data and every open/active neighboring claim are excluded. Issue `#77` remains open for its broad native documentation scope.

## Validation plan

- focused static gate;
- `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds;
- full deterministic Core smoke;
- focused documentation/view/sheet/schedule/tag gate set;
- aggregate `scripts/preflight.py`.
