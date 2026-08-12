# Work claim — Revision capture XML text integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-capture-xml-text-integrity`
- Registered: `2026-08-12T10:50:00+07:00`
- Completed: `2026-08-12T10:57:00+07:00`
- Baseline main SHA: `4bbbeb52344131d6acb6d1edd99a00fd585bde44`
- Priority: P1 — producer/store compatibility at the revision snapshot capture boundary.

## Confirmed defect

`RevisionService.Capture(ProjectState, string)` validated canonicality and numeric finiteness but could return a `RevisionSnapshot` containing XML-invalid serialized strings (for example a property value containing U+0001). `RevisionSnapshotStore.Save(...)` rejects those strings during its canonical preflight, so Capture could construct a DTO that the canonical persistence consumer could not accept.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionCaptureXmlTextIntegritySmoke.cs`
- this claim file

## Implemented contract

- XML-invalid `revisionId` now fails at Capture as caller input with `ArgumentException`.
- XML-invalid project-derived snapshot strings now fail closed with `InvalidOperationException` before Capture returns a snapshot.
- Validation covers element/category/relation IDs, property keys/values, quantity keys, source handles and dependencies.
- Valid Unicode is preserved exactly.
- `RevisionSnapshotStore`, XML schema/file I/O and Compare semantics were not modified.

## Evidence

- Claim registration: `ae0f0e8cd3e8864600aa951c2f3b54ba39ded294`
- Source fix: `0ec378d42acdf2d62b031f7dd011040e2914f78f`
- Focused Core smoke regression + ModuleInitializer registration: `3bb91ab0852a47dd79fde309bed18e2bf36f6dac`
- Readback confirmed the source diff adds `XmlConvert.VerifyXmlChars` fail-closed validation only to the Capture producer boundary.
- Readback confirmed the smoke covers invalid revision id plus invalid element/relation/property/quantity/source/dependency text and a valid Unicode control.
- Ancestry verification against live `main` at `794c2c2aeec1ee06332b5dcac080b922200cad49`: source `behind_by=0`; smoke `behind_by=0`.

## Validation boundary

This connector session did not dispatch GitHub Actions and did not execute the full Core smoke executable, a local build, signing/package gates or licensed BricsCAD V25 runtime. No PASS claim is made for those environments.

## Completion condition

`COMPLETED`: the Capture producer now rejects XML-invalid payloads before returning a snapshot, focused regression coverage is published on `main`, exact integration SHAs are recorded, and the completed store-side XML lane remains untouched.
