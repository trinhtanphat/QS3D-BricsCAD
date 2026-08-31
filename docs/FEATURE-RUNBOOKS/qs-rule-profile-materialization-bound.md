# QS rule profile materialization integrity

Lane-Key: `issue-5090`

## Scope

This runbook covers the public `QsRuleProfile` constructor boundary that accepts caller-controlled `IEnumerable<QsRuleDefinition>`. Runtime qualification is **NOT_APPLICABLE** because the contract is deterministic Core diagnostics/rule-profile integrity.

## Defect

Historical production called `rules.ToList()` before validating null/duplicate rule semantics. A non-terminating or excessive enumerable could therefore consume unbounded CPU/memory before a profile was published. Known Count channels were also ignored, so over-yield, under-yield, conflicting Count channels, and transient Count drift were not treated as integrity failures.

## Production contract

1. Accept at most 10,000 rules, aligned with the repository's existing bounded rule-collection ceiling.
2. Reject a known Count above the ceiling before obtaining an enumerator.
3. Treat generic, read-only, and non-generic Count channels as integrity evidence; reject negative or conflicting values.
4. Rebound admitted Count around caller-controlled `MoveNext` and `Current` operations.
5. Reject known-count over-yield before reading an unexpected `Current`, and reject under-yield after traversal.
6. Preserve null-rule rejection, case-insensitive duplicate RuleId/HealthIssueCode detection, deterministic rule ordering, detached read-only publication, and resolution semantics.
7. Permit pure streaming inputs up to the hard bound without requiring a Count channel.

## Deterministic regression

`QsRuleProfileSmoke` covers known over-bound rejection before enumeration, pure-streaming first-over-bound rejection before unexpected `Current`, transient Current-time Count drift, plus the existing stable/detached and malformed/duplicate controls. `preflight-qs-rule-profile-materialization-bound.py` pins the explicit traversal ordering and rejects regression to raw `rules.ToList()`.

## Landing

Run the focused feature preflight, aggregate feature guards, Core build and deterministic smoke. The exact merge candidate requires protected `preflight + core`. If protected `main` advances, collision-scan and reconcile this same canonical branch non-force, preserving only the reserved paths, then obtain fresh exact-head protected checks before expected-head merge.
