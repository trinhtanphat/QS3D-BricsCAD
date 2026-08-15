# Work claim — ProjectZoneService XML current-main recovery

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-zone-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:35+07:00`
- Original main baseline: `0bf036c49fa7efdc04745f8a2af57e390d2b8cd7`
- Final production landing: `26f82241be9d1fd2120ad64b0b00382eb19295af`
- Current release-qualified descendant: `858c1c6e2b5e6d40655a9ace93bfa540929569d2`
- Issue: `#1469` — `COMPLETED`
- Final current-main PR: `#1600`
- Superseded ancestry-bloated PR: `#1594`
- Clean integration-only PR: `#1596`
- Historical reviewed PR: `#1470`
- Priority: Core P1 persistence / failure atomicity

## Recovered defect

`ProjectZoneService.Required(...)` accepted XML-illegal UTF-16 after required/trim/length/control validation. With the public `ZoneDefinition` XML-safe boundary, `Update(...)` could otherwise reach `project.Touch()` before the later Zone setter rejected malformed text.

The landed recovery adds only the reviewed `System.Xml` import and `XmlConvert.VerifyXmlChars(...)` guard in `Required(...)`, preserving existing assignment, freshness and reference behavior.

## Exact source / regression evidence

- current production source blob: `5da8ea778b4ff5444543f53c55a8e41032bc2978`;
- focused smoke blob: `d88f53324217773a1a7a60355cd21f549d6dcd32`;
- smoke registration blob: `f3e5444f51e582451bee5a48ed189165ec1f7b1c`;
- production source delta: `+9/-0`;
- XML-invalid Zone id/name rejects on Create before project mutation;
- XML-invalid lookup/name rejects on Update before `project.Touch()` side effects;
- valid supplementary-Unicode Zone id/name survives service Create/Update and canonical QSDB SaveNew/Load exactly.

## Integration history

- #1594 carried the reviewed four-file recovery but became ancestry-bloated after retargeting and was closed without merge;
- #1596 rebuilt the same four reviewed blobs cleanly on the owner integration branch;
- owner batch #1597 reached main before #1596, so the ZoneService source was still absent from main;
- #1600 rebuilt the exact four-file recovery on release-prepared current main and merged at `26f82241be9d1fd2120ad64b0b00382eb19295af`.

## Cloud qualification

Automatic dispatcher #94 / run `31865342158` succeeded and V25 #253 / run `31865395265` published `v0.1.0-preview.10039` from exact release source `393583a2754806410b42ad261d8f18925b11599b` with all source guards, Core build/smoke, V25 reference validation/plugin build, package integrity and publication green.

A later owner integration landing advanced current main to `e90d762d1caf1b75ee5d594106d3b8b239c01103`. Automatic dispatcher #95 / run `31865634872` then succeeded, and V25 #254 / run `31865689617` qualified that exact current-main descendant end-to-end. Release preparation produced `858c1c6e2b5e6d40655a9ace93bfa540929569d2` and published `v0.1.0-preview.10040`; manual policy, generic + all-discovered source guards, Core Release build, deterministic Core smoke, pinned BricsCAD V25 reference validation, V25 plugin build, package/version/checksum/integrity validation, artifact upload and GitHub prerelease publication all passed.

## Boundary

This is source/Core/cloud-build qualification. The cloud workflow explicitly does not execute real BricsCAD NETLOAD/licensed runtime validation, so no LOCAL/runtime PASS is inferred here. No ProjectState, Floor/Family service, ProjectElement, serializer/schema, adapter/native, workflow/release or product-boundary behavior is changed by this closeout document.
