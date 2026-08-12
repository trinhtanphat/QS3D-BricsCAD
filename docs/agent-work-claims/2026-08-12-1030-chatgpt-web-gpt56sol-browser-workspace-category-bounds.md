# Work claim — Project Browser workspace category bounded enumeration

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:30:00+07:00`
- Baseline main SHA: `2722ef061f55f651a32aedbae032284db3d04d25`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect

`ProjectBrowserWorkspaceState` bounds floor ids, zone ids, selected element ids, and expanded paths while materializing its public constructor inputs. `NormalizeCategories(...)` is the exception: it enumerates the supplied `IEnumerable<ElementCategory>` until exhaustion with no bound. A huge or non-terminating sequence of repeated valid categories can therefore hang/resource-exhaust the constructor before the later `ProjectBrowserQueryOptions` 10,000-item guard can participate.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- focused dedicated smoke source under `tests/QS3D.Core.SmokeTests/`
- dedicated smoke registration under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended change

Bound category input enumeration at the same existing Project Browser query filter limit of 10,000 consumed items, before accepting another item. Preserve undefined-enum rejection, sorting/deduplication, Save/Clear semantic-version isolation, XML behavior, and all other workspace semantics.

## Coordination / validation

- The previous exact-path semantic-version claim is `COMPLETED` and merged as `1bac370a427741dd9d37081842b6c89d8d80f17d` before this claim.
- Current source was re-read after that merge and still contains unbounded `NormalizeCategories(...)`.
- No product source has been edited before this claim.
- No GitHub Actions will be dispatched.
- No .NET SDK or BricsCAD runtime PASS will be claimed unless actually executed.