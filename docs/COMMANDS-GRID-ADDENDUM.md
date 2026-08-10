# QS3D Grid command addendum

Updated: 2026-08-10 (UTC+7)

This addendum records Grid/reference commands that advanced after the current large `docs/COMMANDS.md` baseline. Keep it until the next safe latest-blob merge into the canonical command catalog.

- `QS3DGRID` — capture supported existing `LINE` / `ARC` CAD sources as semantic `ElementCategory.Grid` references. The source CAD remains authoritative; this does not generate native Grid bubbles or 3D geometry.
- `QS3DGRIDNUMBER` — assign deterministic semantic Grid labels in the user's explicit per-entity click order. Supports Numeric/Alphabetic sequence, start index, numeric zero-padding, optional prefix/suffix, whole-batch uniqueness validation, project rollback on operation failure, and best-effort post-success UI synchronization. It does not infer spatial order or mutate source CAD.
- `QS3DSYNCSOURCE` — after editing tracked Grid source CAD with native BricsCAD tools, reconcile source-derived semantic state using the authoritative-source workflow; it does not convert Grid into generated native geometry.

Canonical Grid semantics and runtime boundaries are documented in `docs/GRID-WORKFLOW.md`.
