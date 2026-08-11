# Agent work claim — Reference Wall bootstrap rollback

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: source-safe project-context rollback for failed projectless `QS3DDRAWWALLREF` / `QS3DDRAWWALLREFADV` authoring.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/DirectDrawReferenceWallCommands.cs`
  - `scripts/preflight-reference-wall-bootstrap-rollback.py`
  - this claim file for close-out
- Problem: after reference acquisition/prompts, `projectPreview.ResolveForMutation` may bootstrap a project and pass only `ProjectState` into `Execute`. Later source/capture/regeneration/native-build failure erases owned CAD and restores `ProjectState` but leaves that newly-created project context cached.
- Intended contract:
  - preserve `projectPreview.HasProject` as the pre-authoring ownership signal through `Execute`;
  - existing-project failure keeps canonical context;
  - failed projectless authoring performs existing CAD/semantic rollback, then forgets only the project bootstrapped by this command;
  - success keeps intentional bootstrap;
  - PICKFIRST/fallback reference acquisition, Family defaults, prompt behavior, scoped regeneration, generated ownership and UI finalization remain unchanged.
- Non-overlap: the earlier Reference Wall PICKFIRST claim is completed/released; excludes other Direct Draw files, Ribbon, Quantity, updater and LOCAL_ONLY V25 execution.
- Validation: exact diff/current-source review plus focused static preflight; no GitHub Actions under `continue all`.
- Completion condition: failed projectless Reference Wall cannot leave a cached QS3D project and claim closes with exact SHAs.