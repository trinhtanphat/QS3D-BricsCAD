# Agent work claim — Template import confirmation freshness

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DTEMPLATEIMPORT` bind an existing canonical project before user review, fail closed if the project changes during file-load/confirmation, and isolate rollback/UI reporting from committed semantic state.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/TemplateCommands.cs`
  - `scripts/preflight-template-import-freshness.py`
  - this claim file
- Contract:
  - import must require/read the existing project before opening/loading a template;
  - freeze primitive `ProjectId` + `ChangeVersion` before the OpenFileDialog/template load/confirmation boundary;
  - after confirmation, canonical mutation bind must revalidate both values before `Apply`;
  - rollback restoration remains authoritative; palette refresh failure must not be mislabeled as rollback failure;
  - successful import must not be reported as business failure solely because palette/editor finalization fails;
  - template schema/apply semantics, export cancel-first behavior, persistence policy and LOCAL_ONLY V25 qualification are unchanged.
- No GitHub Actions dispatch authorized. No BricsCAD runtime PASS will be claimed from this web session.
