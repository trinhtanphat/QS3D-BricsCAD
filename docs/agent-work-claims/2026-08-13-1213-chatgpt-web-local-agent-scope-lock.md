# Local-agent scope policy lock

- Status: COMPLETED
- Agent: ChatGPT Web / GPT-5.6 Sol
- Owner request: restrict the two local agents to work explicitly marked LOCAL_ONLY / local-agent-only; they must not operate GitHub CI or use CI failures as a bug-fix backlog.
- Baseline: current `main` at claim publication time.
- Scope completed: `AGENTS.md`, `CI_POLICY.md`, and this claim closeout only.
- Exclusions preserved: no source/runtime implementation changes, no workflow dispatch/rerun/cancel, no release changes, no automatic trigger changes, no edits to LOCAL_ONLY implementation claims owned by LOCAL-002/LOCAL-003.
- Enforced outcome:
  - local agents may select work only from explicit LOCAL_ONLY/local-agent-only entries, primarily `docs/LOCAL-AGENT-INBOX.md`;
  - local agents must not discover or claim general bugs from CI failures;
  - local agents must not fix unrelated CI failures, source-guard failures, smoke failures, packaging failures, or release failures unless the owner explicitly reassigns that exact item as LOCAL_ONLY;
  - local agents must not dispatch, rerun, cancel, or otherwise operate GitHub Actions unless the owner explicitly designates that specific local agent for that exact CI operation in a separate request;
  - CI failures are information for the owner-designated CI agent, not a local-agent work queue;
  - local qualification scripts/tests needed by an already assigned LOCAL_ONLY item may still run locally, but they do not authorize GitHub Actions or unrelated bug fixing;
  - when there is no compatible LOCAL_ONLY item, the local worker must stop/idle rather than broaden scope.
- Policy commits:
  - `1d1d88f1dfc4c246c54fe5748a6b78d69c99085e` — hard-lock local workers in `AGENTS.md`;
  - `d82c43bab1312e535b6f2d41b7ccd49ca31e922d` — make LOCAL-002/LOCAL-003 non-CI-designated by default in `CI_POLICY.md`.
- Validation: policy text was written directly to `main`; no CI run was authorized or dispatched by this policy-only request.
