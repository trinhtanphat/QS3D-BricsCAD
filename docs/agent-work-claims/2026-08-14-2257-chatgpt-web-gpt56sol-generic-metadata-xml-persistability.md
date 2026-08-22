# Agent work claim — Generic project metadata XML persistability

- Agent: `chatgpt-web-gpt56sol-generic-metadata-xml-persistability`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `5f78adec71f292bf04014283bcb5b7825ef3bbae`
- Claim commit: `81403a9adb71c9f75bf4aee1496d4f98c54b35cc`
- Implementation branch: `agent/chatgpt-web-gpt56sol/generic-metadata-xml-persistability-20260814`
- Source commit: `5b74f81e6b5f9e7404f54f217cdae2d051626347`
- Regression commit / implementation head: `7c688954dc570b735c63fd8403532a3a700a4320`
- Integration branch: `integration/chatgpt-web-gpt56sol-generic-metadata-xml-persistability-20260814`
- Final integration / source landing: `df435ee365ff2964c5fdd533919011658beb750e`
- Priority: Core P1 persistence integrity

## Reserved scope

Fixed one confirmed public-mutation persistability gap in generic `ProjectState.Metadata`. QSDB writes every metadata entry as XML attributes and its current schema requires metadata keys to be non-empty and canonical (no leading/trailing whitespace), but the public dictionary previously rejected only null keys and normalized only null values. Public callers could therefore add blank/padded keys or XML-illegal key/value text that entered project state but was rejected by the canonical Save/schema boundary.

This lane adds fail-before-write validation only to public Add/indexer Set mutations. Accepted generic metadata remains revision-neutral exactly as established by existing project-browser and reserved-mapping ownership semantics.

## Changed surfaces

- `src/QS3D.Core/Domain/ProjectMetadataDictionary.cs` — public indexer/Add now require canonical non-empty keys and XML-representable key/value text before the existing reserved-catalog validation/backing mutation path. Null values still canonicalize to empty string. Internal owned/persistence hydration paths are unchanged.
- `tests/QS3D.Core.SmokeTests/ProjectMetadataPersistabilitySmoke.cs` — focused deterministic source coverage for valid generic revision neutrality, blank/padded/XML-illegal key rejection, XML-illegal replacement-value rejection, rejection atomicity, and exact XML-valid whitespace/newline QSDB SaveNew→Load round-trip.

## Excluded scope preserved

- No broad semantic versioning for generic metadata.
- Reserved `QS3D.Mapping.v1.*` ownership/revision semantics are unchanged; the completed reserved-mapping metadata lane remains authoritative.
- Internal `AddOwned`, `SetPersistenceValue`, `ReplacePersistenceState`, QSDB/snapshot hydration semantics were not changed.
- `QsdbProjectStore`, `QsdbProjectXmlSchemaValidator`, schema/migration, Project Browser business semantics, material catalog encoding, family/element property maps, native adapters, CI/release/signing, and LOCAL_ONLY BricsCAD qualification were not changed.
- No manual GitHub Actions dispatch/rerun/cancel was performed.

## Evidence and integration

- At baseline `5f78adec71f292bf04014283bcb5b7825ef3bbae`, public `ProjectMetadataDictionary.Add` and indexer Set delegated directly to `Set(...)`, which checked only null key / duplicate-add and normalized null value to empty. `QsdbProjectStore.Map(...)` serializes map keys/values directly as XML attributes, while the current schema requires non-empty trimmed map keys and serialized XML validation rejects XML-illegal text. This proved a public API → persistence contract mismatch.
- The earlier null-metadata-value fix only canonicalized null values, and the completed reserved mapping metadata integrity lane explicitly kept generic metadata revision semantics unchanged; neither lane owned this public key/XML persistability contract.
- Claim-only reservation landed on `main` at `81403a9adb71c9f75bf4aee1496d4f98c54b35cc` before source work.
- Source commit `5b74f81e6b5f9e7404f54f217cdae2d051626347` and regression head `7c688954dc570b735c63fd8403532a3a700a4320` were read back from the agent branch. Compare from the claim commit reported exactly two changed surfaces: `ProjectMetadataDictionary.cs` and the new focused smoke file.
- The public validator uses `XmlConvert.VerifyXmlChars`, so XML-valid whitespace/newline text remains accepted and is preserved exactly. The regression uses C# `\u0001` runtime escapes for invalid key/value cases and asserts metadata count/existing value/project `ChangeVersion`/`UpdatedUtc` remain unchanged after rejection.
- Claim implementation SHAs were recorded on `main` at `efe6641f2ab55338eabc406d5328fbfd9e9ea05e`; integration candidate `df435ee365ff2964c5fdd533919011658beb750e` was built from that refreshed HEAD with implementation head `7c688954dc570b735c63fd8403532a3a700a4320` as additional parent.
- Freeze compare from refreshed main to the integration candidate reported exactly the two reserved source/test files. A final refresh still showed `main` at `efe6641f2ab55338eabc406d5328fbfd9e9ea05e`, so the `main` ref was fast-forwarded to `df435ee365ff2964c5fdd533919011658beb750e` with `force:false`; immediate readback confirmed exact landing SHA.
- On the first post-landing Actions read, no workflow run was yet indexed for exact SHA `df435ee365ff2964c5fdd533919011658beb750e`. No manual dispatch was performed, so this claim does not report managed/cloud CI PASS.
- Licensed/native BricsCAD V25/V26 NETLOAD acceptance was not executed by this remote lane and remains LOCAL_ONLY.

## Completion

The generic metadata public-mutation persistability fix and focused regression source are reachable from `main` at `df435ee365ff2964c5fdd533919011658beb750e`. Claim-first/source/integration protocol is complete, generic metadata remains revision-neutral, reserved mapping ownership and internal hydration boundaries are preserved, no force push/manual CI dispatch was used, and validation limitations are explicit.
