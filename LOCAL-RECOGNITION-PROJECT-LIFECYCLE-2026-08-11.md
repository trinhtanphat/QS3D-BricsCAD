# LOCAL_ONLY Recognition project lifecycle qualification

Updated: 2026-08-11 (UTC+7)

This is a supporting execution runbook for the existing `LOCAL-011` item in `docs/LOCAL-AGENT-INBOX.md`. It is **not** a second live queue. `docs/LOCAL-AGENT-INBOX.md` remains authoritative for priority/status, and remote/non-local agents must treat this runtime qualification as `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.

## Source contract under qualification

The current Recognition lifecycle source contract is implemented in:

- `src/QS3D.BricsCAD.V25/ReviewCommands.cs`;
- `scripts/preflight-review-recognition-project-lifecycle.py`.

For `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`, and `QS3DB4D`, usable CAD input must be acquired and the empty-input path must return before unit resolution or project bootstrap. `QS3DB4D` may inspect an existing project only through read-only state while filtering command-generated handles; an existing-project mutation must then rebind canonical state and still match the ProjectId observed during the read-only scan. Creation remains intentional only when no project exists **and** usable CAD source has already been established.

## Exact V25 scenario matrix

Run all scenarios on a clean checkout of the exact candidate SHA with licensed BricsCAD V25 x64. Use disposable drawings/sidecars and sanitized evidence only.

1. **`QS3DRECOGNIZE` empty/cancel selection** — cancel or provide no usable selected CAD source. Verify the command returns before unit resolution/project binding, creates/caches no project or sidecar, does not change semantic state/audit, and does not change CAD objects.
2. **`QS3DRECOGNIZEAUTO` empty/cancel selection** — repeat the same no-bootstrap/no-mutation assertions for automatic apply mode.
3. **`QS3DB4D` no usable current-space source** — use an empty current space or otherwise produce no eligible source. Verify no project bootstrap/cache/sidecar, no semantic/audit mutation, and no CAD mutation.
4. **Existing-project B4D generated-handle filtering** — start with a valid `.qsdb` containing generated ownership. Forget/reload cache, run `QS3DB4D`, and verify generated-owned CAD handles are excluded from Recognition input through read-only project state. Before mutation, prove canonical state rebinds to the same ProjectId observed during the scan.
5. **Existing-project freshness failure** — after B4D has scanned/read the existing project but before mutation binding, replace/remove the sidecar or otherwise replace the canonical project identity. The same-ProjectId guard must fail closed: no recognition mutation, no audit advance, no replacement project creation/cache, and no CAD mutation.
6. **No existing project with valid CAD source** — provide usable CAD source with no QS3D project. Verify project creation is still intentionally allowed, but only after source acquisition succeeds; exactly one project/sidecar is created and normal Recognition/B4D processing continues without duplicate bootstrap.
7. **Generated-only current space** — with an existing project, make all candidate current-space objects command-generated/owned. After generated-handle filtering the usable source set must be empty; verify the command returns without recognition mutation, audit change, replacement project creation, or cross-DWG effects.
8. **Multi-DWG isolation** — keep a second drawing open with a distinct project. Exercise successful and refused paths above in the active drawing and verify neither cache/project/audit nor CAD objects in the other drawing change.

## Evidence required

Record only sanitized evidence tied to the exact tested SHA:

- exact QS3D SHA, Windows build and BricsCAD V25 build;
- command/scenario name and whether an existing project/sidecar was present;
- before/after canonical ProjectId continuity or expected refusal, without exposing private paths;
- before/after project cache presence, `ChangeVersion`, audit count/summary and semantic element counts;
- before/after CAD object counts relevant to the scenario;
- evidence that generated-owned handles were excluded from B4D source input;
- evidence that stale/replaced project identity is refused before mutation and does not create/cache a replacement project;
- proof that the valid no-project source path creates exactly one intentional project only after usable CAD input exists;
- multi-DWG no-cross-mutation result;
- sanitized failure/output notes if a scenario fails.

Do not commit private/customer DWGs, proprietary BricsCAD DLLs, raw private paths, raw Handle lists, credentials, or unsanitized runtime captures.

## Status boundary

Source/static review may establish `REMOTE_DONE` for the lifecycle contract but cannot establish `LOCAL_PASS`. Until a compatible local agent runs the matrix above on an exact current candidate SHA and updates the existing `LOCAL-011` evidence/status, this qualification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
