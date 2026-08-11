# Agent work claim — semantic capture context rollback

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: source-safe transactional cleanup when semantic capture bootstraps a brand-new project and the later mutation/regeneration fails.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs`
  - `scripts/preflight-semantic-capture-context-rollback.py`
  - this claim file for close-out
- Problem: validation-before-bootstrap is now enforced, but after valid preflight `GetOrCreate(document)` may still create/cache a brand-new project. If capture mutation or regeneration then throws, `ProjectStateSnapshot` restores project contents but does not remove the newly-created document context, so a failed authoring command can leave an empty QS3D project bound in-session.
- Intended contract:
  - detect whether a QS3D project already existed before authoring bootstrap using read-only project access;
  - preserve existing-project rollback behavior unchanged;
  - if no project existed and post-bootstrap capture fails, restore semantic state and forget the newly-created document project context;
  - cleanup applies to both batch capture and single-snapshot capture;
  - valid successful capture continues to intentionally bootstrap a project;
  - no Ribbon/Quantity/WPF/native geometry/LOCAL_ONLY V25 changes.
- Validation: exact source/diff review plus auto-discovered static preflight. No GitHub Actions under `continue all`.
- Completion condition: failed new-project capture cannot leave cached project state, regression guard exists, claim is `COMPLETED` with exact SHAs.