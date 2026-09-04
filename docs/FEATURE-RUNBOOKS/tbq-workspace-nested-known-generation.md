# TBQ workspace nested known-generation fence

## Scope

This contract covers `TbqProjectWorkspaceState` admission of caller-supplied rate-reference and BQ-library collections when those collections expose a stable known `Count`.

## Required behavior

- A known-Count input keeps its semantic-generation contract at the TBQ workspace boundary even though the final nested model is a `RateReferenceGraph` or `BqLibraryCatalog`.
- The workspace admits one bounded generation, then replays a counted source once and compares complete immutable state before publishing the nested snapshot.
- Rate-reference replay compares source rate code, target kind, and target id exactly.
- BQ-library replay compares item code, description, unit, category path, and reference unit rate exactly.
- Count drift, cardinality mismatch, conflicting/negative Count, and maximum limits keep their existing fail-closed behavior.
- Uncounted inputs remain single-pass at the outer TBQ workspace boundary. The nested graph/catalog can validate the already-materialized immutable snapshot without re-enumerating caller code.
- Null and duplicate validation remain authoritative in `RateReferenceGraph` and `BqLibraryCatalog`; the outer generation fence does not weaken those contracts.

## Regression

`TbqWorkspaceNestedKnownGenerationSmoke` proves that same-count rate-reference drift and BQ-library drift are rejected, honest counted sources are replayed exactly once, and uncounted sources are not spuriously double-enumerated.

`preflight-tbq-workspace-nested-known-generation.py` is auto-discovered by shared feature-source validation and prevents reintroduction of the old direct `Bounded(...)` handoff that erased caller known-Count identity.

## Runtime boundary

This is deterministic managed-Core cost/state correctness. No licensed BricsCAD runtime evidence is required or claimed.
