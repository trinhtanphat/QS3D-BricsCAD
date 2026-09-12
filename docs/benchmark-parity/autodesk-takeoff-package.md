# Autodesk Takeoff Package parity

## Drawing evidence source affinity

A package admits drawing quantity evidence only when the evidence belongs to the same logical sheet, revision, and exact drawing source reference as the `DrawingSheet2D` currently admitted to the package.

This closes a provenance gap where a PDF or raster sheet could be replaced while retaining the same logical sheet id and revision string. Evidence extracted from the replaced source must not continue through `Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate` merely because its `SheetId` and `Revision` still match.

The coordinator reports `PKG.STALE_EVIDENCE_SOURCE` as an error and blocks inventory/estimate publication when `TakeoffQuantityEvidence2D.SourceReference` differs from the admitted sheet source. Source-reference comparison is ordinal so provenance identity is not silently normalized.

Existing compatibility rules remain unchanged: orphan evidence uses `PKG.ORPHAN_EVIDENCE`, revision drift uses `PKG.STALE_EVIDENCE_REVISION`, and valid evidence from the exact admitted drawing source continues without migration. Integrations that replace a PDF/image under an unchanged sheet id/revision must re-extract evidence from the replacement source before package readiness can become `Ready`.
