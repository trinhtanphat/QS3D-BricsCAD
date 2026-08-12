# Work claim — release #30 Template Profile schema preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-template-profile-schema-preflight`
- Registered: `2026-08-12T09:46:00+07:00`
- Baseline main SHA: `ec8e35ca150f2e63ef6b2eb51d2fbbe969957b43`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports six Template Profile XML schema token failures because the gate still pins `RequireAtMostOne`/`Skip(1).Any()` after the validator was hardened to require exactly one canonical singleton container and canonical root section order.

## Reserved scope

Reconcile only `scripts/preflight-template-profile-schema.py` with the current stronger `TemplateProfileXmlSchemaValidator` contract. Preserve validator/store production code unchanged.

## Canonical evidence

- `TemplateProfileXmlSchemaValidator.Validate` now calls `RequireExactlyOne` for root `families`, `rules`, `layerMappings`, `bqColumns` and Family `properties`.
- `RequireExactlyOne` uses bounded `parent.Elements(XName.Get(childName)).Take(2).Count()` and requires `count == 1`.
- Root containers additionally must match canonical section order through `SequenceEqual(expectedRootOrder)`.
- Foreign namespaces, unknown attributes/children and non-whitespace text remain fail-closed.
- `TemplateProfileStore.Load` still calls validator before reading schema/profile fields.

## Expected surfaces

- `scripts/preflight-template-profile-schema.py`
- this claim file for close-out

## Excluded scope

- No edits to TemplateProfileXmlSchemaValidator.cs, TemplateProfileStore.cs, import/export behavior or template data.
- No weakening from exactly-one back to at-most-one.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace obsolete `RequireAtMostOne(...)` requirements with all five current `RequireExactlyOne(...)` calls.
- Replace obsolete `parent.Elements(name).Skip(1).Any()` with bounded `Take(2).Count()`, `count != 1`, and canonical root `SequenceEqual(expectedRootOrder)` checks.
- Retain all namespace/attribute/child/text validation tokens and Store validation-before-read ordering.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for Template Profile schema validator/preflight. Current active Regeneration claim is unrelated.

## Completion condition

The Template Profile schema gate pins the current stronger exactly-one/canonical-order validator without weakening XML safety, is pushed to `main`, and this claim is closed with exact evidence.
