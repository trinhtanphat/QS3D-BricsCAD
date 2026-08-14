# BQ report family-category integrity claim
Status: ACTIVE
Agent: gpt56sol-quantity-report-family-category-integrity-20260814-0845
Baseline: f38cc3464a11c62df31d50c186012a654b192e1f
Scope: src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs; tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs.
Goal: fail closed when a quantity-report element references an existing Family whose category differs from the element category, before family-derived name/material/note/density can affect BQ grouping or mass. Preserve missing-family behavior and valid inheritance; add focused regression. Family mutation services, Revision, persistence, MAP/IFC and host UI are out of scope.
