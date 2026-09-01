# Semantic untrack predicate content freshness

Lane-Key: `issue-5041`

## Defect boundary

`SemanticUntrackService.Untrack` accepts a caller-controlled predicate over resolved semantic elements. Before this correction, predicate freshness covered explicit project revision changes and structural ownership changes, but did not cover persistent in-place mutation of the content of already-owned `ProjectElement` instances.

Because `SourceHandles`, `DependsOn`, `Properties`, `Quantities`, relation/classification properties and dirty state can change without advancing `ProjectState.ChangeVersion`, a hostile predicate could change semantic content while preserving the same element references. In particular, clearing a dependent element's `DependsOn` relation could make subsequent dependency planning observe the caller-mutated graph and incorrectly permit the target to be untracked while the dependent remained.

## Correctness contract

Before predicate evaluation, semantic untrack snapshots both element ownership and persistent per-element predicate content freshness. After the predicate returns and before the no-op path, dependency planning, or removal, the service requires all of the following to remain unchanged:

- project `ChangeVersion`;
- element count, ids and reference ownership;
- category, family/floor/zone relations and drawing fingerprint;
- dirty flags and update timestamp;
- source-handle sequence and dependency sequence;
- property and quantity dictionaries.

Any persistent content drift fails closed with the dedicated predicate-content freshness error. The existing structural-freshness error remains authoritative for add/remove/replacement ownership changes, and the existing project-state freshness error remains authoritative when the predicate advances `ChangeVersion`.

Predicate caller side effects are not rolled back by this guard. The contract is to prevent semantic untrack from proceeding on state that changed during predicate evaluation; callers remain responsible for their own side effects.

## Deterministic acceptance

The hostile smoke proves that clearing a dependent relation cannot bypass dependency planning. It separately covers source-handle, property, quantity, scalar relation and dirty-state mutation, including a false-predicate/no-op path. Stable controls prove that an unchanged predicate still reaches the normal stable dependency guard and that an independent target can still be untracked successfully.

Run the repository's normal Shared CI. The focused source guard is `scripts/preflight-semantic-untrack-predicate-content-freshness.py`, and the runtime regression is auto-discovered through `SemanticUntrackPredicateContentFreshnessSmoke`.

No licensed BricsCAD runtime or private DWG is required for this Core lifecycle/state contract, and no `LOCAL_PASS` should be inferred from hosted CI.
