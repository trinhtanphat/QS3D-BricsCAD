# Agent Work Claim — IFC-02A canonical round-trip projection

- **Agent:** `chatgpt-web-gpt56sol`
- **Date (Asia/Ho_Chi_Minh):** 2026-08-13
- **Status:** `COMPLETED`
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02A` — CAD-independent canonical round-trip projection
- **Priority:** P1
- **Dependency:** IFC-01 acceptance contract (`docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`)

## Why this slice

IFC-01 defines the round-trip acceptance contract, but the baseline `main` had no `IFC-02` implementation and no CAD-independent IFC round-trip projection in `QS3D.Core`. This slice establishes the deterministic Core contract needed before a native V25/V26 IFC adapter can map real IFC entities.

## Implemented scope

- `src/QS3D.Core/Export/IfcRoundTripProjection.cs`: canonical stable QS3D identity, IFC global identity, semantic classification, dimensions, primary quantity/unit, provenance, finite numeric enforcement, deterministic ordering, duplicate/malformed failure paths, and tolerance-aware round-trip equivalence.
- `tests/QS3D.Core.SmokeTests/IfcRoundTripProjectionSmoke.cs`: representative beam/column/plate coverage, ordering/tolerance boundaries, exact semantic/unit/provenance checks, and malformed/non-finite/duplicate regressions.
- `tests/QS3D.Core.SmokeTests/IfcRoundTripProjectionRegistration.cs`: self-registers the focused smoke through the existing `ModuleInitializer` convention.

## Coordination and publication

- Claim-first commit on `main`: `1d2f9f936825e8bca4fc3c93a78be15f3cb7338c`.
- Implementation branch: `agent/ifc02a-canonical-roundtrip`.
- Branch source commit: `a19b4efa098399e2328ad1f83c9a0fab972eae60`.
- Branch smoke commit: `673a8cc4781978e1e2946a9896b50eed62e695d5`.
- Branch registration head: `6ee74c1cabcd3d3c47382b44a27d5b8f30de952b`.
- Pull request: `#1090` — `feat(ifc): add canonical round-trip projection`.
- Squash merge on `main`: `d9f74f7aded8a1760834369213c10b546e8a42cb`.
- The merge pinned the expected PR head SHA so an unexpected concurrent head change would have been rejected.

## Validation actually executed

- Re-fetched live `main` and claim state before implementation and again before closeout.
- Rechecked concurrent work repeatedly while `main` advanced; no force update or history rewrite was used.
- GitHub compare before merge showed exactly the three claimed IFC files and no unrelated agent changes.
- PR #1090 was re-read as mergeable with exactly three changed files before squash merge.
- Read back the merged Core source, focused smoke, and registration file from `main` after merge.
- The available execution environment has no `dotnet`, `gh`, or C# compiler, so no managed build/smoke/runtime PASS is claimed.
- No GitHub Actions workflow was dispatched for this lane.

## Explicit non-scope and remaining gates

- No BricsCAD V25/V26 source changes or native `IfcImport` / `IfcExport` invocation/result-state changes.
- No IFC parser/writer dependency, external IFC SDK, QSDB schema/persistence, or geometry-kernel changes.
- IFC-01 and issue #982 native-release gates were not reopened.
- A managed compile/smoke run remains required in an environment with the .NET SDK.
- Native V25/V26 IFC adapter and round-trip runtime qualification remain follow-on IFC work and must be claimed separately.

## Completion condition

Satisfied for this bounded CAD-independent Core slice: the canonical round-trip projection, fail-closed invariants, deterministic ordering, tolerance-aware comparison, focused regression surface, and smoke registration are present on remote `main`; concurrent work was preserved without force-push; unavailable managed/native execution gates remain explicitly unclaimed.
