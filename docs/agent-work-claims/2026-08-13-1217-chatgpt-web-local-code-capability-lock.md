# Local code capability lock

- Status: ACTIVE
- Agent: ChatGPT Web / GPT-5.6 Sol
- Owner request: local agents may code only work that genuinely requires the local machine and installed/proprietary BricsCAD, AutoCAD, BLT3D, private fixtures, or equivalent local-only resources; general bug fixing belongs to other agents.
- Baseline: `8e755c7d514d6eaa1699a8fa88b94f2c89ae3f43`.
- Scope: `AGENTS.md`, `docs/LOCAL-AGENT-INBOX.md`, and this claim closeout.
- Exclusions: no application/source bug fix, no CI operation, no workflow/release change, no modification of another agent's active implementation claim.
- Required outcome:
  - LOCAL-002/LOCAL-003 code only when the code change itself requires local-only capability/evidence that remote agents cannot reproduce from repository source;
  - local-only examples include code/probes that must be authored against installed BricsCAD/AutoCAD/BLT3D APIs/runtime behavior, private local drawings/assets, machine-specific native/UI behavior, proprietary SDK/runtime state, or local-only integration surfaces;
  - ordinary Core/source/test/docs/refactor/CI-failure bug fixes remain for non-local agents even if the defect was first observed locally;
  - local agents may report a sanitized reproduction/evidence and hand the bug to non-local agents, then resume only the LOCAL_ONLY validation/integration portion after the remote fix lands;
  - if a task can be coded correctly from GitHub source without opening/using the required local applications/resources, it is not eligible local-agent coding work.
- Validation: read back policy from `main`; no CI is authorized by this policy change.
