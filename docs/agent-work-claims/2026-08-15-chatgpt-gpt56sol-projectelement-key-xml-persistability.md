# Work claim — ProjectElement property/quantity key XML persistability

- Status: `RELEASED` — implementation complete; pending authorized review/integration
- Agent: `chatgpt-gpt56sol-projectelement-key-xml-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `6b686a32934ef9fd750f3ff5ade6508cc14259c9`
- Latest reconciled main: `34cf22de040ff5425b65d0e8c1edb7bc29a8cdf6`
- Issue: `#1572`
- PR: `#1578` (`ready for review`, latest readback `mergeable=true`)
- Branch: `agent/chatgpt-gpt56sol/projectelement-key-xml-persistability-20260815`
- Priority: Core P1 persistence / public mutation-boundary integrity

## Confirmed defect

`ProjectElement.SetProperty(...)` and `SetQuantity(...)` trimmed property/quantity keys and rejected control characters, but did not preflight XML character representability. Canonical QSDB persistence writes those keys into XML `name` attributes, so lone UTF-16 surrogates could be accepted by either public setter, mutate dictionary/dirty/timestamp state, and leave live state canonical persistence cannot represent.

## Implemented fix

- after existing trim/control checks, normalized property keys route through the existing `RequireXmlText(...)` helper;
- quantity keys use the same preflight;
- `RemoveProperty(...)` remains unchanged so invalid raw state can still be repaired;
- focused smoke rejects lone high and lone low surrogate keys before dictionary/dirty/timestamp mutation;
- valid trimmed supplementary-Unicode property/quantity keys survive exact QSDB SaveNew/Load.

## Evidence

- claim-only: `62aaf04c8b28771e948a28e6d14035e80bfcce8f`
- implementation/regression: `e7a238163f838c79bee9eb259624760af409f7a3`
- non-force reconciliation onto `34cf22de040ff5425b65d0e8c1edb7bc29a8cdf6`: `cad06dd9ba840100b266ae29d547e7d0e5eb3be5`
- PR: `#1578`
- final compare before handoff: ahead 3 / behind 0; exactly four task files
- production source delta: `+2/-0`
- exact GitHub source/diff readback: PASS
- managed build/smoke: NOT_RUN — no `dotnet` execution available in this session; no PASS claimed
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Coordination / exclusions

This lane intentionally covers only public property/quantity **key** persistability. Current-main readback separately shows ProjectElement identity/relation/fingerprint persisted-text boundaries that still rely on trim/control checks; those are not claimed as fixed here and should be treated as a separate follow-up candidate after fresh overlap checks.

No raw dictionary redesign, `RemoveProperty` restriction, ProjectState/Family/Floor/Zone service, serializer/schema, adapter/native, workflow/release or product-boundary changes. No direct main merge by this normal-agent session.

## Handoff / release

All source/regression state is represented by ready PR #1578 against `main`. Reservation ownership is released from this session. Keep Issue #1572 open until an authorized coordinator integrates #1578 and remote ancestry/source readback confirms the two public key guards on `main`.
