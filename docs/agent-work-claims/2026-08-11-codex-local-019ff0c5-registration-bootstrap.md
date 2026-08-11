# Work claim — agent registration protocol bootstrap

- Status: `COMPLETED`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows agent)
- Registered: `2026-08-11` (Asia/Bangkok)
- Baseline main SHA: `1e94a8bfda5e3d0990794b1f40f5f6002b0207cd`
- Priority: coordination safety requested by the repository owner

## Reserved scope

Bootstrap the repository-wide rule that every agent must publish a Markdown work claim before beginning an implementation lane. Define conflict-safe claim naming, required fields, overlap checks, status transitions, sync requirements, and completion/release behavior.

Expected implementation files after this claim is pushed:

- `AGENTS.md`
- `docs/AGENT-WORK-REGISTRATION.md`
- `docs/agent-work-claims/README.md`
- this claim file for close-out status

## Excluded scope

- No QS3D product source changes.
- No BricsCAD runtime qualification.
- No GitHub Actions dispatch or release operation.
- No edits to unrelated active agent work.

## Coordination

Other agents should not independently implement a competing registration protocol while this claim is `ACTIVE`. They may continue non-overlapping work after checking current claims and the latest `main`.

## Completion condition

The protocol is documented in the canonical agent instructions, this claim is marked `COMPLETED`, and the coherent documentation batch is committed and pushed to current `main` without force.

## Close-out

- Registration commit: `4990eb98c13be8d948a52c997b177e0ef0cb60e6`.
- Protocol implementation SHAs: `0296f6f31e28a598474875805b934edc26c98e60` and CRLF claim-discovery fix `c7dd212d36677a1d2e005becf8709768fe98d6a1`.
- Final pushed protocol SHA: `c7dd212d36677a1d2e005becf8709768fe98d6a1`.
- Implemented surfaces: `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, and `docs/agent-work-claims/README.md`.
- Validation executed: `git diff --check`; LF + CRLF claim discovery; `scripts/preflight-ci-manual-only.py`; `scripts/preflight-product-boundary.py`; remote ancestry/status verification.
- Product source/runtime scope remained unchanged and no GitHub Actions workflow was dispatched.
