# Work claim — CI manual guard semantic integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ci-manual-guard-semantic-integrity`
- Registered: `2026-08-11T22:12:00+07:00`
- Baseline main SHA: `4f4cc84f3248e94cd6b7a9686d8ce490619b7f83`
- Priority: owner-requested whole-repository review; close a verified fail-open in the manual-only Actions policy gate without changing workflows or dispatching CI.

## Reserved scope

Harden `scripts/preflight-ci-manual-only.py` so a YAML comment or a bypassing OR branch cannot satisfy the repository's claim that every Actions job is independently hard-guarded to `github.event_name == 'workflow_dispatch'`. Preserve the valid current job expressions, including conjunctions such as the release confirmation guard, and add deterministic parser/regression cases. Update the canonical health/preflight documentation only as needed to describe the stricter source-static contract.

## Expected surfaces

- `scripts/preflight-ci-manual-only.py`
- `docs/HEALTH-AND-PREFLIGHT.md`
- this claim file for close-out

## Excluded scope

- `.github/workflows/**` behavior/content changes.
- GitHub Actions dispatch/re-run, release publication, signing, installer or licensed BricsCAD V25 execution.
- Product implementation under `src/` or `tests/`.
- Current export/BBS, Build3D, updater, reporting, quantity, UI/theme, Xref, licensing, project-unit and geometry lanes owned by other agents.

## Validation plan

- Re-fetch current `main` and preserve concurrent commits before each write.
- Parse the exact edited Python source with `ast.parse`.
- Execute the exact edited gate against synthetic workflow fixtures proving: canonical manual equality passes; manual equality plus `&&` confirmation passes; comment-only equality fails; `!=` plus equality in a comment fails; and a top-level `||` bypass fails.
- Re-read the pushed blob from `main` and verify the claim commit remains an ancestor of current `main`.

## Coordination

Recent claim/commit review found active lanes around export preflight, Build3D preflight selection, updater, reporting, quantity/UI/Xref/licensing/project-unit and geometry. None reserves the manual-only CI policy parser or these exact expected surfaces. Historical manual-only CI work predates the current claim and no current `manual-only`/CI-preflight claim was found in recent claim history.

## Completion condition

The verified fail-open is fixed on `main`, regression evidence is recorded, documentation matches the stricter contract, this claim is marked `COMPLETED`, and no workflow is dispatched.