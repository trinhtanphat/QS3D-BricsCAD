# Work claim — V25 release tag target binding

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release-tag-target-binding`
- Registered: `2026-08-12T01:08:00+07:00`
- Baseline main SHA: `17c51afff0e5faaa7dbe9914807bc1b446c541bb`
- Priority: owner-requested continue-all review; close a release race where the workflow checks local tag absence before draft creation but never re-resolves the resulting remote tag to prove it targets the exact qualified `GITHUB_SHA` before assets/publication.

## Reserved scope

Harden `.github/workflows/release-v25.yml` so after creating the draft release, and again immediately before `draft=false`, the exact remote release tag is resolved from `origin` and its peeled commit SHA must equal `GITHUB_SHA`. Handle lightweight and annotated tags deterministically; fail closed on missing/ambiguous refs or git command errors. Extend V25 release publication regression and update release docs.

## Expected surfaces

- `.github/workflows/release-v25.yml`
- `scripts/preflight-release-asset-integrity.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- V26 workflow/lane; tag naming/version semantics; actual release dispatch/publication; asset byte-integrity semantics already completed; package/finalizer/manifest/signing/updater/installer; `src/**`; `tests/**`; licensed V25 runtime.

## Validation plan

- Add a local PowerShell assertion in the publish step using `git ls-remote --tags origin` for exact tag + peeled annotated-tag ref.
- Require exactly one logical resolved commit: peeled `^{}` when present, otherwise the lightweight tag SHA; compare case-insensitively to exact `GITHUB_SHA`.
- Invoke immediately after draft creation before uploads and again after asset byte verification immediately before publish.
- Regression model covers lightweight exact PASS, annotated exact PASS, missing tag FAIL, wrong SHA FAIL and ambiguous duplicate logical refs FAIL.
- No GitHub Actions dispatch/re-run.

## Coordination

The V25 asset-integrity lane is completed. The active V26 release lane reserves V26-only behavior and is not touched. No current V25 tag-target claim was found.

## Completion condition

The V25 release cannot upload/publish artifacts unless its actual remote tag resolves to the exact qualified workflow SHA both after draft creation and immediately before publication, with regression/docs on `main` and this claim `COMPLETED`.
