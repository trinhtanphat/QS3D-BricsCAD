# Work Claim: Project Browser Workspace Primary Selection Canonicality

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `892651bcf8aaeb452a554b5cde7a64b7f3647b35`
- Scope: fail closed when persisted Project Browser workspace `primaryElementId` would be silently normalized to a different selected-element representation.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspacePrimaryCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspacePrimaryCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0101-chatgpt-web-gpt56sol-browser-workspace-primary-canonicality.md`

## Completed work

- `Deserialize(...)` now captures persisted `primaryElementId`, constructs the state through the existing normalization/validation path, and requires exact ordinal equality between persisted text and the resulting `state.PrimaryElementId`.
- A blank persisted primary while selected elements exist now fails closed instead of silently becoming the first selected id.
- A case-varied primary that only case-insensitively matches a selected id also fails closed instead of silently changing to the selected-list spelling on re-serialize/save.
- Canonical primary values and canonical empty-primary/empty-selection state remain accepted.
- In-memory constructor convenience semantics remain unchanged; only persisted XML acceptance was hardened.
- Existing query/enum/boolean canonicality, selected-element normalization, serializer format and schema/version remain unchanged.

## Published commits / PR

- Claim-first commit: `b7274c4b4ecf078e735c19d684dd4bb179667693`.
- Source commit: `d0828255135a666f29a57e74fa2db3f100ce302d`.
- Focused smoke: `4aadb9770f047334af9dc2fddb8f60e4e60fa46c`.
- Smoke registration: `96b28c8a6cc421951e90ad6dd005cec812fe9508`.
- PR #596 contained exactly the three reserved source/test files and was squash-merged.
- Published `main` squash SHA: `e79d82536fe8fefa2ed9a31847027e4142746762`.

## Validation notes

- Reviewed PR #596's exact three-file patch before merge.
- The first merge attempt correctly failed because `main` moved; current `main` and the reserved source blob were re-read, confirmed unchanged, and the merge was retried without force-updating the branch.
- GitHub Actions were not dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test PASS.

## Blocked dependencies

None.
