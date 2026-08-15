# Work claim — BCF timestamp canonical UTC current-main recovery

- Status: `COMPLETED` — implementation integrated and independently validated on the exact main merge
- Agent: `chatgpt-gpt56sol-bcf-timestamp-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `e73c39a25deb427d81ba66fe08418e60e73bd6f6`
- Latest reconciled main: `7dbe90ee27b3e22dfdcf2163a248109151dbac13`
- Issue: `#1512`
- Replacement current-main PR: `#1563` (`ready for review`, latest readback `mergeable=true`)
- Superseded integration-v2 PR: `#1513` (`closed`, not merged)
- Branch: `agent/chatgpt-gpt56sol/bcf-timestamp-main-recovery-20260815`
- Priority: Core P1 interoperability / canonical reader integrity

## Confirmed current-main defect

`BcfIssueExchangeSerializer.ParseUtc(...)` accepted offset/non-canonical timestamp text through tolerant `DateTimeOffset.Parse(... AdjustToUniversal)` and normalized it into UTC even though the canonical writer only emits exact UTC round-trip `O` text.

## Recovered implementation

- exact `DateTime.TryParseExact(..., "O", RoundtripKind)` parsing;
- `DateTimeKind.Utc` required;
- byte-for-byte equality with canonical `parsed.ToString("O", InvariantCulture)` required;
- focused smoke rejects topic/comment explicit offsets and shortened UTC text;
- canonical serializer-emitted UTC payload deserializes and reserializes exactly;
- unrelated BCF schema/GUID/numeric/collection/camera/ordering semantics unchanged.

## Evidence

- claim-only: `bf0fa9b28c62872c7db7f3ccd5dffdf4567385ff`
- implementation: `a054f5d886fa1132e5ae8b82876d06364a8dfc89`
- reconciliation onto `88f83db19ed5dfd85606d5a5e00adfc28f4fd99c`: `93dede0fbae7b6362283f580e5ad4b50019a4caf`
- latest reconciliation onto `7dbe90ee27b3e22dfdcf2163a248109151dbac13`: `4106e526f4a269965c1a2ef5163427a96e4ce4e1`
- replacement PR: `#1563`
- replacement PR merge: `f10c1fed5af58e2a0f3be1d63637c190696eb605`
- task diff: exactly four files; production source delta `+5/-2`
- exact GitHub source/diff readback: PASS
- prior v2 source/smoke/registration: `5be56fe971c2f79226bd4f75662d6e4ae7d908a2` / `a596db471fc0fcd78ca6bf14931b6e0a6f55c48e` / `f3e37cf30b031bdfc734134c52225d1a1e969a28`
- coordinator validation on exact merge `f10c1fed5af58e2a0f3be1d63637c190696eb605`: smoke registration, interchange validation and interchange JSON focused gates PASS; Core and SmokeTests Release build 0 warnings/0 errors; full registered Core smoke `ALL PASS`
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Coordination / exclusions

- #1513 was closed only after #1563 was verified clean/mergeable; old branch/history remains intact.
- #1506/#1559 remains the separate BCF model XML representability lane.
- #1444 remains the separate BCF package structural-integrity lane.
- No BCF model, ZIP/package, IFC contract, adapter/native, workflow/release, schema or product-boundary changes.
- No direct main merge by this normal-agent session.

## Handoff / release

PR #1563 is merged at `f10c1fed5af58e2a0f3be1d63637c190696eb605`, remote ancestry/source readback and exact-merge validation are complete, and Issue #1512 is closed. Reservation ownership is released and this claim is complete.
