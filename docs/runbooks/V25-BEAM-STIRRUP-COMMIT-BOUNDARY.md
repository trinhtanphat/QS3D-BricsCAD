# V25 Beam Stirrup commit-boundary fail-safe

## Scope
C03 / issue-7262. `QS3DREBARSTIRRUP3D` must not invite a retry after control has crossed into the native mutation builder when the final CAD outcome cannot be proven.

## Remote-safe contract
- Capture and fence the active `Document` plus native database generation before mutation.
- Revalidate project id/change version and the semantic beam target set before entering the builder.
- Any exception escaping `BeamStirrupSolidBuilder.BuildSelected` is reported as **outcome indeterminate** with explicit **do not retry** guidance; exception details are not exposed to UI.
- A builder post-commit cleanup warning is also conservatively published as outcome-indeterminate at the command boundary. This avoids false success while the builder's legacy generic post-commit cleanup classification remains broader than the desired final contract.
- Admission failures before entering the builder remain normal retryable operation failures.
- Builder pre-commit rollback, aggregate rollback-failure behavior, transaction ownership and generation-bound post-commit UI refresh remain unchanged.

Run:

```text
python scripts/preflight-v25-beam-stirrup-commit-boundary.py
```

Fresh exact-head Shared/Hybrid CI and admitted-reference V25 compile are required before merge.

## LOCAL_ONLY
Licensed BricsCAD V25 is required to qualify native transaction/disposal timing, MDI switching during command execution, visible palette/editor publication and real geometry replacement. Remote/static green is not `LOCAL_PASS`.
