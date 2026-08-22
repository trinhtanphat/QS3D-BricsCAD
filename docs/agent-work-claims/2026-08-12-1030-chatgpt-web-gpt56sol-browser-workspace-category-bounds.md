# Work claim — Project Browser workspace category bounded enumeration

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:30:00+07:00`
- Completed: `2026-08-12T10:34:00+07:00`
- Baseline main SHA: `2722ef061f55f651a32aedbae032284db3d04d25`
- Claim commit: `a489642424c357d881da086ae3d092e45db1d192`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect fixed

`ProjectBrowserWorkspaceState` bounds floor ids, zone ids, selected element ids, and expanded paths while materializing its public constructor inputs. `NormalizeCategories(...)` was the exception: it enumerated the supplied `IEnumerable<ElementCategory>` until exhaustion with no bound. A huge or non-terminating sequence of repeated valid categories could therefore hang/resource-exhaust the constructor before the later `ProjectBrowserQueryOptions` guard could participate.

`NormalizeCategories(...)` now counts consumed category items and fails closed before accepting item 10,001, using the existing shared `ProjectBrowserQueryPlanner.MaxFilterIds` limit of 10,000. The bound applies to consumed input rather than unique values, so duplicate-heavy or non-terminating sequences cannot bypass it.

## Implemented scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceCategoryBoundsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceCategoryBoundsRegistration.cs`
- this claim file

## Product commits

- `20cec15dad8ef4506b8e96ad65fbf8d8e4644228` — `fix(browser): bound workspace category enumeration`
- `796cc74c10f86e726dad34591b99ad53d6b712cf` — `test(browser): cover workspace category bounds`
- `7c43babfd7063b9d84dd0c097f72af4c8a2dd49f` — `test(browser): register workspace category bounds smoke`

## Regression coverage

Focused smoke verifies:

- normal category inputs remain sorted and deduplicated;
- exactly 10,000 consumed category items remain accepted;
- item 10,001 is rejected before an over-bound sentinel enumerable can continue;
- undefined enum category values remain rejected.

Registration uses a dedicated module initializer and does not edit shared smoke registration.

## Coordination / validation truth

- The previous exact-path semantic-version claim was `COMPLETED` and merged as `1bac370a427741dd9d37081842b6c89d8d80f17d` before this claim began.
- This claim was published and re-read from `main` before product changes.
- The source blob was re-read immediately before the product write and remained `b563d522ec99bbf4d736b1eaca44c992bc9e92e1`.
- Exact source diff was re-read after push and contains only the bounded category enumeration change; Save/Clear/XML semantics are unchanged.
- Exact smoke and registration diffs were re-read after push.
- Comparison from registration commit `7c43babfd7063b9d84dd0c097f72af4c8a2dd49f` to then-current `main` `e7c5e5fbb5b6cccfeff910b0e94a867ed556a177` reported `behind_by: 0` with the registration commit as merge base; all six intervening files were disjoint from this lane.
- No GitHub Actions were dispatched.
- No executable .NET SDK or BricsCAD runtime PASS is claimed from this remote lane.

## Exclusions respected

No workspace Save/Clear semantic-version behavior, XML schema/canonicalization, query planner behavior, selection behavior, ProjectState.Touch semantics, or BricsCAD integration was changed.

## Completion condition

`COMPLETED`: public workspace category enumeration is bounded at the established Project Browser filter limit, existing category semantics remain intact, focused regression coverage is registered on `main`, ancestry is verified, and the claim is released.