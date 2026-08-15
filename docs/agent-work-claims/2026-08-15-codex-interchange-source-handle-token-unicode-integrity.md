# Work claim — Interchange source-handle provenance token Unicode integrity

- Status: `ACTIVE`
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

## Completion condition

This claim may become `COMPLETED` only after the exact implementation is integrated into `main` and independently validated. Issue `#84` remains open for the broader interoperability gap.
