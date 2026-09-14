# Cubicost component reconstruction verification

This P1 parity boundary sits between drawing/model recognition and the existing Concrete/Formwork quantity workflows. It is intentionally host-neutral and lives in `QS3D.Core`; BricsCAD/AutoCAD adapters remain optional producers of `RecognizedQsComponent` and `QuantityEvidence` data rather than dependencies of the reconstruction contract.

## Contract

`CubicostComponentReconstructionVerifier` requires one unique review decision for every recognized component and rejects orphan/duplicate decisions. Accepted components must not carry correction dimensions. Corrected components use the existing `ComponentReviewDecision` dimension contract. Rejected components are intentionally excluded from quantity publication, while Proposed components fail closed because unreviewed recognition must not become commercial quantity evidence.

A verified reconstruction preserves the original `QuantityEvidence` identity while exposing deterministic effective dimensions. `QuantifyVerified` reuses `CubicostConcreteFormworkDomainOrchestrator`, so Concrete (`m3`) and Formwork (`m2`) quantities continue through the existing same-generation reconciliation, inventory, Estimate, Tender and Procurement infrastructure rather than creating a second quantity engine.

Evidence confidence must be positive and the component source kind must be a defined `ComponentSourceKind`. The verifier does not infer geometry or silently manufacture evidence: drawing/IFC recognition adapters must supply the source reference, revision, method and confidence that make the reconstruction auditable.

## Architecture and compatibility

- Shared Core only: no BricsCAD, AutoCAD, WPF or WinForms dependency.
- Drawing recognition, IFC model recognition and manual reconstruction remain explicit source kinds.
- Quantity evidence remains authoritative through existing domain/publication pipelines; reconstruction geometry is not an independent commercial quantity ledger.
- Existing public Concrete/Formwork APIs remain unchanged; this is an additive verification boundary.

## Validation

Run `python scripts/preflight-cubicost-component-reconstruction.py`, the `QS3D.Core.SmokeTests` project, and repository Shared/Hybrid CI. Licensed BricsCAD host behavior remains LOCAL_ONLY / NO_RESULT until exercised in the licensed host; the reconstruction verifier itself is REMOTE_SAFE.
