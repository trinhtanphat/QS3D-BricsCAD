# DependencyGraph post-Current Count integrity

Issue: #4874  
Lane-Key: `issue-4874`

## Purpose

`DependencyGraph.Rebuild` and `TopologicalDirtyOrder` accept caller-owned `IEnumerable<ProjectElement>` inputs. When an input exposes one or more known Count surfaces, the admitted cardinality must remain stable across every caller-controlled enumerator boundary before a semantic element is processed.

## Defect boundary

The existing implementation already validates known-count source agreement, rebinds Count before and after `MoveNext`, rejects known-count overrun before `Current`, enforces the 10,000-element ceiling, validates exact observed cardinality, and preserves dependency/snapshot stability. A remaining gap existed immediately post-`Current`: the returned element could enter count increment, validation, staging, or materialization before known Count was rebound again.

A hostile collection can therefore change Count from `Current`. Without the post-`Current` rebound, malformed dependency validation or semantic staging may occur before traversal integrity fails at the next loop edge.

## Production contract

For both `Rebuild` and `TopologicalDirtyOrder`:

1. Admit and validate all available known Count surfaces.
2. Rebind known Count immediately before and after caller-controlled `MoveNext`.
3. Reject known-count overrun and the hard element cap before reading `Current`.
4. Read `Current` exactly once for the admitted element.
5. Rebind known Count immediately post-`Current`, before semantic processing: count increment, null/id/dependency validation, staging, materialization, or graph side effects.
6. Preserve final exact observed-cardinality and Count-stability checks.
7. Preserve atomic rebuild behavior: failure cannot replace the previously committed graph.

The new ordering makes Count drift fail before semantic processing of the element whose `Current` access induced that drift.

## Deterministic regression

`DependencyGraphCurrentCountIntegritySmoke` uses a hostile `ICollection<ProjectElement>` whose first `Current` changes Count from 1 to 2 while returning an element with a malformed blank dependency.

- `Rebuild` must report traversal Count drift after one `MoveNext` and one `Current`, before malformed-dependency validation, and must retain a previously committed seed graph.
- `TopologicalDirtyOrder` must report the same Count drift after one `MoveNext` and one `Current`, before dependency validation/materialization.

The auto-discovered `preflight-dependency-graph-current-count-integrity.py` pins the post-`Current` source ordering for both public traversal surfaces and focused smoke registration.

## Runtime boundary

Runtime: `NOT_APPLICABLE` — this is deterministic Core dependency-graph integrity. Hosted Core smoke and protected CI are valid evidence; no licensed BricsCAD/private-DWG `LOCAL_PASS` is claimed.
