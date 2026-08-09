# Source-of-truth and synchronization policy

| Data | Authoritative source | Derived/cached copies |
|---|---|---|
| CAD geometry | DWG | normalized metrics in QS3D element |
| project / zone / floor / family | `.qsdb` | workspace view-models |
| element semantic metadata | `.qsdb` | property-grid rows |
| host/element relationships | `.qsdb` | dependency graph |
| quantity formulas/rules | core/rule catalog | BQ rows / Excel |
| quantity result | regenerated deterministic value | UI/export |

Rules:

1. Never persist a BricsCAD runtime `ObjectId` as project identity.
2. Handles are validated against the live drawing by Model Health.
3. Derived quantities may be discarded and rebuilt.
4. Editing a Family/property updates semantic state first; CAD generation consumes that state.
5. A missing/changed CAD source is an explicit health issue, never silently accepted.
6. Opening/Door host links use `HostWallId`; wall deductions are recalculated from linked openings.
7. Same DWG + same `.qsdb` + same rule version must produce the same quantity result.
8. Numerical values are stored unrounded; rounding belongs to UI/report output.
