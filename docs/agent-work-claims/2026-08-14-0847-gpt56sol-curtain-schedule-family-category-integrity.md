# Curtain Wall schedule Family-category integrity claim
Status: ACTIVE
Agent: gpt56sol-curtain-schedule-family-category-integrity-20260814-0847
Baseline: f910b2809e13e50650fc59775f93904158ec7778
Scope: src/QS3D.Core/Reporting/CurtainWallSchedule.cs; tests/QS3D.Core.SmokeTests/CurtainWallScheduleFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Goal: fail closed when a GlassWall schedule element resolves an existing Family whose category differs from GlassWall, before the wrong Family identity/name can be projected into curtain schedule grouping. Preserve matching-family and missing-family behavior; add focused regression. Curtain layout/detail generation, BQ/Material Usage, persistence and host UI are out of scope.
