# Work claim — Remaining static-gate fresh validation

- Status: `ACTIVE`
- Agent: `codex-fix_remaining_static_gates-20260814`
- Registered: `2026-08-14T10:07:00+07:00`
- Baseline main SHA: `cad16e1132ecbd73b007add42ce297dd201af329`
- Priority: fresh exact-main validation of the last three unrelated failures reported by the issue #1099 aggregate run after their independently completed source-shape corrections.

## Reserved scope

Validate the current-main descendants of the completed product-boundary, research-status, and Wall Junction preflight corrections. If a focused gate still fails deterministically, make only the narrowest source-shape correction inside the already identified guard file and preserve the current documentation and production contracts.

## Expected surfaces

- `scripts/preflight-product-boundary.py`
- `scripts/preflight-research-implementation-status.py`
- `scripts/preflight-wall-junctions.py`
- this claim file

The three scripts remain read-only unless a fresh exact-main failure demonstrates that one of the completed corrections is still incomplete.

## Excluded scope

- product, Core, BricsCAD V25/V26, Wall Junction production, updater #1099, Curtain, LOCAL-002/P10/P11, LOCAL-003/004, installer, release, private data, and local/native runtime source or evidence;
- `docs/PRODUCT-BOUNDARY.md`, BLT3D research content, implementation-status content, and all feature documentation except this claim;
- GitHub Actions dispatch, rerun, cancellation, log inspection, or workflow changes.

## Validation plan

- run the three focused Python preflights against a fresh current-main descendant;
- run `scripts/preflight-all.py` and classify any remaining failure without absorbing unrelated lanes;
- run `git diff --check` and, only if a script changes, the relevant source/static checks needed to prove the preserved contract;
- record exact baseline, validation results, implementation commit if any, and final merged SHA.

## Coordination

The predecessor claims `2026-08-14-0932-chatgpt-web-gpt56sol-research-preflight-literal.md`, `2026-08-14-0934-chatgpt-web-gpt56sol-product-boundary-research-marker.md`, and `2026-08-14-0936-chatgpt-web-gpt56sol-wall-junction-preflight-signature.md` are `COMPLETED` and leave fresh aggregate validation pending. This successor does not reopen their broader documentation or production scope. Current ACTIVE/BLOCKED claims were rechecked and do not reserve these three static guard files.

## Completion condition

The three focused gates and a fresh aggregate run are recorded on a current-main descendant; any necessary correction is merged through a focused PR, or the lane closes as a validation-only no-op when all three predecessor fixes already pass. No native/local or CI evidence is claimed.
