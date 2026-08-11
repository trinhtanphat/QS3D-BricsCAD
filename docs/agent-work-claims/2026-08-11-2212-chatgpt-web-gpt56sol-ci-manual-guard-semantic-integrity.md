# Work claim — CI manual guard semantic integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ci-manual-guard-semantic-integrity`
- Registered: `2026-08-11T22:12:00+07:00`
- Expanded: `2026-08-11T22:14:00+07:00`
- Completed: `2026-08-11T22:19:00+07:00`
- Baseline main SHA: `4f4cc84f3248e94cd6b7a9686d8ce490619b7f83`
- Priority: owner-requested whole-repository review; close verified fail-open paths in the manual-only Actions policy gate without changing workflows or dispatching CI.

## Reserved scope

Harden `scripts/preflight-ci-manual-only.py` so a YAML comment, negated equality or bypassing OR branch cannot satisfy the repository's claim that every Actions job is independently hard-guarded to `github.event_name == 'workflow_dispatch'`. Preserve the valid current job expressions, including conjunctions such as release confirmation. Also enforce the explicit `inputs.confirm_release == 'RELEASE'` publish guard semantically for both release workflows currently present (`release-v25.yml` and `release-v25-cloud.yml`) instead of protecting only one filename or accepting comment/string decoys.

## Completed changes

- `7d01db7448386d64fbdf838eaffc87ec583f5ba8` — replaced substring-only job guard detection with comment-aware condition extraction and canonical fail-closed guard validation. The manual event equality must be the leading conjunction, `||` bypass expressions and negated/comment decoys are rejected, duplicate/missing job-level `if` expressions fail closed, and deterministic positive/negative parser regressions run inside the preflight.
- The same commit extends release confirmation enforcement to both `release-v25.yml` and `release-v25-cloud.yml`; each `release` job must hard-require `github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'`.
- `b7de7662e7feab6401eb6130946765ced3ddd171` — documented the stricter semantic manual-only/release-confirmation contract in `docs/HEALTH-AND-PREFLIGHT.md`.
- No `.github/workflows/**` file was modified.

## Validation evidence

- Re-fetched the pushed preflight from current `main`; exact blob SHA is `501a27e33b6251a9b1ba0a795400887b0389229d`.
- `ast.parse` succeeds on the exact edited source.
- Reviewed all eight current workflow files. Their job guards use the accepted canonical shapes: ordinary jobs use the manual-event equality and both release workflows use manual-event equality plus the `RELEASE` confirmation conjunction.
- Executed the exact edited preflight with `python -S` against a synthetic eight-workflow fixture matching those current guard shapes: baseline returned `0` and printed PASS.
- Negative fixtures returned non-zero for comment-only equality, `!=` plus equality in a YAML comment, a top-level `||` bypass, and `release-v25-cloud.yml` with confirmation present only as a comment decoy.
- Registration commit `e084a638d582e635fc9aec085909c60809004e08` and expansion commit `ab530fc2b33b2524e0e4e7bd23aa9103b3fa42c3` remained ancestors while `main` advanced rapidly; conflict-safe Contents API writes were used and no force-push occurred.
- GitHub Actions were not dispatched or re-run. Licensed BricsCAD V25/runtime/release behavior was not claimed as qualified.

## Coordination / exclusions respected

No product implementation under `src/` or `tests/`, workflow behavior, release publication, signing, installer or local V25 execution was changed. Concurrent export/BBS, Build3D, updater, reporting, quantity, UI/theme, Xref, licensing, project-unit and geometry work was preserved.

## Result

The manual-only Actions preflight no longer accepts comment, negation or OR-bypass decoys as a hard event guard, and both current release workflows are now regression-protected by the explicit `RELEASE` confirmation requirement. The lane is complete and released.