# Work claim — Generated rebar ownership global element identity integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-generated-rebar-ownership-element-integrity`
- Registered: `2026-08-12T09:53:00+07:00`
- Last Updated: `2026-08-12T09:53:00+07:00`
- Baseline main SHA: `fdb8394f9cd60767e1c1027070c0ab5990ff5ff3`
- Priority: P1 — prevent generated rebar ownership diagnostics from false-cleaning a globally ambiguous semantic element identity set
- Task Key: `CORE-GENERATED-REBAR-OWNERSHIP-GLOBAL-ELEMENT-INTEGRITY`

## Confirmed defect

`GeneratedRebarOwnershipHealthService.Inspect(...)` rejects null elements but does not validate case-insensitive uniqueness of semantic element IDs before composing ownership tokens as `element.Id + "/" + key`. Two distinct elements with IDs such as `E1` and `e1` that claim the same generated rebar handle in the same slot produce the same token, so the service can return no ownership conflict even though the project identity set is globally invalid. Other Core ownership/persistence/graph boundaries fail closed on duplicate semantic element identity, and `ComprehensiveModelHealthService` already converts diagnostic data failures into an error issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedRebarOwnershipElementIntegritySmoke.cs`
- this claim file

## Intended contract

- Preflight `project.Elements` before generated rebar ownership aggregation.
- Reject null, blank, or case-insensitively duplicate semantic element IDs before token comparison.
- Preserve existing cross-element/cross-slot handle conflict semantics and valid-project results.
- Do not change generated handle parsing, rebar generation, Comprehensive health aggregation or persistence behavior.

## Validation plan

Add an auto-registered Core smoke proving an unrelated/colliding `E1` + `e1` project fails closed even when both claims would otherwise collapse to the same ownership token, plus a valid control proving one canonical owner remains clean. Re-read source before branch write and review exact PR diff before merge.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Generated rebar ownership health can no longer false-clean duplicate semantic identities, regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
