# Work claim — document lifecycle start atomicity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-document-lifecycle-start-atomicity`
- Registered: `2026-08-11T22:12:00+07:00`
- Completed: `2026-08-11T22:18:00+07:00`
- Baseline main SHA: `4f4cc84f3248e94cd6b7a9686d8ce490619b7f83`

## Result

Two source-proven startup ownership defects are fixed on `main`.

- `c9b05df72fcc603fcc662fae0d834bae5d6352a2` — `DocumentLifecycleCoordinator.Start()` now wraps manager-event subscription plus active-document persistence/selection attachment in one rollback boundary. Failure removes all four manager handlers best-effort, detaches incomplete persistence ownership, stops selection sync, keeps `_started = false`, and rethrows.
- `4225a6635b9fd17635cf29b5f7097daeca6cf53b` — `SelectionSyncCoordinator.Attach()` now subscribes `ImpliedSelectionChanged` before claiming `Attached` ownership and removes native/bookkeeping state on failure so retry remains possible.
- `cc3203a50f0ab8f90a54ff97e319fdb842eecc80` — the existing auto-discovered document lifecycle preflight now requires success/rollback ordering and rejects add-before-subscribe selection ownership while preserving exact-Document destruction cleanup.

Exact implementation/preflight diffs were inspected. Compare from `cc3203a5...` to later `main` reported `behind_by: 0` with that commit as merge base. A first contents write collided with concurrent `main`; all retries remained non-force and preserved concurrent winners.

## Validation boundary

The static gate is merged but was **not executed in a full local checkout in this connector-only lane**. No GitHub Actions, BricsCAD V25 failure injection, build/NETLOAD, installer, signing or release was run. Native event-subscription failure/retry proof remains local-only; no `LOCAL_PASS` is claimed.

## Coordination

No ProjectContext semantics, modeless UI, updater, Ribbon, Quantity/BQ, Direct Draw, Core or LOCAL inbox files were modified.
