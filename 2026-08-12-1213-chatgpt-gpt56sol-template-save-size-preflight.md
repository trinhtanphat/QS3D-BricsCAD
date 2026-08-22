# Work claim — Template save size preflight

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-template-save-size-preflight`
- Registered: `2026-08-12T12:13:00+07:00`
- Last Updated: `2026-08-12T12:18:00+07:00`
- Baseline main SHA: `ef75c65b742726b3c4ff98a660084cf3ed48bccc`
- Claim commit: `60efa1c4d111f82533a6687bded8def6b50f4a6f`
- Implementation PR: `#870`
- Main implementation commit: `816f6a92965661d4c3afa3a53662023fb8a50915`
- Priority: evidence-driven persisted-template side-effect defect found during owner-requested `continue all`
- Task Key: `TEMPLATE-SAVE-SIZE-PREFLIGHT`

## Confirmed defect

`TemplateProfileStore` has an explicit 8 MiB persisted-file limit. `LoadDocument(...)` rejected files above that limit before XML parsing, but `Save(...)` validated the in-memory profile, created the destination directory, created/wrote a sibling temp file, and only then called `Load(temp)` to discover that serialized output exceeded 8 MiB. An invalid oversized profile could therefore mutate the filesystem before the save was rejected.

## Implemented

- Preserved profile validation and `Path.GetFullPath(...)` error precedence.
- Serialize the exact XML payload into a `MemoryStream`, enforce the existing 8 MiB byte limit, and only then create the destination directory/temp path.
- Write the validated byte snapshot to temp so the preflight bytes and persisted bytes are identical.
- Preserved defensive `Load(temp)` validation and `ReplaceWithBackup(...)` atomic/backup semantics.
- Added focused registered Core smoke coverage with a >8 MiB UTF-8 family-property payload; rejection asserts the nonexistent destination directory/file were not created.

## Validation evidence

- Source branch commit: `40b78dac19353f392de56cf98016326868122097`.
- Smoke branch commit: `f3d8a05fa366c8c25f1d3cd710ddbf82188039d0`.
- Registration branch commit/head: `543b5e2d9f9b042690027dec6b22d69a25815e86`.
- Exact PR #870 unified diff reviewed before merge: 3 files, +73/-1; production change was two focused hunks (+12/-1).
- Squash merge to `main`: `816f6a92965661d4c3afa3a53662023fb8a50915`.
- No force-push, no GitHub Actions/build/release dispatch, and no executable smoke or BricsCAD V25/V26 runtime PASS claimed.

## Completion

Current `main` preflights the explicit template size contract before filesystem mutation while preserving defensive reload and atomic backup replacement. This claim is closed `COMPLETED`.
