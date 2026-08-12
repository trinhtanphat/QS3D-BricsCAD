# Work claim — ProjectBrowserQueryOptions bounded input enumeration

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-browser-query-option-bounds`
- Registered: `2026-08-11T23:51:00+07:00`
- Released: `2026-08-11T23:54:00+07:00`
- Baseline main SHA: `9fd71f120b6460c42587a675194497a88d970d91`
- Reservation commit: `4ec0e38a9bc0a331302a7fde6966da86d2773d9f`
- Priority: P1 — public query-option construction must not bypass the browser's declared filter cardinality guards.

## Confirmed defect

`ProjectBrowserQueryOptions` currently materializes `categories`, `floorIds`, and `zoneIds` with `new List<T>(IEnumerable<T>)`. The later `ProjectBrowserQueryPlanner.Build(...)` path limits floor/zone filters to 10,000 and validates categories, but those limits run only after the options object already exists. A very large or non-terminating enumerable can therefore consume unbounded time/memory inside the public options constructor and never reach the planner guards.

## Release reason

A concurrent agent published later claim commit `8fd300f26707ab1a08c838e099764f662f37ee5d` for the same defect and exact source surface, with baseline already including this reservation commit. The attempted source write was rejected by the repository concurrency guard with HTTP 409 before any product change from this lane was published. To avoid duplicate ownership and overwrite risk, this claim is released and the other active lane is left authoritative.

## Former reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryOptionsBoundSmoke.cs`
- this claim file

## Coordination notes

- No source/test changes from this claim were published.
- No force-push or conflict overwrite was attempted.
- GitHub Actions were not dispatched.

## Completion condition

Superseded by concurrent active claim `8fd300f26707ab1a08c838e099764f662f37ee5d`; no further work will be performed in this lane.
