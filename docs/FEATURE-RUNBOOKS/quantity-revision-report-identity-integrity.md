# Quantity revision report identity integrity

## Scope

`QuantityRevisionReport.Build` and `QuantityRevisionReport.Summarize` are public Core reporting boundaries over caller-constructible revision/report objects. Identity text admitted by these APIs must remain compatible with the revision domain's canonical persistence/comparison contract.

The protected contract is limited to project IDs, element IDs, quantity keys and summary quantity names. Category validation and finite numeric behavior remain unchanged.

## Fail-closed identity admission

Required identity text remains non-empty, exact-trim canonical and free of control characters. In addition, identity text must be well-formed XML character data: malformed UTF-16 surrogate sequences and other XML-invalid characters are rejected before report rows are published or summary groups are formed.

Validation is performed through `XmlConvert.VerifyXmlChars` from the canonical identity validator. This aligns quantity revision reporting with `RevisionService.Compare`, which already rejects XML-invalid revision payload identities before delta materialization.

Valid supplementary-plane Unicode remains supported and is preserved exactly. The fix must not replace valid surrogate pairs, normalize case, or rewrite identity strings.

## Deterministic regression

`QuantityRevisionReportIdentityIntegritySmoke` is module-initialized and covers:

- malformed project identity rejected by `Build`;
- malformed element identity rejected by `Build`;
- malformed quantity identity rejected by `Build`;
- malformed summary quantity identity rejected by `Summarize`;
- valid supplementary-plane Unicode preserved exactly through both Build and Summarize.

The auto-discovered `scripts/preflight-quantity-revision-report-identity-integrity.py` pins production admission, hostile regression coverage and this runbook contract.

## Validation boundary

Runtime: NOT_APPLICABLE — this is deterministic Core revision-report identity integrity. No licensed BricsCAD runtime evidence is required or claimed.
