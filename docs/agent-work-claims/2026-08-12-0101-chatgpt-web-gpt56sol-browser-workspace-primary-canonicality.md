# Work Claim: Project Browser Workspace Primary Selection Canonicality

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `892651bcf8aaeb452a554b5cde7a64b7f3647b35`
- Scope: fail closed when persisted Project Browser workspace `primaryElementId` would be silently normalized to a different selected-element representation.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspacePrimaryCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspacePrimaryCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0101-chatgpt-web-gpt56sol-browser-workspace-primary-canonicality.md`

## Defect evidence

`ProjectBrowserWorkspaceState.NormalizePrimary(...)` intentionally supplies the first selected element when an in-memory caller omits/whitespace-fills the primary id, and returns the exact selected-element id when a case-insensitive primary match is supplied. `Serialize(...)` therefore always emits that resulting canonical `PrimaryElementId`. The persisted XML reader currently passes raw `primaryElementId` into the constructor without checking whether the constructor changed its representation. With selected elements present, `primaryElementId=""` silently becomes the first selected id; a case-varied primary can likewise be rewritten to the selected-list spelling on the next save.

The recently completed query canonicality lane is closed; the concurrent Project Browser Family/category integrity lane reserves `ProjectBrowserQueryPlanner.cs`, not this workspace-state store.

## Boundaries

- Navigation/Core persistence only; no BricsCAD/native/UI changes.
- Preserve in-memory constructor convenience semantics; harden only persisted XML acceptance.
- Preserve selected-element ordering/case, selection validation, serializer format, query/enum/boolean canonicality and workspace schema/version.
- No GitHub Actions dispatch.

## Validation plan

- Capture persisted `primaryElementId`, construct the state through the existing normalization/validation path, then require ordinal equality between persisted text and `state.PrimaryElementId`.
- Add isolated smoke coverage for canonical primary/empty selection plus blank-with-selection and case-varied primary rejection.
- Review exact PR diff through GitHub connector.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke PASS unless actually available.
