# Work claim — updater manifest v2 compatibility

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-updater-manifest-v2-compat`
- Registered: `2026-08-11T21:26:00+07:00`
- Baseline main SHA: `f373d932fd90faf7355283234da7e711633339d8`
- Priority: P0 functional/security compatibility blocker in the newly released updater source lane.

## Verified defect

`new-v25-update-manifest.ps1` currently emits `schemaVersion = 2` and a signed-plugin-derived `productVersion`, while `update-v25.ps1` still rejects every manifest whose `schemaVersion` is not `1`. A correctly generated current release manifest therefore fails before package download/install.

## Reserved scope

Make the updater consume the current manifest schema without weakening existing v1 compatibility or signed-package version/signer checks. Bind schema-v2 `productVersion` to the Authenticode-validated QS3D plugin payload so same-AssemblyVersion prerelease updates cannot silently accept the wrong signed prerelease package.

## Expected surfaces

- `scripts/update-v25.ps1`
- `scripts/preflight-auto-update.py`
- `scripts/new-v25-update-manifest.ps1` only if a source mismatch requires a narrow generator correction
- this claim file

## Excluded scope

- in-plugin updater UI/coordinator/Ribbon behavior
- GitHub release selection/SemVer ordering
- release workflow dispatch/publication
- unrelated installer/package behavior
- Quantity, Workspace, Direct Draw, reporting, Core mutation and other active lanes
- native/local signed-update qualification already delegated to `LOCAL-009`

## Validation plan

- re-read latest updater/generator sources before writing
- preserve schema-v1 read compatibility while accepting schema v2
- require and validate strict SemVer `productVersion` for schema v2
- after package Authenticode verification, compare manifest productVersion with signed plugin ProductVersion before install
- strengthen auto-update preflight against generator/updater schema drift
- inspect commit/status evidence; do not dispatch manual Actions

## Completion condition

Current generated manifests are consumable by current updater source, v2 productVersion is cryptographically anchored through the validated plugin payload, regression coverage prevents schema drift, and the claim is marked `COMPLETED`.
