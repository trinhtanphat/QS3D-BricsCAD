# Work claim — BCF timestamp canonical UTC current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-bcf-timestamp-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `e73c39a25deb427d81ba66fe08418e60e73bd6f6`
- Issue: `#1512`
- Superseded integration-v2 PR: `#1513`
- Branch: `agent/chatgpt-gpt56sol/bcf-timestamp-main-recovery-20260815`
- Priority: Core P1 interoperability / canonical reader integrity

## Confirmed current-main defect

`BcfIssueExchangeSerializer.ParseUtc(...)` still uses tolerant `DateTimeOffset.Parse(... AssumeUniversal | AdjustToUniversal)` and returns normalized UTC. It therefore accepts offset or otherwise non-canonical timestamp text that the canonical serializer does not emit.

## Reserved recovery surfaces

- `src/QS3D.Core/Export/BcfIssueExchangeSerializer.cs`
- focused BCF timestamp canonicality smoke
- focused smoke registration
- this claim file

## Recovery contract

- require exact `DateTime` round-trip `O` timestamp text;
- require `DateTimeKind.Utc`;
- require exact equality with canonical serializer output;
- reject topic/comment explicit offsets and shortened/non-canonical UTC text as `InvalidDataException`;
- preserve exact canonical serializer round-trip and all unrelated BCF semantics;
- no `BcfIssueExchange.cs`, `BcfZipPackage.cs`, global IFC contract, adapter/native, workflow/release, schema or product-boundary changes;
- no direct main merge and no manual GitHub Actions dispatch/rerun.

## Prior reviewed evidence

- v2 source: `5be56fe971c2f79226bd4f75662d6e4ae7d908a2`
- v2 smoke: `a596db471fc0fcd78ca6bf14931b6e0a6f55c48e`
- v2 registration: `f3e37cf30b031bdfc734134c52225d1a1e969a28`

Implementation begins only after this claim is published.
