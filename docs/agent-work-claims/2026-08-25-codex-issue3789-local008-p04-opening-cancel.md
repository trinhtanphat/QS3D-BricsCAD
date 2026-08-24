# LOCAL-008 P04 — Door/Opening per-prompt physical ESC qualification

- Agent: `codex`
- Date: 2026-08-25 (UTC+7)
- Status: `ACTIVE / PENDING_LOCAL`
- Issue: #3789
- Parent: #74; queue parent #72
- Branch: `agent/codex/issue3789-local008-p04-opening-cancel`
- Baseline: `origin/main@a917e823799ebeaad2f629daecf9a611b6c0231d`
- Execution environment: licensed BricsCAD V25.2.10 on Windows x64

## Scope

Qualify the bounded cancel-before-execute lifecycle for production Door and
WallOpening Direct Draw commands on one exact pushed candidate:

- Quick `QS3DDRAWDOOR` / `QS3DDRAWOPENING`: physical ESC at the first and
  second geometry prompts;
- Advanced `QS3DDRAWDOORADV` / `QS3DDRAWOPENINGADV`: physical ESC at the
  first point, second point, height, sill and boolean-clearance prompts;
- every case returns to an idle editor and leaves no newly created/cached QS3D
  project, sidecar/backup/lock, command-owned source, semantic element,
  generated/native entity, implied selection or project audit/version change.

P01 view/consecutive/cancel and P03 repeated DrawJig V25/V26 are already PASS
and are not rerun. This lane does not claim project-identity drift, Auto Host
success/ambiguity, reference LINE, Ribbon/UI/DPI, save/reopen or broader #74.

## Boundary

This local lane may add only ignored runtime probe material and sanitized
claim/inbox evidence. It will not edit production source. A reproducible source
defect is handed to a distinct remote/source issue and the licensed cell waits
for a new exact pushed fix SHA.

Raw/proprietary runtime output stays under ignored `artifacts/`. DemandLoad,
installed-loader bytes, disposable fixtures and exact owned BricsCAD processes
must be restored/cleaned before a result is published.

## Coordination check

Current `origin/main`, issue #74, open Direct Draw issues/PRs and current
LOCAL-008 claims were inspected before registration. No open child or PR owns
this exact Door/Opening per-prompt physical-ESC no-residue matrix.
