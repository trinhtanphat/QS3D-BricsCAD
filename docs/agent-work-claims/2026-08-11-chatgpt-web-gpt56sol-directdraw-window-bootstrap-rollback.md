# Agent work claim — Direct Draw Window bootstrap rollback

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: source-safe project-context rollback for failed projectless Window Direct Draw after deferred post-prompt binding.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/DirectDrawWindowCommands.cs`
  - `scripts/preflight-directdraw-window-bootstrap-rollback.py`
  - this claim file for close-out
- Problem: Window correctly defers project binding until prompts finish, but `BindProjectAfterPrompts` can bootstrap a project before `Execute`. `Execute` receives only `ProjectState`, so on later source/capture/Auto Host failure it restores semantic state and erases source but cannot know that this command created the project context.
- Intended contract:
  - preserve the pre-prompt `projectPreview.HasProject` ownership signal through deferred binding into `Execute`;
  - existing-project failure preserves project context;
  - failed projectless Window authoring erases source + restores semantic state, then forgets the project bootstrapped after prompts;
  - cleanup runs before rollback-error aggregation;
  - project freshness, exact-project checks, Auto Host stable-id checks, scoped regeneration and success behavior remain unchanged.
- Non-overlap: excludes P0/P1/Opening/ReferenceWall, Ribbon, Quantity, WPF, updater/license/XData and LOCAL_ONLY V25 execution.
- Validation: exact diff/current-source review plus auto-discovered static preflight; no GitHub Actions under `continue all`.
- Completion condition: failed projectless Window Direct Draw cannot leave cached project state and claim closes with exact SHAs.