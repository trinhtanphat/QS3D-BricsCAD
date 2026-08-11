# Work claim — Project Browser query option bounded enumeration

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:50:00+07:00`
- Baseline main SHA: `4ec0e38a9bc0a331302a7fde6966da86d2773d9f`
- Claim commit: `8fd300f26707ab1a08c838e099764f662f37ee5d`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect fixed

`ProjectBrowserQueryPlanner` declares a 10,000-ID filter bound, but `ProjectBrowserQueryOptions` previously materialized caller-provided category, floor-ID and zone-ID `IEnumerable` inputs with unbounded `List<T>` construction before the planner could enforce any cardinality guard. An excessive or non-terminating source could therefore consume unbounded time/memory while constructing the public options object.

The options constructor now copies each optional enumerable through a bounded helper using the planner's existing 10,000 filter limit. Normal finite inputs and null/empty inputs retain their prior semantics; existing planner category, duplicate-ID and reference validation remain unchanged.

## Implementation surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryOptionBoundsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryOptionBoundsRegistration.cs`
- this claim file

## Product commits

- `ed6f268312a46d4639bcb7c6b630ad479d16903c` — `fix(browser): bound query option enumeration`
- `539c0f4ab7f7b915f4ddd32773959a9f09799822` — `test(browser): cover query option bounds`
- `d4aa2bef0ef3d3433a6fe64dfc220105f01cfc41` — `test(browser): register query option bounds smoke`

## Regression coverage

Focused smoke source verifies:

- null/default option collections remain empty;
- normal finite category/floor/zone options are preserved;
- category, floor and zone enumerables that cross the 10,000-item bound fail with the browser guard before a sentinel exception placed beyond the supported enumeration boundary can be reached.

Registration uses a dedicated module initializer and does not edit shared smoke registration.

## Coordination / validation truth

- The claim was published and re-read from `main` before product changes.
- A second agent independently reserved the same defect after this claim; its claim `2026-08-11-2351-chatgpt-web-gpt56sol-browser-query-option-bounds.md` is now `RELEASED` and explicitly names this lane as authoritative. It published no product/test changes.
- Exact implementation diff was re-read after push: only bounded option copying plus exposure of the existing 10,000 constant to the adjacent options class changed; query/filter algorithms were not changed.
- Exact smoke and registration diffs were re-read after push.
- Comparison from implementation commit `ed6f268312a46d4639bcb7c6b630ad479d16903c` to registration commit `d4aa2bef0ef3d3433a6fe64dfc220105f01cfc41` reported `behind_by: 0` with the implementation commit as merge base.
- Comparison from registration commit `d4aa2bef0ef3d3433a6fe64dfc220105f01cfc41` to observed `main` `b81e277b75d714c6d4805d14623cfeb26a674cfe` also reported `behind_by: 0`; the intervening commit was a disjoint claim file.
- Hosted environment has no .NET SDK, so the smoke suite was not executed in this session.
- No GitHub Actions were dispatched and no BricsCAD V25 runtime/build PASS is claimed.

## Exclusions respected

No Project Browser selection, virtualization, workspace-state/UI, grouping semantics, project/domain identity, native adapter or BricsCAD runtime files were changed.

## Completion condition

Satisfied for remote/source scope: query-option enumeration is bounded before materialization, normal filter behavior is preserved, focused regression source is registered on `main`, concurrent work was preserved, duplicate ownership was reconciled, and this claim is released as `COMPLETED`.