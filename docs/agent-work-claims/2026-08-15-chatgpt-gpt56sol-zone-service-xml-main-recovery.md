# Work claim — ProjectZoneService XML current-main recovery

- Status: `READY_FOR_MAIN_INTEGRATION`
- Agent: `chatgpt-gpt56sol-zone-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:35+07:00`
- Original main baseline: `0bf036c49fa7efdc04745f8a2af57e390d2b8cd7`
- Final current-main baseline: `a81bc3e771bdc10c2fbb794b5a9dcb1508ee6e66`
- Issue: `#1469`
- Final main PR: `#1600`
- Clean integration PR: `#1596` / merge `0432871edf25b583f37ecfa5a839fa023fd198dd`
- Superseded ancestry-bloated PR: `#1594`
- Historical reviewed PR: `#1470`
- Final branch: `agent/chatgpt-gpt56sol/zone-service-xml-final-main-20260815`
- Priority: Core P1 persistence / failure atomicity

## Recovered defect

`ProjectZoneService.Required(...)` accepted XML-illegal UTF-16 after required/trim/length/control validation. With the current public `ZoneDefinition` XML-safe boundary, `Update(...)` could call `project.Touch()` before the later Zone setter rejected such text.

The recovery restores only the reviewed `System.Xml` import and `XmlConvert.VerifyXmlChars(...)` guard in `Required(...)` while preserving current assignment/freshness/reference behavior.

## Regression contract

- XML-invalid Zone id/name is rejected on `Create(...)` before project revision, timestamp, active-Zone or collection mutation.
- XML-invalid lookup id/name is rejected on `Update(...)` before `project.Touch()` side effects.
- Valid supplementary-Unicode Zone id/name survives service Create/Update and canonical QSDB SaveNew/Load exactly.
- Smoke registration is additive through a dedicated module initializer; no shared registration list is rewritten.

## Exact integration evidence

- reviewed production source blob: `5da8ea778b4ff5444543f53c55a8e41032bc2978`
- reviewed smoke blob: `d88f53324217773a1a7a60355cd21f549d6dcd32`
- reviewed registration blob: `f3e5444f51e582451bee5a48ed189165ec1f7b1c`
- production source delta: `+9/-0`
- original #1594 was closed after retargeting made its ancestry expand beyond the four-file lane;
- #1596 rebuilt exactly the four reviewed blobs on the owner integration branch and merged there;
- owner batch #1597 reached `main` before #1596, so current main still lacked this ZoneService guard;
- #1600 therefore rebuilds the same reviewed four-file recovery directly on release-prepared current `main@a81bc3e771bdc10c2fbb794b5a9dcb1508ee6e66`.

## Exclusions / close condition

No ProjectState, Floor/Family service, ProjectElement, serializer/schema, adapter/native, workflow/release or product-boundary changes. BricsCAD licensed runtime is not required for this Core-only lane and none is claimed.

Do not mark this claim/issue `COMPLETED` until #1600 is actually reachable from current `main` and the automatic V25 cloud path for the merged exact descendant completes source guards, Core build/smoke, BricsCAD V25 plugin compile, package integrity, and prerelease publication successfully. No manual Actions rerun/dispatch is part of this lane.
