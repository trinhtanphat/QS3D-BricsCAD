# Work claim — Quantity Rule provenance canonical read

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-provenance-canonical-read`
- Registered: `2026-08-12T00:19:00+07:00`
- Baseline main SHA: `07c986cc4419eae81d11adf505b4586f7247c030`
- Priority: P1 — fail closed on malformed persisted quantity-rule provenance before cleanup/application mutation.

## Confirmed defect

`QuantityRuleEngine.GetStaleManagedOutputs(...)` currently trims the suffix of persisted `Rule:<Output>` property keys while reading it. A malformed key such as `Rule: Ghost` is therefore interpreted as canonical output `Ghost`, but cleanup later removes `Rule:Ghost` rather than the original malformed key. With no active rule this leaves the bad provenance behind indefinitely; with an active `Ghost` rule the malformed key is silently accepted and canonical `Rule:Ghost` can be written beside it. Blank keys such as `Rule:` / `Rule:   ` are also silently ignored.

`QuantityRulePreviewService.ManagedProvenance(...)` already treats these forms as invalid persisted state. The lower-level engine mutation boundary should apply the same canonical-read policy rather than repairing/ignoring malformed provenance implicitly.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleProvenanceCanonicalReadSmoke.cs` (new)
- `scripts/preflight-quantity-rule-provenance-canonical-read.py` (new)
- this claim file for close-out

## Intended contract

- Persisted `Rule:<Output>` keys require a non-blank output suffix with no surrounding whitespace.
- Malformed provenance fails closed before stale cleanup, rule evaluation or any quantity/property mutation.
- Canonical stale provenance continues to clean the matching quantity/property exactly as before.
- Active canonical rule application and canonical ownership behavior remain unchanged.
- `QuantityRulePreviewService` policy is not weakened or duplicated into a second repair path.

## Excluded scope

No rule formula/category semantics, no project revision ownership changes, no UI/native work, no GitHub Actions dispatch and no BricsCAD V25 runtime claim.

## Completion condition

The engine rejects malformed provenance at its public mutation boundary, focused auto-registered smoke/static coverage is on current `main`, and this claim is closed with exact SHAs and truthful validation limits.
