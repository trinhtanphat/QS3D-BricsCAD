# Work claim — release #30 Template Profile schema preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-template-profile-schema-preflight`
- Registered: `2026-08-12T09:46:00+07:00`
- Completed: `2026-08-12T09:48:00+07:00`
- Baseline main SHA: `ec8e35ca150f2e63ef6b2eb51d2fbbe969957b43`
- Claim commit: `732aa3fd73d607615fc884c218920df0e0f6eb7a`
- Implementation commit: `a859874745ef83799ee8315c71582e7c27eab546`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reported six Template Profile XML schema token failures because the gate still pinned `RequireAtMostOne`/`Skip(1).Any()` after the validator was hardened to require exactly one canonical singleton container and canonical root section order.

## Completed scope

Reconciled only `scripts/preflight-template-profile-schema.py` with the current stronger `TemplateProfileXmlSchemaValidator` contract. Validator/store production code remained unchanged.

## Implemented gate contract

- Requires `RequireExactlyOne` for root `families`, `rules`, `layerMappings`, `bqColumns` and Family `properties`.
- Requires canonical root section order through `expectedRootOrder` and `SequenceEqual(expectedRootOrder)`.
- Requires the current bounded singleton helper with `parent.Elements(XName.Get(childName)).Take(2).Count()` and `count != 1`.
- Explicitly fails if `RequireAtMostOne(` returns.
- Retains exact-root namespace, allowed-attribute/child, non-whitespace-text and Store validation-before-read checks.

## Validation performed

- Verified claim commit `732aa3fd73d607615fc884c218920df0e0f6eb7a` was current `main` immediately after claim creation.
- Re-fetched the exact gate before implementation.
- Read back the implemented gate from `main` at blob `72df32068f3d3e470629dc55e7c4f7e38acbd3cf`.
- Re-read current validator and confirmed the stronger exactly-one/canonical-order production behavior remained unchanged.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The Template Profile schema gate now pins the current stronger exactly-one/canonical-order validator without weakening XML safety, and this reservation is released.
