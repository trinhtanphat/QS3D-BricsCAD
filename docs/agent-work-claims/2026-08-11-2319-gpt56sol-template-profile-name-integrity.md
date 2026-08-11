# Work claim — TemplateProfile name integrity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-template-profile-name-integrity-20260811-2319`
- Registered: `2026-08-11T23:19:28+07:00`
- Baseline main SHA: `c607ee3b73ba6091d39c45ad5f69d8c05829c1bd`
- Priority: deterministic CAD-independent invariant defect found during owner-requested `continue all` review

## Reserved scope

Keep `TemplateProfile.Name` canonical after construction. The constructor currently requires/trims a name, but the public setter is an unvalidated auto-property, so callers can later assign blank or padded text and create an in-memory template representation that cannot be reconstructed through the canonical constructor without changing/rejecting its identity-facing data.

## Expected surfaces

- `src/QS3D.Core/Templates/TemplateProfile.cs`
- `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs`
- this claim file for close-out metadata

## Excluded scope

- No native `QS3DTEMPLATE*` command lifecycle, BricsCAD V25 UI/runtime or local qualification.
- No changes to template schema, family/rule merge semantics, recognition mapping, BQ column policy, persistence atomicity or quantity-settings templates.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS claim.

## Defect evidence

`TemplateProfile(string id, string name)` rejects blank values and trims both tokens, but `Name` is currently `public string Name { get; set; }`. Therefore a valid profile can be mutated to `"   "` or `"  Company Standard  "` after construction. `TemplateProfileStore.Save/Apply` validate structural members but do not re-establish the constructor's name canonicalization, and serialization emits `profile.Name` directly. This breaks the object's own required/canonical name invariant and can create state whose round trip either changes representation or is rejected on load.

## Validation plan

- Replace the auto-property with the same required/trimmed invariant used at construction.
- Add deterministic smoke coverage showing padded assignment is canonicalized and blank assignment throws without changing the previous valid name.
- Preserve the existing template save/load/apply round trip in the same smoke surface.
- Re-fetch current `main` and both reserved blobs after this claim becomes visible; use SHA-guarded writes under concurrent movement.

## Coordination

Recent active claims reserve browser/material/interchange/revision/updater/regeneration/native-table and other disjoint surfaces. No current recent claim reserves `TemplateProfile.cs` or this narrow template-name invariant.

## Completion condition

The Core fix and regression are reachable from current `main`, this claim is marked `COMPLETED` with exact SHAs and truthful validation scope, and no hosted/native runtime qualification is claimed.