# Work claim — Quantity Rule provenance canonical read

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-provenance-canonical-read`
- Registered: `2026-08-12T00:19:00+07:00`
- Completed: `2026-08-12T00:22:00+07:00`
- Baseline main SHA: `07c986cc4419eae81d11adf505b4586f7247c030`
- Reservation commit: `bfbaaf35d7e77134be9eb530975ce9809f9d2a8e`
- Priority: P1 — fail closed on malformed persisted quantity-rule provenance before cleanup/application mutation.

## Defect fixed

`QuantityRuleEngine.GetStaleManagedOutputs(...)` trimmed the suffix of persisted `Rule:<Output>` property keys while reading it. A malformed key such as `Rule: Ghost` was interpreted as canonical output `Ghost`, while cleanup later targeted `Rule:Ghost` rather than the original malformed key. With no active rule this left bad provenance behind indefinitely; with an active `Ghost` rule the malformed key was silently accepted and canonical `Rule:Ghost` could be written beside it. Blank keys such as `Rule:` / `Rule:   ` were also silently ignored.

The engine now reads the suffix exactly and requires it to be non-blank and already canonical with no surrounding whitespace. Malformed persisted provenance throws before active/stale classification, cleanup, formula evaluation or quantity/property mutation. This matches the existing fail-closed policy already used by `QuantityRulePreviewService.ManagedProvenance(...)` without introducing an implicit repair path.

## Published commits

- `29e798b8122364120fd322bb3ca0aebe9224969c` — `fix(quantity): reject malformed rule provenance keys`.
- `b47f39fcffb2ecd53a808b515b7b20ffb0b3c392` — `test(quantity): guard canonical rule provenance reads`.
- `e07818b0481838fae3536a75b4ba3ec0ce2efe5f` — `test(quantity): pin canonical rule provenance reads`.

## Preserved contract

- Canonical stale provenance still removes the exact managed quantity/property and reports one cleanup operation.
- Active canonical rule application, formula/dependency ordering and canonical element ownership are unchanged.
- `QuantityRulePreviewService` canonical provenance policy remains unchanged.
- `QuantityRuleEngine` remains revision-agnostic; project revision ownership remains with its callers/batch boundaries.

## Validation notes

Current `main` source, auto-registered focused smoke and dedicated static preflight were re-fetched after publication and contain the intended exact-read/fail-closed contracts. Writes used current main/current blob state while the branch was moving; no force-push or concurrent overwrite was used. The smoke/preflight were not executed from a full repository checkout in this connector-only lane, so no executable Core PASS is claimed. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.

## Excluded scope

No rule formula/category semantics, no project revision ownership changes, no UI/native work and no release workflow changes.

## Completion condition

Satisfied for the remote-safe source/static contract: the engine rejects malformed persisted provenance before mutation, canonical stale cleanup remains intact, focused regression/static coverage is on current `main`, and this reservation is released.
