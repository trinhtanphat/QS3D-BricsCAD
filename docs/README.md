# QS3D documentation map

Use this index to load only the documents needed for the current task. Do not treat every Markdown file as mandatory bootstrap.

## Everyday agent start

| Need | Canonical reference |
| --- | --- |
| Everyday agent lifecycle | [`../AGENTS.md`](../AGENTS.md) |
| Main/merge authorization | [`MAIN-WRITE-AUTHORIZATION.md`](MAIN-WRITE-AUTHORIZATION.md) |
| CI semantics | [`../CI_POLICY.md`](../CI_POLICY.md) |
| Reservation/collision | [`AGENT-RESERVATION-V2.md`](AGENT-RESERVATION-V2.md) |
| Detailed carrier registration | [`AGENT-WORK-REGISTRATION.md`](AGENT-WORK-REGISTRATION.md) |
| Branch/PR CI timing | [`PR-CI-LIFECYCLE.md`](PR-CI-LIFECYCLE.md) |
| Remote vs LOCAL_ONLY | [`REMOTE-AGENT-SCOPE.md`](REMOTE-AGENT-SCOPE.md) |
| MCP/ChatGPT/host automation | [`MCP-CANONICAL-RUNBOOK.md`](MCP-CANONICAL-RUNBOOK.md) |
| Product/hosting boundary | [`PRODUCT-BOUNDARY.md`](PRODUCT-BOUNDARY.md) |
| LOCAL_ONLY queue | [`LOCAL-AGENT-INBOX.md`](LOCAL-AGENT-INBOX.md) |

For a normal prompt, read `AGENTS.md`, current GitHub carrier state and exact current `main`; load the other documents only when that specialist boundary is relevant.

## Product and engineering references

- architecture → [`ARCHITECTURE.md`](ARCHITECTURE.md);
- commands/workflows → [`COMMANDS.md`](COMMANDS.md);
- project setup → [`PROJECT-SETUP.md`](PROJECT-SETUP.md);
- source/data authority → [`SOURCE-OF-TRUTH.md`](SOURCE-OF-TRUTH.md);
- static/preflight detail → [`HEALTH-AND-PREFLIGHT.md`](HEALTH-AND-PREFLIGHT.md);
- V25 qualification/release → current V25 local/release runbooks;
- V26 qualification/release → current V26 local/release runbooks.

## Documentation hygiene

1. One rule should have one canonical source.
2. Prefer links over copying the same lifecycle into multiple files.
3. Root `README.md` is product orientation, not merge/CI authority.
4. `docs/CI.md` and `docs/CI-READINESS.md` are navigation/evidence aids; `CI_POLICY.md` is canonical CI policy.
5. Dated `REVIEW-*`, `AUDIT-*`, `PLAN-*`, `HANDOFF-*` and `agent-work-claims/**` are point-in-time evidence/history unless a current canonical document explicitly promotes them.
6. Historical direct-main/manual-only/stop-before-merge wording never overrides current `AGENTS.md`, `MAIN-WRITE-AUTHORIZATION.md` or `CI_POLICY.md`.
7. Source/static proof is not licensed BricsCAD runtime proof.

## Current task lifecycle summary

```text
prompt
  -> current main + carrier
  -> implement/fix
  -> commit + push
  -> automatic CI / remediation
  -> protected PR checks
  -> merge same task PR when eligible
  -> verify main
  -> close/release reservation
  -> MERGED_MAIN
```

This summary is navigation only; `AGENTS.md` is the everyday contract.