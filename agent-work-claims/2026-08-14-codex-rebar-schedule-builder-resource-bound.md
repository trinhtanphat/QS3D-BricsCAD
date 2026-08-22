# Work claim — Rebar schedule builder resource bound

- Status: `COMPLETED`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T15:51:54+07:00`
- Baseline main SHA: `3ccb9c4a2aa93405da8828b9c6fe919fd01aa011`
- Issue: `#81`
- Priority: remote-safe public Core resource correctness

## Verified gap

`RebarScheduleBuilder.Build(IEnumerable<RebarScheduleInput>)` consumes arbitrary caller input and appends every notation-expanded row before aggregate validation, without any output-row ceiling. A lazy or non-terminating sequence of ordinary one-group inputs therefore never reaches validation and grows the result without bound. Counting only inputs would not close the boundary because one compound notation input can expand into multiple schedule rows.

The same BBS/export subsystem already defines a 10,000-row public limit in `RebarCsvExporter`, so applying that existing policy to canonical schedule output does not invent a new capacity contract. No open PR or active exact claim owns this builder boundary.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarSchedule.cs`: reject before adding expanded output row 10,001 while retaining one-pass caller enumeration.
- `tests/QS3D.Core.SmokeTests/RebarScheduleBuilderResourceBoundSmoke.cs`: prove exact-cap acceptance, cap-plus-one rejection, and termination of an infinite source after exactly 10,001 `MoveNext` calls.
- this claim document for closeout only.

## Preserved contracts and exclusions

- Preserve all behavior through 10,000 expanded rows, input/compound ordering, notation parsing, quantity/spacing arithmetic, aggregate validation, and read-only result semantics.
- No benchmark or timing thresholds; no native/UI/LOCAL automation, Browser/current fixture lane, BricsCAD/private data, release/signing, or GitHub Actions changes.
- Validate focused BBS/rebar gates, Core `Release` build, and full Core smoke; report any independent blocker without expanding.

Completion means the bounded source/smoke fix is merged through normal PR, this claim is closed, and the exact merged-main SHA is returned to `/root`.

## Outcome

- Merged source/smoke fix: PR `#1244`, main SHA `666dcdca99fcb4d75a00abae8128854f96049025`.
- Canonical schedule output now accepts up to 10,000 expanded rows and rejects before adding row 10,001. The registered smoke proves exact-cap acceptance, cap-plus-one rejection, and termination of an infinite source after exactly 10,001 `MoveNext` calls.
- Core and smoke-project `Release` builds passed with 0 warnings and 0 errors. Focused BBS fabrication, BBS command-arithmetic, and rebar numeric-safety preflights passed. A direct Core boundary invocation also accepted exactly 10,000 rows and rejected 10,001.
- Full Core smoke reached the independent `ProjectMaterialCatalogSmoke.RenameStalesInheritedConsumerWithPaddedFamilyId` stale fixture, now owned by the separate ACTIVE claim merged in PR `#1243`; no scope expansion was made.
- No native/UI/LOCAL automation, Browser/current fixture, BricsCAD/private data, release/signing, or GitHub Actions surface changed.
