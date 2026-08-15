# Work claim — ProjectZoneService XML current-main recovery

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-zone-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:35+07:00`
- Original baseline: `0bf036c49fa7efdc04745f8a2af57e390d2b8cd7`
- Final source landing: PR `#1600` / `26f82241be9d1fd2120ad64b0b00382eb19295af`
- Issue: `#1469` — `COMPLETED`
- Superseded ancestry-bloated PR: `#1594`
- Clean integration-only PR: `#1596`
- Historical reviewed PR: `#1470`
- Priority: Core P1 persistence / failure atomicity

## Recovered defect

`ProjectZoneService.Required(...)` accepted XML-illegal UTF-16 after required/trim/length/control validation. The landed recovery adds the reviewed `System.Xml` import and `XmlConvert.VerifyXmlChars(...)` guard before project mutation while preserving current assignment/freshness/reference behavior.

## Exact source / regression evidence

- production source blob: `5da8ea778b4ff5444543f53c55a8e41032bc2978`;
- focused smoke blob: `d88f53324217773a1a7a60355cd21f549d6dcd32`;
- registration blob: `f3e5444f51e582451bee5a48ed189165ec1f7b1c`;
- production source delta: `+9/-0`;
- invalid Zone id/name rejects on Create before project mutation;
- invalid lookup/name rejects on Update before `project.Touch()` side effects;
- valid supplementary-Unicode Zone id/name survives service Create/Update and canonical QSDB SaveNew/Load exactly.

## Integration history

#1594 carried the reviewed four-file recovery but became ancestry-bloated after retargeting and was closed. #1596 rebuilt the same four blobs cleanly on the owner integration branch. Because owner batch #1597 reached main before #1596, #1600 was created directly from current main and landed the exact four-file recovery at `26f82241be9d1fd2120ad64b0b00382eb19295af`.

## Cloud qualification

- Dispatcher #94 / `31865342158` and V25 #253 / `31865395265`: SUCCESS; published `v0.1.0-preview.10039` from `393583a2754806410b42ad261d8f18925b11599b`.
- After later owner integration advanced main, dispatcher #95 / `31865634872` and V25 #254 / `31865689617`: SUCCESS; published `v0.1.0-preview.10040` from `858c1c6e2b5e6d40655a9ace93bfa540929569d2`.
- Source Reconcile PR #1606 then advanced production source to `2bb380e343aafdf2d64d23f280858aff7b8ab602`. V25 #255 / `31865930599` also completed end-to-end SUCCESS and published `v0.1.0-preview.10041` from exact release source `b6004bf6356bc02687afaa38d4243b3c21a14fff`.

For #255, release preparation, manual-only policy, generic and all-discovered source guards, Core Release build, smoke harness, deterministic Core smoke, pinned BricsCAD V25 reference acquisition/validation, V25 plugin build, package/version binding, checksum/integrity, artifact upload and GitHub prerelease publication all passed.

## Boundary

This is source/Core/cloud-build qualification. The cloud workflow does not execute real BricsCAD NETLOAD/licensed runtime validation, so no LOCAL/runtime PASS is inferred. This closeout document itself changes no source, test, workflow, package or runtime behavior.
