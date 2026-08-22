# Work claim — Interchange validator Unicode integrity

- Status: `COMPLETED`
- Agent: `gpt56sol /root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T15:38:35+07:00`
- Baseline main SHA: `7ccd75c52a26d038e80f28ab82a80ca688662d2a`
- Priority: issue #84 — prevent strict semantic-snapshot validation/typed reading from silently replacement-encoding malformed caller-provided UTF-16

## Reserved scope

Make `ProjectInterchangeJsonValidator.Validate(string)` encode its caller-provided JSON through the existing strict UTF-8 encoder before size checking/deserialization. A lone high or low UTF-16 surrogate must fail closed with deterministic `JSON_UTF16` validation evidence; a valid surrogate pair must remain byte-valid and survive typed reading exactly.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs` — strict string-to-UTF-8 validation boundary only.
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidatorUnicodeIntegritySmoke.cs` — new auto-registered validator/typed-reader regression.
- `scripts/preflight-interchange-validation.py` — focused strict-string validation tokens only.
- This claim file for completion closeout.

## Excluded scope

- No changes to `ProjectInterchangeJsonExporter`, typed-reader parsing/mapping, import planners/importers/mutation policies, schema/version, native commands/UI, CSV/XLSX/template surfaces, or documentation.
- No BricsCAD, private data, GitHub Actions, release, LOCAL-002/003/004, runner, probe, or runtime qualification.

## Validation plan

- Run `py -3 scripts/preflight-interchange-validation.py` and the relevant validator/validated-reader/interchange gates.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release with warnings as errors.
- Run the full Core smoke executable; report any first unrelated current-main blocker without expanding this claim.
- Review the exact diff and verify claim/implementation/closeout ancestry on current `origin/main`.

## Coordination

No open PR or ACTIVE/BLOCKED claim owns validator Unicode. The active Core mutation audit is currently scoped to persistence/session atomicity and does not own this read-only interchange validation boundary. The completed exporter surrogate-integrity lane remains unchanged and provides the adjacent fail-closed precedent.

## Completion condition

A normal implementation PR is merged to current `main`, issue #84 receives a source-only update, and this claim records exact validation and merge evidence without overstating broader interoperability or native runtime completion.

## Completion evidence

- Claim commit: `c0c6438f9036183fb25831bf21847b6bd6846c36`; claim-only PR #1223 merged as `50a622d51f208c126d361ec099e288cc46b8960c` before source edits.
- Implementation commit: `94d539e2044d8a2efd18a6160f5cab287b98f2a7`.
- Implementation PR #1228 merged normally to `main` as `5ce2459d134e7562ac56d6d9a2969bbebb1f1c40`.
- Exact implementation diff: validator strict string encoding, new auto-registered validator/typed-reader Unicode smoke, and the existing interchange-validation preflight only.
- PASS: `preflight-interchange-validation.py`, `preflight-interchange-validated-reader.py`, `preflight-interchange-validator-canonical.py`, `preflight-interchange-bounded-file-read.py`, and `preflight-interchange-json.py` via `py -3`.
- PASS: `QS3D.Core` Release build and `QS3D.Core.SmokeTests` Release build, each with 0 warnings / 0 errors; the focused Unicode initializer executed successfully before later unrelated module-initializer failures.
- Full Core smoke was rerun on exact implementation merge `5ce2459d134e7562ac56d6d9a2969bbebb1f1c40` and does not have a PASS claim: after the concurrent Project Browser fixture fix, the first out-of-lane failure is `ProjectElementNullScalarPersistabilitySmoke.ConstructorTrimAndSetterExactnessRemain`, whose padded `FamilyId` expectation predates canonical relation setter trimming.
- Issue #84 source updates: https://github.com/trinhtanphat/QS3D-BricsCAD/issues/84#issuecomment-5291333732 and exact-merge rerun https://github.com/trinhtanphat/QS3D-BricsCAD/issues/84#issuecomment-5291341089.
- No exporter, typed-reader implementation, import mutation policy, native/UI, documentation, BricsCAD runtime, private data, GitHub Actions, release, or LOCAL surface was changed.
