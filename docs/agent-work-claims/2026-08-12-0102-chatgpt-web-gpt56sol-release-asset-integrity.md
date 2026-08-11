# Work claim — V25 release asset integrity before publish

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release-asset-integrity`
- Registered: `2026-08-12T01:02:00+07:00`
- Baseline main SHA: `50ac762364be318d65e046eeb09af5b0f5af0581`
- Priority: owner-requested continue-all review; close a manual release publication fail-open where a draft is published after checking only that expected asset names exist.

## Verified defect

`.github/workflows/release-v25.yml` uploads local ZIP/checksum/(signed manifest), fetches the draft release, and only checks expected names before setting `draft=false`. A truncated/corrupt upload can therefore satisfy the name check and be published even though its bytes differ from the locally qualified artifact.

## Reserved scope

Before publish, bind every expected GitHub release asset to its exact local file by byte length and SHA-256: locate exactly one draft asset by name, compare GitHub-reported `size` with local length, re-download that asset through its GitHub API asset URL using octet-stream Accept, and compare downloaded SHA-256 with local SHA-256. Any mismatch keeps the release draft because publish is not reached. Add a focused auto-discovered static/model preflight and update the V25 release runbook.

## Expected surfaces

- `.github/workflows/release-v25.yml`
- `scripts/preflight-release-asset-integrity.py` (new)
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- actual workflow dispatch/re-run/publication; V26 release lane and V26 workflow; release tag/version/runtime/signing requirements; package/finalizer/manifest bytes; updater/installer; `src/**`; `tests/**`; licensed BricsCAD runtime.

## Validation plan

- Preserve upload list and draft-first publication sequence.
- Build local asset lookup from exactly the files uploaded.
- Require exactly one draft asset with each expected name, remote size equals local size, re-download exact asset API URL and SHA-256 equals local artifact.
- Temporary verification files are cleaned in `finally`.
- Static preflight pins upload -> draft read -> name/size/hash verification -> `draft=false` patch ordering and models exact/truncated/hash-mismatch outcomes.
- No GitHub Actions dispatch/re-run.

## Coordination

The ACTIVE BricsCAD V26 package/release claim requires V25 behavior remain unchanged and primarily reserves V26-only surfaces. This lane changes only V25 release publication verification and does not touch any V26 file.

## Completion condition

A V25 draft release cannot publish merely because asset names exist; uploaded bytes must match local qualified artifacts by size and re-downloaded SHA-256, with regression/docs on `main` and this claim `COMPLETED`.
