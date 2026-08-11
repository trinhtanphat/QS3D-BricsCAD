# Agent work claim — QSDB map-key load canonicality

Status: ACTIVE

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA observed before reservation: `5d8f9c209e7f25b39510766f9c8f672ffd498679`

## Scope

Harden CAD-independent QSDB XML loading so persisted metadata/property map keys must already be canonical on disk; the loader must not silently normalize leading/trailing whitespace that save-side validation rejects.

Expected implementation surfaces:

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim file for completion status

## Concrete defect

Save-side `ValidateCanonicalKey()` rejects padded metadata/property keys. On load, however, map entry names pass structural XML validation and are later read through `Required(..., "name")`, which trims the raw attribute before the project-level canonical validator can observe the original persisted spelling. A tampered QSDB entry such as `name=" CanonicalKey "` can therefore be silently normalized to `CanonicalKey` instead of failing closed.

Duplicate map keys are already guarded separately by `ReadStringMap`; this claim does not change duplicate handling.

## Exclusions

- No schema/version expansion or migration behavior changes.
- No changes to general project/family/element ID trimming semantics outside map keys.
- No BricsCAD V25/native runtime, UI, quantity rules, updater, installer, or release changes.
- No GitHub Actions dispatch.

## Validation plan

- Extend canonical persistence smoke with a valid QSDB that is then tampered to add whitespace around a persisted metadata map key; load must throw `InvalidDataException` instead of normalizing it.
- Preserve canonical map round-trips and existing duplicate-key handling.
- Re-fetch current `main` and target files before writes; never force-push.
