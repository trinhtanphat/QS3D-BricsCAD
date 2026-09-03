# Auto Room source-signature input bound

## Scope

This Core model-lifecycle contract covers semicolon-delimited Auto Room topology/source-handle text consumed from `BoundarySourceSignature`, the legacy `BoundarySourceHandles` fallback, lookup signatures, stale-room matching, and `MarkActive`.

## Integrity invariant

Auto Room source-handle inputs have one canonical envelope of **5,000 non-empty input tokens**. Text parsing must enforce that envelope while scanning delimiters and before materializing token 5,001; eager `string.Split` is prohibited because it allocates the complete token array before the bound can reject oversized persisted metadata.

Historical `StringSplitOptions.RemoveEmptyEntries` behavior is preserved: zero-length delimiter tokens do not consume the envelope, while whitespace-only non-empty tokens do consume an input slot and are subsequently ignored by canonical normalization. Canonical handle normalization, case-insensitive deduplication and deterministic sorting remain unchanged.

`MarkActive` must normalize and validate the complete bounded source signature before changing lifecycle state or removing stale metadata, so an oversized signature cannot leave a partially reactivated room.

## Deterministic qualification

`AutoRoomSourceSignatureBoundSmoke` covers exact 5,000-token acceptance, 5,001-token rejection for both persisted signature and persisted fallback fields, pre-mutation failure in `MarkActive`, empty-token compatibility, and whitespace-token envelope accounting. `preflight-autoroom-source-signature-bound.py` pins bounded delimiter scanning, all text-signature call sites, absence of eager semicolon `Split`, and validation-before-mutation ordering.

Runtime is `NOT_APPLICABLE`: this is deterministic Core/model-lifecycle data-integrity behavior and does not constitute licensed BricsCAD `LOCAL_PASS` evidence.
