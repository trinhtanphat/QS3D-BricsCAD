# Material Usage Family-category integrity claim
Status: COMPLETED
Agent: gpt56sol-material-usage-family-category-integrity-20260814-0855
Baseline: 38a9e6669986554171b83a3d7fed033aeb9c4bb4
Scope: src/QS3D.Core/Reporting/MaterialUsageSchedule.cs; tests/QS3D.Core.SmokeTests/MaterialUsageFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Defect: Material Usage could inherit Material/CurtainFrameMaterial/FamilyName from an existing Family whose category differed from the element category, even though Family assignment rejects that relation and model health reports FAMILY_CATEGORY_MISMATCH.
Implemented: claim e1325a68fd3d6af0c311cf81d935adca03e46a77; source guard fde7e001e068fe7f0a7b679a778f5a01f1838096; focused regression 726dcd4e1cda5a56ac64677cacc4614b7b17b913; smoke registration 609a28afc3e62c3c6ef7e43fad8a86ee22b0136a.
Validation: remote diff/readback verified the guard is before Family-derived schedule data; a registry write race returned 409 and was reconciled by re-fetching the current registry before the successful registration. No Actions were dispatched. No dotnet/csc/mcs or licensed BricsCAD runtime is available here, so executable managed/native PASS is not claimed.
Excluded: Family mutation/repair, BQ source, Revision, persistence/schema, MAP/IFC, update/release UI and CAD host behavior.
