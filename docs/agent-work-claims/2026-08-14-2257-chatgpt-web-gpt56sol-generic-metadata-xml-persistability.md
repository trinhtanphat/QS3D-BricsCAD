# Agent work claim — Generic project metadata XML persistability

- Agent: `chatgpt-web-gpt56sol-generic-metadata-xml-persistability`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `5f78adec71f292bf04014283bcb5b7825ef3bbae`
- Claim commit: `81403a9adb71c9f75bf4aee1496d4f98c54b35cc`
- Implementation branch: `agent/chatgpt-web-gpt56sol/generic-metadata-xml-persistability-20260814`
- Source commit: `5b74f81e6b5f9e7404f54f217cdae2d051626347`
- Regression commit / implementation head: `7c688954dc570b735c63fd8403532a3a700a4320`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-generic-metadata-xml-persistability-20260814`
- Priority: Core P1 persistence integrity

## Reserved scope

Fix one confirmed public-mutation persistability gap in generic `ProjectState.Metadata`. QSDB writes every metadata entry as XML attributes and its current schema requires metadata keys to be non-empty and canonical (no leading/trailing whitespace), but the public dictionary currently rejects only null keys and normalizes only null values. Public callers can therefore add blank/padded keys or XML-illegal key/value text that enters project state but is rejected by the canonical Save/schema boundary.

This lane adds fail-before-write validation only to public Add/indexer Set mutations. Accepted generic metadata remains revision-neutral exactly as established by existing project-browser and reserved-mapping ownership semantics.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMetadataDictionary.cs` — public Add/indexer Set require canonical non-empty keys and XML-representable key/value text before reserved-catalog validation or backing mutation; null values remain canonicalized to empty string.
- new focused `tests/QS3D.Core.SmokeTests/ProjectMetadataPersistabilitySmoke.cs` — rejection atomicity, generic revision neutrality, valid whitespace/newline value round-trip.
- this claim file for coordination/closeout evidence.

## Excluded scope

- No broad semantic versioning for generic metadata.
- Reserved `QS3D.Mapping.v1.*` ownership/revision semantics remain unchanged; the completed reserved-mapping metadata lane remains authoritative.
- Internal QSDB/snapshot hydration semantics are not changed; this lane is public mutation only.
- No changes to `QsdbProjectStore`, `QsdbProjectXmlSchemaValidator`, schema/migration, Project Browser business semantics, material catalog encoding, family/element property maps, native adapters, CI/release/signing, or LOCAL_ONLY BricsCAD qualification.
- No manual GitHub Actions dispatch/rerun/cancel.

## Evidence before registration

At baseline `5f78adec71f292bf04014283bcb5b7825ef3bbae`, public `ProjectMetadataDictionary.Add` and indexer Set delegate to `Set(...)`, which checks only null key / duplicate-add and normalizes null value to empty. `QsdbProjectStore.Map(...)` serializes keys and values directly as XML attributes. `QsdbProjectXmlSchemaValidator.ValidateMap(...)` requires each map key to be non-empty and equal to its trimmed form, while serialized XML text validation rejects XML-illegal characters. Thus public metadata such as key `" padded "`, blank key, or value containing `U+0001` can create in-memory state that canonical persistence rejects.

The earlier null-metadata-value fix only canonicalized null values, and the completed reserved mapping metadata integrity lane explicitly kept generic metadata revision semantics unchanged; neither lane reserved this public key/XML persistability contract.

## Implementation evidence before integration

- Source commit `5b74f81e6b5f9e7404f54f217cdae2d051626347` routes only public indexer/Add writes through a validation layer that requires canonical non-empty keys and XML-representable key/value text. Accepted generic values are preserved exactly, including XML-valid whitespace/newline text.
- Regression commit `7c688954dc570b735c63fd8403532a3a700a4320` adds `ProjectMetadataPersistabilitySmoke` covering valid revision-neutral metadata, blank/padded/XML-illegal key rejection, XML-illegal replacement-value rejection, failure atomicity, and QSDB SaveNew→Load exact value round-trip.
- Compare from claim commit to implementation head reports exactly two changed surfaces: `ProjectMetadataDictionary.cs` and the new focused smoke file.
- Source/test were read back from the agent branch. Internal `AddOwned`, `SetPersistenceValue`, and `ReplacePersistenceState` hydration/owned-write paths are unchanged; reserved mapping revision ownership remains in the existing `Set(..., touchReserved)` path.
- No managed/cloud/native PASS is claimed from the agent branch; no manual Actions dispatch was performed.

## Validation plan

- verify claim visibility on current `main` and re-check concurrent ACTIVE/BLOCKED ownership before source work;
- add the smallest public-mutation validation layer while leaving internal persistence hydration paths untouched;
- focused smoke: valid generic entry remains revision-neutral and round-trips exact XML-valid value text; padded/blank/XML-illegal key and XML-illegal replacement value reject before dictionary/value/project timestamp/version mutation;
- read back exact source/test diff, reconcile fresh `main`, final landing with `force:false`, observe automatic CI only, and record actual validation status without manufacturing PASS.

## Completion condition

Claim-first reservation, source + focused regression, fresh-main integration/reconciliation, final `main` ancestry/readback, and truthful CI/runtime boundaries are all recorded; then status becomes `COMPLETED`.
