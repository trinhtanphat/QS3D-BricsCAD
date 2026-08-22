# Work claim — Room Boundary diagnostic source-fingerprint structural integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T11:50:00+07:00`
- Baseline main SHA: `79782b0882f6e144f0549ce143d5364830b87eb4`
- Priority: Core deterministic diagnostic identity integrity

## Reserved scope

- `src/QS3D.Core/Geometry/RoomBoundaryDiagnostics.cs`
- One focused auto-registered Core smoke test for Room Boundary diagnostic source-fingerprint structural framing.

## Confirmed defect

`BoundarySegment.SourceId` trims only outer whitespace, so embedded newlines remain valid input. `RoomBoundaryDiagnosticService.Fingerprint(...)` currently normalizes logical source IDs and serializes them with `string.Join("\n", ...)` before SHA-256. Consequently one logical source ID `A\nB` and two logical source IDs `A`, `B` produce the same preimage, so distinct provenance sets can receive the same `SourceFingerprint` and, for equal geometry, the same `FaceFingerprint`.

## Intended fix

- Keep existing trim/case-insensitive/distinct/order semantics.
- Frame each normalized logical value unambiguously before hashing rather than using a data-valid delimiter.
- Do not reject embedded newlines or broaden source-ID domain rules.
- Preserve existing diagnostic/topology behavior outside fingerprint identity.

## Regression contract

- Distinct source sets whose old newline-joined payloads collide must produce different source fingerprints.
- Logically equivalent source sets under existing trim/case/order normalization must remain fingerprint-equivalent.
- Normal stable input remains deterministic and topology/report counts remain unchanged.

## Excluded scope

- No RoomBoundaryEngine topology rewrite.
- No source-ID domain restriction.
- No UI/runtime/BricsCAD changes.
- No GitHub Actions dispatch or runtime PASS claim.
