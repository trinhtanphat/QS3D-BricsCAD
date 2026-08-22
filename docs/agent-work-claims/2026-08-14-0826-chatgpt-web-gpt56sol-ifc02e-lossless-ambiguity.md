# Agent Work Claim — IFC-02E lossless ambiguity integrity

- **Agent:** `chatgpt-web-gpt56sol`
- **Registered (Asia/Ho_Chi_Minh):** 2026-08-14 08:26
- **Status:** `COMPLETED`
- **Completed (Asia/Ho_Chi_Minh):** 2026-08-14 08:30
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02E` — ambiguous quantity evidence cannot report lossless support
- **Priority:** P1
- **Baseline main SHA:** `1fa65c2167d578f496743000e46a58b87039ee52`
- **Dependencies:** completed IFC-02B/IFC-02C/IFC-02D and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed gap

IFC-02D made conflicting per-quantity evidence explicitly visible through `IfcRoundTripProjection.QuantityEvidence.HasAmbiguity`. Before this slice, `IfcRoundTripExchangeResult.IsLosslessSupported` returned `true` solely when `State == Supported`. A caller could therefore construct a supported result around a trusted projection whose quantity evidence was ambiguous and receive `IsLosslessSupported == true`, even though IFC-01 requires conflicting duplicate evidence to remain visible and prevents ambiguous/lossy evidence from masquerading as lossless round-trip support.

## Reserved scope

- `src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultSmoke.cs`

No quantity-evidence implementation file, projection implementation, native adapter or registration file was reserved or modified by this slice.

## IFC-02 claim gate for this bounded slice

- **Schema/version + runtime/library boundary:** schema-neutral CAD-independent Core result semantics only; no parser/writer, IFC SDK or BricsCAD runtime.
- **Supported object subset:** existing IFC-02B exchange results that carry an IFC-02A/02D canonical projection.
- **QS3D-to-external identity relation:** unchanged; the existing trusted projection remains authoritative for supported results.
- **Canonical projection:** unchanged `IfcRoundTripProjection`; this slice only consumes its `QuantityEvidence.HasAmbiguity` signal.
- **Result states:** existing enum values remain unchanged. A `Supported` state can still identify a supported object subset, but `IsLosslessSupported` no longer claims losslessness when its retained quantity evidence is ambiguous. Ambiguity remains visible through a dedicated result-level predicate and the projection evidence groups.
- **Reuse boundary:** no quantity/unit conversion, measurement, mapping or cost rule was introduced.
- **Test matrix covered:** row 10 (conflicting duplicate evidence remains visible), row 12 (no false lossless result when retained evidence is ambiguous), row 15 (deterministic result semantics).

## Completed behavior

1. Added `HasAmbiguousQuantityEvidence`, derived only from the attached canonical projection/evidence set.
2. `IsLosslessSupported` remains true for ordinary `Supported` results with no ambiguous quantity evidence.
3. `IsLosslessSupported` is false for a `Supported` result whose projection retains ambiguous quantity evidence.
4. `SupportedLossy`, `Unmapped`, `Unsupported`, and `InvalidOrAmbiguous` continue to report `IsLosslessSupported == false` under existing semantics.
5. The supplied projection is retained unchanged; conflicting quantity-evidence candidates remain present and visible.
6. Existing constructor validation, identity rules, relation evidence and duplicate external-identity coalescing remain unchanged.
7. Focused smoke proves normal supported behavior remains lossless while a supported result with conflicting quantity evidence exposes ambiguity and cannot masquerade as lossless.

## Implementation and publication

- Claim-first commit: `59cb89014e6621fc36181bfb82059febece0a096`
- Source fix: `0d52652f8eef7b829ebbcb9f2d617257c7c9ea69`
- Focused regression / nullable-safe branch head: `c6fc1102986d31ad765db17751ed0102c6087b5e`
- Pull request: `#1098` — `fix(ifc): prevent ambiguous evidence from reporting lossless`
- Squash merge to `main`: `68a0aabce4f1736b5b762a9cb92b6334fe8bea68`

## Validation actually executed

- Refreshed current `main` before claim, before source write and before publication.
- GitHub compare showed exactly the two claimed files even after `main` advanced by unrelated work.
- Verified both claimed files on `main` still had their pre-claim blob SHAs before PR publication, so no concurrent file collision occurred.
- Performed source/smoke static read-back and corrected a nullable-flow risk in the regression before publication.
- Raw PR metadata reported `mergeable: true`, `rebaseable: true`, `mergeable_state: clean` for exact head `c6fc1102986d31ad765db17751ed0102c6087b5e`.
- Squash merge used the expected-head guard.
- Read source and focused smoke back from `main` after merge and confirmed the ambiguity predicate, lossless guard and retained conflicting evidence regression are present.
- This environment has no executable `dotnet`, `csc`, or `mcs`; no managed compile/smoke/runtime PASS is claimed.
- No GitHub Actions workflow was dispatched.

## Explicit non-scope / remaining gates

No change was made to IFC result enum values, projection/evidence canonicalization, native IFC import/export, unit conversion, measurement formulas, mapping/cost business rules, persistence/QSDB, BricsCAD, Rebar, Cost/CST, Formula, update/release or LOCAL qualification. Native/schema/runtime qualification remains a later explicitly claimed gate.

## Completion record

IFC-02E is complete on `main` at merge commit `68a0aabce4f1736b5b762a9cb92b6334fe8bea68`; this closeout releases the two-file reservation for later explicitly claimed work.
