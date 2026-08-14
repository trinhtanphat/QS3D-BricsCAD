# Work claim — EntitySnapshot identifier control-character canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-entitysnapshot-identifier-control-20260814`
- Registered UTC: `2026-08-14T01:41:00Z`
- Completed UTC: `2026-08-14T01:45:00Z`
- Baseline main SHA: `023a2d89e719d8051dbcd0fa91e780de6fe1ebaf`
- Priority: `P1 foundation hardening`

## Verified defect

Existing EntitySnapshot canonicalization explicitly trimmed surrounding whitespace from both CAD `Handle` and `EntityType`, and regression coverage preserved those canonical values through recognition. The constructor still accepted internal control characters in either identifier. A malformed handle could therefore cross the canonical CAD identity boundary, while a malformed entity type could enter recognition normalization/ranking inputs even though both fields were already treated as canonical identifiers.

## Completed implementation

- `31b26d789b8fec8acbf37bb4f4aba57c0b7e62ea` — updated `src/QS3D.Core/Model/EntitySnapshot.cs` on current `main`.
- `c1b4ce4c994a2b256d1febb72fc62bd99f8e6d52` — added self-registering `tests/QS3D.Core.SmokeTests/EntitySnapshotIdentifierControlCharacterSmoke.cs`.
- Handle and EntityType now share a canonical identifier guard that preserves existing blank rejection and surrounding-whitespace trimming while rejecting control characters after trim.
- Existing EntityType casing, Layer/Metadata behavior, generated-ownership marker and metric validation remain unchanged.

## Validation recorded

- claim-first ownership was published to `main` at `ae7ff334e853ff4fe0951bed4ced6c1d719ea29c` before source/test work;
- branch self-review showed only `EntitySnapshot.cs` plus one focused smoke file;
- current-main overlap was rechecked; concurrent work touched Curtain/LOCAL-003 only and not Model;
- parent-specific Git Data publish attempts were allowed to fail non-fast-forward without force; the final source publish used the live source blob SHA through the Contents API, then the smoke was added on current `main`;
- remote `main` was re-fetched at `c1b4ce4c994a2b256d1febb72fc62bd99f8e6d52`, and the updated source blob was re-fetched from that commit;
- smoke covers newline/U+001F rejection for Handle, tab/U+001F rejection for EntityType, and preservation of existing Handle trim plus EntityType trim/casing behavior;
- no GitHub Actions were dispatched, no managed runtime/native BricsCAD execution was performed, and no runtime/native PASS is claimed;
- no force-push.

## Scope exclusions preserved

No CAD-handle hexadecimal grammar, EntityType casing policy, Layer, Metadata, metrics, recognition/capture semantics, adapters, V25/native host code or persistence behavior were modified.
