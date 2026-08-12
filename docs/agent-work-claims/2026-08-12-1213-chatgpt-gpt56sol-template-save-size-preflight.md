# Work claim — Template save size preflight

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-template-save-size-preflight`
- Registered: `2026-08-12T12:13:00+07:00`
- Last Updated: `2026-08-12T12:13:00+07:00`
- Baseline main SHA: `ef75c65b742726b3c4ff98a660084cf3ed48bccc`
- Priority: evidence-driven persisted-template side-effect defect found during owner-requested `continue all`
- Task Key: `TEMPLATE-SAVE-SIZE-PREFLIGHT`

## Confirmed defect

`TemplateProfileStore` has an explicit 8 MiB persisted-file limit. `LoadDocument(...)` rejects files above that limit before XML parsing, but `Save(...)` currently validates the in-memory profile, creates the destination directory, creates/writes a sibling temp file, and only then calls `Load(temp)` to discover that the serialized template exceeds 8 MiB. An invalid oversized profile can therefore mutate the filesystem before the save is rejected.

## Reserved scope

Serialize and enforce the existing 8 MiB file-size contract before `Directory.CreateDirectory(...)`, temp-path creation, or any destination/temp filesystem write. Preserve current XML serialization semantics, defensive `Load(temp)` validation, backup/atomic replace behavior, and all template profile validation/canonicality rules.

## Expected surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- focused registered Core smoke proving oversized save has no filesystem side effect
- this claim file

## Explicit exclusions / coordination

- Do not change the 8 MiB limit, template XML schema, import/apply/export mapping semantics, family/rule canonicality, or backup/atomic replace policy.
- Do not alter UI/native/release surfaces.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- A profile whose serialized XML exceeds 8 MiB throws `InvalidDataException` before the nonexistent destination directory is created.
- The final destination file remains absent.
- Existing defensive `Load(temp)` and atomic backup replacement remain in the valid save path.
- Re-fetch moving `main` target blob and inspect exact PR diff before integration.

## Completion condition

Current `main` preflights the explicit template size contract before filesystem mutation, focused regression source is merged, and this claim is closed `COMPLETED` with exact evidence.
