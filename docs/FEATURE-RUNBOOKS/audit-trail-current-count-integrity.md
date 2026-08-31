# AuditTrail post-Current Count integrity

Issue: #4871  
Lane-Key: `issue-4871`

## Purpose

`AuditTrail` reads persisted audit history through an `IList<AuditEvent>`. The list is caller/project-owned state, so its admitted Count must remain stable across every caller-controlled enumerator boundary before an event is accepted, validated, cloned, or used to authorize a mutation.

## Defect boundary

The existing traversal already rebound Count before and after `MoveNext`, rejected overrun before `Current`, enforced the 10,000-event ceiling, checked exact observed cardinality, and applied the aggregate text budget plus canonical/XML validation. A remaining gap existed after caller-controlled `Current`: both the public read path and the mutation-validation path incremented the observed count and began processing the returned event before rebinding Count.

A hostile list could therefore change Count from `Current`. The drift would eventually fail at the next traversal edge, but the just-read event had already crossed the acceptance/validation boundary. For an intentionally malformed event, malformed-event validation could win before the cardinality-integrity failure.

## Production contract

For both `Events` and `ValidateExistingHistory(...)`:

1. Bind a supported history Count and enforce its existing size/capacity rules.
2. Rebind Count immediately before and after caller-controlled `MoveNext`.
3. Reject overrun and the hard 10,000-event ceiling before dereferencing `Current`.
4. Read `Current` exactly once for the admitted item.
5. Rebind Count immediately post-`Current`, before incrementing `observed`, null checks, text-budget accounting, canonical/XML validation, cloning, or mutation authorization.
6. Preserve final exact observed-cardinality and Count-stability checks.
7. Preserve atomic behavior: a traversal-integrity failure cannot reach `Clear`, append a new audit event, or publish a partial read snapshot.

The post-`Current` rebound makes Count drift fail before malformed-event validation for the caller-controlled item that induced the drift.

## Deterministic regression

`AuditTrailCurrentCountIntegritySmoke` supplies a hostile `IList<AuditEvent>` whose first `Current` both changes Count from 1 to 2 and returns a malformed event. It proves two independent surfaces:

- the public read path (`Events`) rejects Count drift after one `MoveNext` and one `Current` before malformed-event validation;
- the mutation-validation path (`Clear`) rejects the same Count drift before the backing list's `Clear` can be reached.

The auto-discovered `preflight-audit-trail-current-count-integrity.py` pins the source ordering for both loops and the focused smoke registration without weakening historical AuditTrail guards.

## Runtime boundary

Runtime: `NOT_APPLICABLE` — this is deterministic Core audit/history integrity. Hosted Core smoke and protected CI are valid evidence; no licensed BricsCAD/private-DWG `LOCAL_PASS` is claimed.
