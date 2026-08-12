# Work claim — Revision capture XML text integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-capture-xml-text-integrity`
- Registered: `2026-08-12T10:50:00+07:00`
- Baseline main SHA: `4bbbeb52344131d6acb6d1edd99a00fd585bde44`
- Priority: P1 — producer/store compatibility at the revision snapshot capture boundary.

## Confirmed defect

`RevisionService.Capture(ProjectState, string)` validates canonicality and numeric finiteness but can return a `RevisionSnapshot` containing XML-invalid serialized strings (for example a property value containing U+0001). `RevisionSnapshotStore.Save(...)` now rejects those strings during its canonical preflight, so Capture can construct a DTO that the canonical persistence consumer cannot accept.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionCaptureXmlTextIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/RevisionCaptureXmlTextIntegritySmokeRegistration.cs`
- this claim file

## Intended contract

- Reject XML-invalid `revisionId` at Capture as caller input.
- Reject XML-invalid project-derived serialized strings before Capture returns a snapshot.
- Preserve valid Unicode plus existing canonicality, quantity, source-handle, dependency and comparison semantics.
- Do not modify `RevisionSnapshotStore`, XML schema/file I/O, Compare semantics or native code.
- Do not dispatch GitHub Actions and do not claim BricsCAD/native runtime qualification.

## Completion condition

Source fix and focused deterministic Core smoke regression are published on `main`, exact integration SHAs are recorded here, and this claim is closed without overlapping the completed store-side XML lane.
