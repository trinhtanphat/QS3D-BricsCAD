# Local code capability lock

- Status: COMPLETED
- Agent: ChatGPT Web / GPT-5.6 Sol
- Owner request: local agents may code only work that genuinely requires the local machine and installed/proprietary BricsCAD, AutoCAD, BLT3D, private fixtures, or equivalent local-only resources; general bug fixing belongs to other agents.
- Baseline: `8e755c7d514d6eaa1699a8fa88b94f2c89ae3f43`.
- Scope completed: root `AGENTS.md` policy plus this claim closeout. No `docs/LOCAL-AGENT-INBOX.md` rewrite was necessary because `AGENTS.md` is mandatory reading before the inbox and now supplies the hard eligibility rule for every inbox item.
- Exclusions preserved: no application/source bug fix, no CI operation, no workflow/release change, no modification of another agent's active implementation claim.
- Enforced outcome:
  - LOCAL-002/LOCAL-003 may code only when both conditions are true: the task is explicitly LOCAL_ONLY/local-agent-only, and the implementation itself genuinely needs local-only capability/resources that remote agents cannot access or reproduce from repository source;
  - eligible local coding is limited to the minimum surface that requires installed/proprietary BricsCAD, AutoCAD, BLT3D, private DWG/assets, native UI/runtime state, proprietary SDK/runtime behavior, or equivalent machine-only resources;
  - merely being BricsCAD-related does not make a source fix local-only;
  - ordinary Core/source/test/docs/refactor/source-guard/packaging/CI-failure bug fixes remain for non-local agents even when the defect was first reproduced locally;
  - when local validation exposes a normal bug, the local worker records the smallest sanitized reproduction/evidence and hands the source fix to another agent, then resumes only the LOCAL_ONLY validation/integration after the fix lands;
  - if a task can be coded correctly from GitHub source without opening/using the required local applications/resources, it is not eligible local-agent coding work;
  - local workers still may not dispatch/re-run/cancel GitHub Actions or use CI failures as their backlog.
- Policy commit: `9a11568c25fe30fae148a130737c321b141d10ff`.
- Validation: policy was committed directly to `main`; no CI was authorized or dispatched by this policy change.
