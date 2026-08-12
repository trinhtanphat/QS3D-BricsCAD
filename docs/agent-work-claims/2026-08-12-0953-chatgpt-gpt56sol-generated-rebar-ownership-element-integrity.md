# Work claim — Generated rebar ownership global element identity integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-generated-rebar-ownership-element-integrity`
- Registered: `2026-08-12T09:53:00+07:00`
- Completed: `2026-08-12T09:55:00+07:00`
- Last Updated: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `fdb8394f9cd60767e1c1027070c0ab5990ff5ff3`
- Claim commit: `b59708f299d0708cbaea6a27bbe32958a143e346`
- Priority: P1 — prevent generated rebar ownership diagnostics from false-cleaning a globally ambiguous semantic element identity set
- Task Key: `CORE-GENERATED-REBAR-OWNERSHIP-GLOBAL-ELEMENT-INTEGRITY`

## Confirmed defect

`GeneratedRebarOwnershipHealthService.Inspect(...)` rejected null elements but did not validate case-insensitive uniqueness of semantic element IDs before composing ownership tokens as `element.Id + "/" + key`. Distinct `E1` and `e1` elements claiming the same generated rebar handle in the same slot therefore collapsed to the same token and could false-clean an invalid project.

## Completed scope

- Added a full `project.Elements` preflight before generated rebar ownership aggregation.
- Null, blank, and case-insensitively duplicate semantic element IDs now fail closed before ownership token comparison.
- Existing cross-element/cross-slot generated rebar handle conflict behavior and valid-project results are unchanged.
- Added isolated auto-registered smoke coverage for duplicate-token collapse plus a valid single-owner control.

## Implementation evidence

- Source branch commit: `de12c1a62f31ca1ec07c1e6e61bd4d4fbab3f35c`
- Regression branch commit: `87d49215529ab1a7eeb5d2e97df5d0b04cd37a48`
- Pull request: `#726`
- Squash merge on `main`: `b56d37f01339001a1c062329f77b5579b0de8f1e`
- Main readback source blob: `ec13d728c0d0764befcfd76bd0dcbb054b2a63be`
- Main readback smoke blob: `f410b15ef51c2ceb176a7f39facee0c893fe1720`

## Validation boundary

Exact source/test diff and post-merge readback were verified. GitHub Actions, full build, executable smoke and licensed BricsCAD V25/V26 runtime were not run in this hosted session, so no runtime PASS is claimed.

## Completion

Generated rebar ownership health can no longer false-clean duplicate semantic identities; the claim is released as completed.
