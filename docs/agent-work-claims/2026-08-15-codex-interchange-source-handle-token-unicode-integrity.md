# Work claim — Interchange source-handle provenance token Unicode integrity

- Status: `COMPLETED`
- Agent: `Codex / audit-interchange-gap-next`
- Registered: `2026-08-15T11:21:26+07:00`
- Issue: `#84`
- Baseline main SHA: `5a13195e2b49a64c5b2d728bf4af668d1b9bff88`
- Priority: bounded remote-safe interchange provenance lookup integrity

## Confirmed defect

`ProjectInterchangeSourceHandleProvenance.Token()` canonicalizes source project and Element identities and then uses replacement-fallback `Encoding.UTF8.GetBytes(...)`. A legitimate source project id ending in literal U+FFFD and malformed ids ending in a lone high or lone low UTF-16 surrogate therefore receive the same metadata token. `ReadSourceHandles()` validates the decoded source Element id but the element record does not contain the source project id, so a malformed source-project lookup can select and return handles belonging to the legitimate, different source project instead of failing closed.

The concrete `source-\uFFFD`, `source-\uD800`, and `source-\uDC00` inputs all produce `U09VUkNFLe-_vQ` under the current token implementation. The existing `StrictUtf8` encoder rejects the malformed forms with `EncoderFallbackException` while preserving the valid token bytes.

## Reserved scope

- In `src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs`, use only the existing `StrictUtf8` instance to encode the canonical identity inside `Token()`.
- Add one self-registering `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleProvenanceTokenUnicodeSmoke.cs` regression proving the literal-U+FFFD cross-project alias is closed, lone high/low surrogate lookup identities fail before observable mutation, and valid supplementary-Unicode project/Element identities still retrieve exact handles.
- Add one auto-discovered `scripts/preflight-interchange-source-handle-provenance-token-unicode.py` focused source/regression gate.
- Update only this claim with implementation and validation evidence after integration.

## Preserved contracts and exclusions

- Preserve provenance record version/layout, Base64 URL-safe token form, every valid token byte, case-insensitive lookup behavior, strict persisted-record decoding, handle validation/count/order/bounds, and Store rollback/audit/Touch ordering.
- Do not broaden `EncodeRecord()` or change JSON validation/reading/export, provenance target maps, CSV, IFC, BCF, semantic import/merge/remap/FieldMerge policy, `ProjectState`/domain behavior, or target-DWG handle ownership.
- No native adapter, BricsCAD/runtime/LOCAL, private/customer data, release/package, workflow, or GitHub Actions work.
- Keep issue `#84` open and stop before merging the implementation PR to `main`.

## Coordination evidence

Immediately before this claim, `origin/main` was refreshed to the baseline above. All open PR file lists and all ACTIVE/BLOCKED claims were re-audited. No open PR or active claim owns the exact provenance token source, proposed focused smoke, or proposed gate. The broad Core mutation-atomicity claim is conditional on mutation defects; this lane fixes a read-only lookup identity collision. The active FieldMerge claim owns coordinator/importer/documentation surfaces only. Earlier source-handle strict-decode, handle-integrity, and drawing-scope claims are `COMPLETED`, and the completed target-map/CSV/JSON/IFC Unicode lanes are excluded.

## Validation plan

- Run the new focused preflight and the repository smoke-registration gate.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release with zero warnings/errors.
- Run the focused self-registering smoke through the Core smoke executable, then run the full Core smoke suite.
- Re-fetch and re-audit exact `origin/main`, open PRs, ACTIVE/BLOCKED claims, ancestry, and changed-file scope before every publication; never force-push and never dispatch GitHub Actions.

## Implementation and completion evidence

- Claim chronology: claim commit `8b1bf19e4e4eaf7f1d2b8cc3169d6f281ff19288` was published through claim-only PR `#1583` and merged at `a8f7e56134cf42b28eb15d58155de40a585b840d` before any production/test/gate edit.
- Implementation chronology: product commit `2c0eb514de2426953b3460f4d8fbdead0fd3ae89` was reconciled and published at head `8270685158fb8866a1975a2f0695551ca2c1cd9c` through PR `#1592`, then merged directly to `main` at `a45759d7d9b7d90d63ad32f9e8bc4997e5c14d9a`.
- The merged implementation changed exactly the three reserved implementation surfaces: one strict encoder substitution in `ProjectInterchangeSourceHandleProvenance.Token()`, one focused self-registering smoke, and one focused auto-discovered preflight. All exclusions and valid record/token contracts were preserved.
- Independent exact-main validation at `e2dbb1e03748047f69a556240f8f85b2e7ccc17e` passed the focused provenance-token Unicode gate and smoke-registration gate; `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds completed with `0` warnings / `0` errors; the full Core smoke suite reported `ALL PASS`; the V25 build completed with `0` warnings / `0` errors; and the aggregate gate reported `824/824` passing.
- Closeout refreshed `origin/main` to `a81bc3e771bdc10c2fbb794b5a9dcb1508ee6e66`. Both the implementation merge and validation SHA remain ancestors; the only later delta from the validation SHA is project-version metadata, with no provenance source/test/gate change.
- No GitHub Actions workflow was dispatched or rerun by this lane. No native/runtime/LOCAL/private-data/release implementation was performed. Issue `#84` intentionally remains open for broader interoperability work.

## Completion condition

Satisfied: the exact bounded fix is integrated into `main`, independently validated on an exact descendant, and this claim is released as `COMPLETED`. Issue `#84` remains open for the broader interoperability gap.
