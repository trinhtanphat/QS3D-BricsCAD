# Work claim — agent registration protocol bootstrap

- Status: `ACTIVE`
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
