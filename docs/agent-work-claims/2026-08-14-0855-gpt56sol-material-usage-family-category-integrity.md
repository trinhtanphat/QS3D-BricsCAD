# Material Usage Family-category integrity claim
Status: ACTIVE
Agent: gpt56sol-material-usage-family-category-integrity-20260814-0855
Baseline: 38a9e6669986554171b83a3d7fed033aeb9c4bb4
Scope: src/QS3D.Core/Reporting/MaterialUsageSchedule.cs; tests/QS3D.Core.SmokeTests/MaterialUsageFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Goal: fail closed when Material Usage resolves an existing Family whose category differs from the element category, before Family-derived Material/CurtainFrameMaterial/FamilyName can affect schedule rows. Preserve missing-family and matching-family behavior; add focused regression. Family mutation, BQ source, Revision, persistence and host UI are out of scope.
