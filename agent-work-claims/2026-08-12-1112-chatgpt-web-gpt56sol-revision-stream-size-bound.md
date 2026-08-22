# Work claim — Revision parsed-stream size bound

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:12:00+07:00`
- Completed: `2026-08-12T11:15:00+07:00`
- Baseline main SHA: `c300c2db59663b11961fa1b49418d504e763aa58`
- Claim commit: `12fb9103f84844123fc2ed5059850f5c69214958`
- Source commit on branch: `5b4b6a0eb9f47af64f87ae9ee3d90550f8d310df`
- Regression-source commit on branch: `4f4c734d416c6fa6fe0f0455462d94f4a90db7a8`
- Pull request: `#808`
- Squash merge commit: `deb848933f7543ee43b04a29ea845fc84bed11ac`
- Priority: evidence-driven Core revision input/resource integrity

## Confirmed defect

`RevisionSnapshotStore.LoadDocument(...)` enforced the 64 MiB revision limit through `FileInfo.Length`, then opened and parsed a separate `FileStream`. The file could be replaced or grow between those operations, so the byte-size decision was not bound to the exact stream consumed by `XmlReader`. `MaxCharactersInDocument` is a character limit and not a substitute for the persisted byte contract.

This remained distinct from the completed Revision save-size preflight (#771), which protects write-side filesystem atomicity.

## Implemented

- Public `LoadDocument(path)` delegates to a private bounded overload using the existing 64 MiB limit.
- The private overload validates a positive configured maximum, resolves the full path, opens the exact `FileStream` that will be parsed, and checks `stream.Length` before `XmlReader` creation.
- Only the existing upper byte bound is retained, so empty-file behavior remains an XML/parse failure rather than a new size diagnostic.
- The existing `QS3D revision exceeds the maximum supported file size of 64 MiB.` error contract is preserved.
- XML security/schema/canonicality, `LoadWithBackupFallback`, post-write validation and #771 write-side size preflight remain unchanged.

## Regression source

`RevisionParsedStreamSizeSmoke` covers an invalid 4 KiB stream against a small private 512-byte limit, proving the byte-size guard fires before XML parsing without allocating a 64+ MiB fixture, plus a normal public Save/Load round trip.

## Integration evidence

While the branch was open, `main` advanced 15 commits, but `RevisionSnapshotStore.cs` retained exact pre-patch blob SHA `c0f27a0cd868c87ac1324e0300c973a2483362cb`; no concurrent source overlap was present. PR `#808` was squash-merged with expected head SHA `4f4c734d416c6fa6fe0f0455462d94f4a90db7a8` into `deb848933f7543ee43b04a29ea845fc84bed11ac`. Merged source was read back from `main` with blob SHA `56c20334455ec6d0a90a25a44d780aae0681a6a3`.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched and no executable .NET smoke/build or BricsCAD V25/V26 runtime PASS is claimed.
