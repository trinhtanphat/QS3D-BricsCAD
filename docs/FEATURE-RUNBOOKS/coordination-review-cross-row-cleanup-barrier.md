# Coordination review cross-row cleanup barrier

Lane: `issue-4630`

## Defect boundary

A Coordination Manager row change is a presentation-ownership boundary. Before this fix, `OnSelectionChanged` called a best-effort reset that retained failed native cleanup ownership internally but discarded the failure and immediately enabled the newly selected row. A later review action could therefore run while highlight, isolation-mode, or section-view debt from the previous row was still owned.

## Source contract

- Row change attempts all transient cleanup before the new row can mutate CAD presentation state.
- Any cleanup failure or remaining ownership raises a controller-local cleanup barrier.
- While the barrier is raised, Highlight, Isolate, and Section/Focus are disabled.
- Clear Highlight, Restore Isolation, and Restore View remain available solely according to their corresponding owned cleanup state and do not require provenance resolution of the newly selected row.
- Successful cleanup retries recompute the barrier from `HasTransientState`; only zero owned state releases new mutations.
- A destroyed document remains an explicit abandon boundary and clears controller/session ownership without claiming native cleanup.
- Dispose continues to retry cleanup and does not publish terminal disposal after cleanup failure.

## Repository-safe validation

Run:

```text
python scripts/preflight-coordination-review-cross-row-cleanup-barrier.py
```

Then consume the normal exact-head Shared CI for the canonical branch. Hosted CI/source inspection validates ownership/control-flow contracts only. It does not prove licensed BricsCAD native behavior and must not be reported as `LOCAL_PASS`.

## Licensed qualification tail

When a local licensed campaign covers Coordination Manager review state, include a failure-injection or reproducible native-cleanup-failure case if the host/harness safely supports it: change rows with owned presentation state, verify no new row mutation can run until matching cleanup succeeds, then verify zero residual highlight/isolation/view state. Bind any result to the exact tested SHA and host identity.