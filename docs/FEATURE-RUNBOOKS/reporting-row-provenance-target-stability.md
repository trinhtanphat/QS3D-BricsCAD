# Reporting row provenance target stability

## Scope

`ReportingRowProvenance.AppendSourceHandles` owns atomic publication of validated source handles into a caller-owned target list. This contract extends the historical hostile-source traversal guarantees by binding a semantic target snapshot before traversal and requiring that target state to remain unchanged while caller-controlled source code executes.

Runtime host acceptance is not applicable. This is deterministic Core reporting/provenance integrity.

## Required behavior

Before reading the source, capture the target snapshot by count, order, and exact stored value. Derive existing normalized handle identities from that snapshot rather than from a later live target read.

Revalidate the target immediately before and after every caller-controlled `MoveNext`, and immediately after every caller-controlled `Current`. Revalidate it again after traversal and immediately before staged publication. Append/remove/replace/reorder mutations performed by source callbacks must fail closed with zero C03-owned publication; the routine does not attempt to roll back mutations performed by the hostile caller itself.

The existing source-side contracts remain mandatory: stable supported known-Count evidence, conflicting/negative Count rejection, known-Count overrun before extra `Current`, exact under-yield rejection, the 10,000-entry streaming cap, canonical handle identity, duplicate rejection, staging/atomicity, and pure streaming acceptance.

## Deterministic regression coverage

`ReportingRowProvenanceTargetStabilitySmoke` covers source `MoveNext` callbacks that append and remove target entries, plus `Current` callbacks that replace and reorder target values. MoveNext-induced mutations are required to fail before a `Current` read. Current-induced mutations are required to fail before staging/publication can escape. Stable counted and pure streaming controls must continue to publish atomically.

The smoke deliberately checks the hostile callback's resulting target state rather than pretending the callee can undo caller-owned mutation. "zero C03-owned publication" means no staged SourceHandles are appended after target-state drift is detected.

## Source guard

`scripts/preflight-reporting-row-provenance-target-stability.py` pins target snapshot admission and the traversal ordering around `MoveNext`/`Current`, plus the final pre-publication rebound. It also pins the focused smoke and historical known-Count / 10,000 / pure streaming compatibility language.

Any future change that intentionally alters these semantics must update production code, deterministic smoke, the dedicated preflight, and this runbook together rather than weakening the guard.
