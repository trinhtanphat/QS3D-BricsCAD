# Work claim — coordination preflight integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-coordination-preflight-integrity`
- Registered: `2026-08-11T21:24:00+07:00`
- Baseline main SHA: `f373d932fd90faf7355283234da7e711633339d8`
- Priority: owner-requested repository review; harden a completed repository-health lane without overlapping active product/runtime claims.

## Reserved scope

Harden the repository-health regression gate so it fails closed when the canonical generic preflight disappears and verifies that Markdown files named in the `AGENTS.md` mandatory handoff reading order actually exist. Document the added repository-health checks.

## Expected surfaces

- `scripts/preflight-repository-health.py`
- `docs/HEALTH-AND-PREFLIGHT.md`
- this claim file for close-out

## Excluded scope

- Product feature/runtime implementation under `src/` or `tests/`.
- Updater/SemVer, reporting, formula, quantity-settings, wall-junction, Ribbon/Workspace, Direct Draw, native V25, packaging/signing and release lanes.
- Rewriting `AGENTS.md` policy or changing the mandatory reading order itself unless a fresh verified broken pointer is discovered after registration.
- GitHub Actions dispatch/re-run or licensed BricsCAD V25 qualification.

## Validation plan

- Source-review the final Python diff for deterministic fail-closed behavior.
- Verify the script parses successfully.
- Verify all currently referenced Markdown paths in the mandatory handoff reading-order section resolve in the current repository tree.
- Re-read current `main` before integration and preserve concurrent commits.

## Coordination

The earlier `chatgpt-web-gpt56sol-repo-health-docs` claim is `COMPLETED`. Current active product claims observed in recent `main` history are explicitly excluded. This claim is limited to generic repository-health/coordination preflight integrity.

## Completion condition

A coherent implementation commit is pushed to `main`, the claim records the exact implementation SHA and validation performed, and no active neighboring claim is overwritten.
