# Agent reservation — issue #5589

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260904t0724-harness-core
Canonical carrier: agent/gpt56sol-20260904t0724-harness-core/issue-5589-qs3d-code-harness-core
Lane-Key: issue-5589
Ownership-Key: qs3d-code.harness-core-v1
Branch: agent/gpt56sol-20260904t0724-harness-core/issue-5589-qs3d-code-harness-core
Expected-Paths: src/QS3D.Core/Agent/Harness/HarnessSession.cs; src/QS3D.Core/Agent/Harness/TaskIntent.cs; src/QS3D.Core/Agent/Harness/TaskRouter.cs; src/QS3D.Core/Agent/Harness/SkillDescriptor.cs; src/QS3D.Core/Agent/Harness/SkillCatalog.cs; src/QS3D.Core/Agent/Harness/SkillRouter.cs; src/QS3D.Core/Agent/Harness/HarnessPermission.cs; src/QS3D.Core/Agent/Harness/HarnessPolicy.cs; src/QS3D.Core/Agent/Harness/HarnessLifecycle.cs; src/QS3D.Core/Agent/Harness/HarnessTraceEvent.cs; src/QS3D.Core/Agent/Harness/HarnessEngine.cs; tests/QS3D.Core.SmokeTests/AgentHarnessCoreSmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs; .agent/claims/5589-gpt56sol-qs3d-code-harness-core.md

Scope: implement Child 1 Harness Core from the merged QS3D Code plan. Deterministic routing, skill selection, permission policy, lifecycle, trace, and initial execution snapshot only. No CLI, host runtime, provider, filesystem, shell, GitHub, or BricsCAD access.
