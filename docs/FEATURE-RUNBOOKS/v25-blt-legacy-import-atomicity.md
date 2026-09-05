# V25 BLT legacy import atomicity qualification

Status: `LOCAL_ONLY / PENDING_LOCAL`

This matrix qualifies issue #5776 only after the exact source SHA under test has passed protected hosted source/static/V25 locked-reference compilation. Hosted CI is not proprietary BLT3D or licensed BricsCAD runtime evidence.

## Artifact identity

Record before launch:

- exact git SHA and branch;
- BricsCAD V25 build/version;
- `QS3D.BricsCAD.V25.dll` SHA-256;
- fixture DWG SHA-256;
- fixture source Handle(s);
- whether the project already existed before the test.

Do not reuse evidence from another SHA or preview artifact.

## P01 — control import

1. Open a clean copy of a known BLT fixture with a source object that is `CanImport=true` and has authoritative geometry or explicit unit-labelled legacy quantity evidence.
2. Resolve drawing units normally.
3. Run `QS3DBLTPROBE` and retain the sanitized probe report.
4. Run `QS3DBLTIMPORT` without failure injection.
5. Verify exactly one semantic element owns the original source Handle and expected BLT evidence/provenance is present.
6. Save, close, reopen, and verify the same semantic identity/evidence survives.

Expected: PASS. Source CAD entity/Handle remains unchanged.

## P02 — post-capture evidence failure on an existing project

Use a sanitized local-only fixture/harness that causes BLT post-capture evidence mutation to throw *after* semantic capture core succeeds (for example XML-invalid imported text crossing canonical `ProjectElement.SetProperty` validation). Do not modify production safety validation to manufacture the failure.

1. Establish and persist a baseline project state; record element count, target Handle owner, family/floor relation, quantities, BLT properties and project update identity.
2. Invoke `QS3DBLTIMPORT` against the failure fixture.
3. Confirm the command reports the expected failure.
4. Re-read project state before any corrective command.

Expected: project state is exactly the pre-import baseline for the failed source; there is no partially captured semantic element, no changed Handle owner, no partial CAD/BLT property set, no quantity drift and no relation drift.

## P03 — failure when no project existed

1. Start from a clean drawing copy with no QS3D project context.
2. Trigger the same post-capture evidence failure.
3. Confirm the command reports failure.
4. Verify the temporary/newly-created project context was forgotten rather than retained as a partial project.

Expected: no project survives the failed atomic import.

## P04 — mixed batch fail-closed boundary

Use at least two ready BLT sources where an earlier source imports successfully and a later source triggers the post-capture failure.

Expected: the failing source rolls back to its own pre-capture state; already-completed earlier source remains intact. Record this as per-source atomicity, not all-batch atomicity.

## P05 — retry and cold reopen

1. Remove only the deliberate failure condition; do not alter source Handle/category evidence.
2. Re-run `QS3DBLTIMPORT`.
3. Verify the source imports once with expected evidence and no duplicate semantic owner.
4. Save, close, reopen and repeat Handle/quantity/property/family/floor verification.

Expected: deterministic successful retry and cold-reopen parity.

## Acceptance boundary

`LOCAL_PASS` requires P01–P05 on the exact authorized SHA and artifact identities above. Any startup failure, unavailable licensed host, different SHA, uncontrolled fixture mutation, or missing cold-reopen check is `NO_RESULT`, not PASS. Never infer native/proprietary runtime behavior from hosted CI.