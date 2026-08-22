# Work claim — Regeneration work profile DTO bounded collections

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-profile-dto-cardinality`
- Registered: `2026-08-12T00:09:00+07:00`
- Completed: `2026-08-12T00:12:00+07:00`
- Baseline main SHA: `48591255a4dda245bed178f001e89a415bb20f8a`
- Reservation commit: `21a60734ea9a105f93cb701bd6c61226af599f0e`
- Priority: P1 — public work-profile construction must enforce its declared project cardinality before materializing arbitrary enumerables.

## Defect fixed

`RegenerationWorkProfile` is a public DTO constructor that already receives and validates `projectElementCount`, but previously eagerly called `ToList()` on `targetElementIds`, `items`, and `categories`. All three collections are summaries/scopes of that project and therefore can never validly contain more entries than `projectElementCount`. A caller could provide an excessively large or non-terminating enumerable and bypass the DTO's otherwise strict invariant checks before construction returned.

The constructor now materializes all three project-scoped collections through a shared bounded helper using `projectElementCount` as the exact semantic maximum.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileCollectionBoundSmoke.cs`
- this claim file

## Published commits

- `bd5a432d51dfc73531dc30fcd768beb228ae796f` — bound target IDs, work items, and category summaries at declared project element cardinality while retaining null and ordering semantics.
- `5fca7633d9e15586ad7f7c640bd36408bf571bfe` — add isolated auto-registered sentinel coverage for each collection plus exact-cardinality ordering preservation.

## Delivered contract

- Direct public DTO construction cannot consume any of its project-scoped collections beyond `projectElementCount`.
- Null collection inputs remain rejected with `ArgumentNullException`.
- Exact-cardinality values retain their original ordering and values.
- Existing category/scope/dirty/count validation and profiler algorithms remain unchanged.

## Validation notes

- Exact source/test diffs were fetched after publication and are limited to reserved surfaces.
- Each sentinel enumerable would expose prior over-enumeration; the new helper fails at the first entry beyond declared project cardinality without requesting another item.
- Dedicated smoke auto-registers via `ModuleInitializer`; shared test registry was not edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
