# Work claim — Remaining static-gate fresh validation

- Status: `COMPLETED`
- Phase: `REMOTE_DONE / 783_OF_783_PASS`
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

## Completion record

- Claim-only commit `dff1060b1d3dbca546856d6741b322399d646882` merged through PR `#1111` at `5abb0beaa2f88be31330609704c84282c2bc9774` before the implementation script was read or edited.
- Fresh focused validation confirmed that `preflight-product-boundary.py` and `preflight-wall-junctions.py` passed on the current lineage. The only remaining failure was `preflight-research-implementation-status.py`: the research document correctly emphasized `**not**` in Markdown, while the guard required the unformatted literal `not a live list of missing QS3D code`.
- Implementation commit `99b4ffce170c9339f0fd6c3a113f98cbe9a33087` strips only Markdown bold/strong emphasis markers from the research text before enforcing the existing negative live-backlog sentence. All provenance, advisory-status, evidence-file, workstream, and ownership assertions remain unchanged; documentation and production source remain unchanged.
- PR `#1112` merged the implementation at exact main SHA `907a2e9fa2c0c1282c8361ee8f02b49e7b2687ae`.

Validation executed on the implementation branch based on `5abb0beaa2f88be31330609704c84282c2bc9774`:

- `py -3 -m py_compile scripts/preflight-research-implementation-status.py`: PASS; generated `scripts/__pycache__/` was removed before commit.
- `py -3 scripts/preflight-product-boundary.py`: PASS.
- `py -3 scripts/preflight-research-implementation-status.py`: PASS.
- `py -3 scripts/preflight-wall-junctions.py`: PASS.
- `py -3 scripts/preflight-all.py`: PASS, all `783/783` discovered feature gates.
- `git diff --check`: PASS.

No product build or Core smoke was required for this Python source-shape-only correction. No GitHub Actions, BricsCAD/native runtime, private data, updater implementation, Curtain, or LOCAL-002/003/004 surface was operated. The reservation is released as `COMPLETED`.
