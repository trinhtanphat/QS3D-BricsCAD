# Work claim — TemplateProfile name integrity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-template-profile-name-integrity-20260811-2319`
- Registered: `2026-08-11T23:19:28+07:00`
- Completed: `2026-08-11T23:22:03+07:00`
- Baseline main SHA: `c607ee3b73ba6091d39c45ad5f69d8c05829c1bd`
- Claim commit: `3ff3b88ee828dfa3ece0716c8619a347fbecc190`
- Implementation commit: `6cf4c78fe55e1ec9b792a019adc2449dfef50ae8`
- Regression-test commit: `5a85de7b43922eb250b35c84fd33d3159e3adf2c`
- Priority: deterministic CAD-independent invariant defect found during owner-requested `continue all` review

## Reserved scope

Keep `TemplateProfile.Name` canonical after construction. The constructor required/trims a name, but the former public auto-property setter allowed a valid profile to become blank or padded after construction.

## Implemented

- `TemplateProfile` now stores its name through a guarded backing field.
- Constructor and later setter assignments use one `RequireName` invariant: blank values throw and valid values are trimmed.
- Rejected mutation leaves the previous valid name intact.
- Existing template save/load/apply behavior is preserved.

## Changed surfaces

- `src/QS3D.Core/Templates/TemplateProfile.cs`
- `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs`
- this claim file

## Excluded scope

- No native `QS3DTEMPLATE*` command lifecycle, BricsCAD V25 UI/runtime or local qualification.
- No changes to template schema, family/rule merge semantics, recognition mapping, BQ column policy, persistence atomicity or quantity-settings templates.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS claim.

## Defect evidence

Before this fix, `TemplateProfile(string id, string name)` rejected blank values and trimmed the initial name while `Name` was `public string Name { get; set; }`. A caller could therefore mutate a valid profile to `"   "` or padded text after construction; serialization emitted that raw value directly even though load reconstructed through the stricter constructor.

## Validation performed

- Published claim `3ff3b88ee828dfa3ece0716c8619a347fbecc190` before product/test writes and verified it remained in current-main ancestry with `behind_by=0`.
- Re-fetched both reserved blobs from current `main` after claim publication.
- Source fix committed as `6cf4c78fe55e1ec9b792a019adc2449dfef50ae8`; focused regression committed as `5a85de7b43922eb250b35c84fd33d3159e3adf2c` using exact blob SHAs.
- Regression source verifies padded assignment canonicalizes to `Company Standard`, blank mutation throws without changing the valid name, loaded name remains canonical, and the existing template save/load/apply path continues.
- Compared the claim to then-current `main` `23dba63c5c5b0da5ad04750735cb2d03613687e3`: status `ahead`, `ahead_by=13`, `behind_by=0`; both source/test changes remained reachable despite concurrent disjoint commits.
- No GitHub Actions workflow was dispatched or re-run. No hosted smoke execution or BricsCAD V25 runtime qualification is claimed.

## Outcome

Template profile names now preserve the constructor's required/canonical invariant across later mutation and serialization round trips.