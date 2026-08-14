# Curtain Wall schedule Family-category integrity claim
Status: COMPLETED
Agent: gpt56sol-curtain-schedule-family-category-integrity-20260814-0847
Baseline: f910b2809e13e50650fc59775f93904158ec7778
Scope: src/QS3D.Core/Reporting/CurtainWallSchedule.cs; tests/QS3D.Core.SmokeTests/CurtainWallScheduleFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Implemented: source commit 2964a303d99ad825b7a556d77d31b39bc23f838c rejects a resolved Family whose category differs from the GlassWall element before FamilyName/group projection. Regression commit cfcdaf068f94bb8b158d14ea75b9be80ef8fbd65 covers mismatch fail-closed, valid Family projection/provenance and preserved missing-Family fallback. Registration commit e3a736f1bfc3ca7e78a742981076947c9a92824e adds the dedicated smoke to RunAll.
Validation: remote source/registry diffs verified; e3a736f1bfc3ca7e78a742981076947c9a92824e remains an ancestor of live main 01215685c429e54f657735be7bfc79f526aee53c (ahead 1 / behind 0), with the intervening workflow change outside this scope. GitHub combined status has no attached statuses. The available container has no dotnet SDK, so managed smoke/build and licensed BricsCAD runtime were not executed and are not claimed PASS.
Excluded scope preserved: Curtain layout/detail generation, BQ/Material Usage, persistence and host UI.
