# Agent Work Claim — IFC-02D canonical quantity evidence

- **Agent:** `chatgpt-web-gpt56sol`
- **Registered (Asia/Ho_Chi_Minh):** 2026-08-14 08:20
- **Status:** `COMPLETED`
- **Completed (Asia/Ho_Chi_Minh):** 2026-08-14 08:24
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02D` — per-quantity evidence identity, duplicate collapse, and ambiguity visibility
- **Priority:** P1
- **Baseline main SHA:** `bb27dc0cd65065ae67663ddc32e72cfe26ace220`
- **Dependencies:** completed IFC-02A/IFC-02B/IFC-02C and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed gap

IFC-01 requires supported imported quantity evidence to retain quantity/property key, finite value, unit, external source identity and provenance; exact duplicate evidence must not be counted twice and conflicting duplicate evidence must remain visibly ambiguous. Before this slice, `IfcRoundTripProjection` retained dimensions, one primary quantity/unit and a global provenance list, but it had no per-quantity external source identity and no canonical evidence grouping that could distinguish exact duplicate evidence from conflicting evidence for the same quantity/source identity.

## Reserved scope

- `src/QS3D.Core/Export/IfcRoundTripProjection.cs`
- `src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripQuantityEvidenceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripQuantityEvidenceRegistration.cs`

No existing IFC exchange-result source/test was reserved or modified by this slice.

## IFC-02 claim gate for this bounded slice

- **Schema/version + runtime/library boundary:** schema-neutral CAD-independent Core contract only; no IFC parser/writer, external IFC SDK or BricsCAD native invocation.
- **Supported object subset:** existing canonical `IfcRoundTripProjection` objects when a declared exchange subset includes numeric quantity/property evidence.
- **QS3D-to-external identity relation:** existing IFC-02A projection identities remain authoritative; quantity evidence adds only external evidence/source identity and never fabricates QS3D semantic or rule identity.
- **Canonical projection:** `IfcRoundTripProjection` remains the one comparison projection and now has a backwards-compatible optional quantity-evidence collection rather than a parallel projection/calculation engine.
- **Result states:** IFC-02B/02C exchange states remain unchanged. Evidence conflicts are represented explicitly inside the canonical evidence set.
- **Reuse boundary:** no unit conversion, quantity recomputation, mapping or cost calculation was added; evidence values/units are preserved as declared boundary evidence.
- **Test matrix covered by this bounded contract:** row 6 (quantity value/unit retained), row 7 (non-finite rejected), row 8 (positive zero canonicalization), row 10 (exact duplicate cannot double-count; conflict visible), row 11 (evidence provenance retained), row 15 (deterministic projection/evidence ordering).

## Completed behavior

1. `IfcRoundTripQuantityEvidence` retains canonical quantity key, finite value, unit, external source identity and provenance identity.
2. Numeric signed zero is canonicalized through the existing projection finite-number contract.
3. `IfcRoundTripQuantityEvidenceSet` groups deterministically by quantity key + external source identity.
4. Exact duplicate candidates collapse to one canonical candidate and do not increase `CandidateCount`.
5. Differing candidates for the same quantity/source identity remain in one deterministic group with `IsAmbiguous == true`; no candidate is silently selected as authoritative.
6. The existing `IfcRoundTripProjection` constructor remains source-compatible and yields an empty evidence set when none is declared.
7. A backwards-compatible projection overload retains canonical quantity evidence and `IfcRoundTripProjectionComparer.AreEquivalent()` compares it tolerance-aware while preserving existing dimension/primary-quantity semantics.
8. Self-registering Core smoke covers finite/zero handling, source/provenance retention, exact duplicate collapse, conflicting ambiguity, deterministic order and projection equivalence behavior.

## Implementation and publication

- Claim-first commit: `5e106fd2ce62ed73c474070ce6a3cacbce1384af`
- Evidence contract commit: `fa1f1e761c6934d06b676ee02a3a2cc9f26d1ac2`
- Projection integration commit: `2625e8a82c4cdb6d02d44293b2bab90e907e2c77`
- Focused smoke commit: `9d337ed396857b04f8334d68612068323d0f5862`
- Smoke registration / branch head: `e32cf6f6a834c3f0416aab80b5e55b0eb0f6d0dd`
- Pull request: `#1097` — `feat(ifc): retain canonical quantity evidence`
- Squash merge to `main`: `46a417042a247871c7bd8ed8b51447a04fd825a9`

## Validation actually executed

- Refreshed current `main` before claim, before source work and before publication.
- Verified the existing `IfcRoundTripProjection.cs` blob on current `main` remained the pre-claim blob before PR publication, so no concurrent file collision occurred.
- GitHub compare showed exactly the four claimed files and no unrelated changes.
- Read source and smoke back from the implementation branch after writes.
- Raw PR metadata reported `mergeable: true`, `rebaseable: true`, `mergeable_state: clean` for exact head `e32cf6f6a834c3f0416aab80b5e55b0eb0f6d0dd`.
- Squash merge used the expected-head guard.
- Read the evidence contract and focused smoke back from `main` after merge and confirmed their published blobs.
- This environment has no executable `dotnet`, `csc`, or `mcs`; no managed compile/smoke/runtime PASS is claimed.
- No GitHub Actions workflow was dispatched.

## Remaining gates / non-scope

Native IFC parser/writer/schema/library integration, BricsCAD V25/V26 native qualification, unit conversion implementation, canonical QS3D measurement calculation, mapping/cost business rules, persistence/QSDB, Rebar, Cost/CST, update/release UI and LOCAL qualification remain outside IFC-02D. Any later adapter that consumes ambiguous quantity evidence must map that state visibly rather than silently treating it as lossless supported data.

## Completion record

IFC-02D is complete on `main` at merge commit `46a417042a247871c7bd8ed8b51447a04fd825a9`; this claim-close commit records the completed reservation and releases the scope for subsequent explicitly claimed follow-up work.
