# Room Finish schedule Family-category integrity claim
Status: ACTIVE
Agent: gpt56sol-room-finish-family-category-integrity-20260814-0920
Baseline: 8e888ddf371aa7bbd8c7d34e1e1ea84dcb7fef66
Scope: src/QS3D.Core/Reporting/RoomFinishSchedule.cs; tests/QS3D.Core.SmokeTests/RoomFinishFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Goal: fail closed when a room-finish schedule element resolves an existing Family whose category differs from the finish element category, before Family-derived Material/FamilyName can change schedule grouping, unit hints or primary quantities. Preserve room-finish identity validation, matching-family inheritance and missing-family behavior; add focused regression. Room-finish generation/mutation, BQ/Material Usage, persistence and host UI are out of scope.
