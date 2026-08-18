# Agent prompt-to-release continuation and reporting contract

This document supplements `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/LOCAL-ONLY-RUNTIME-REPORTING.md`, and `CI_POLICY.md`.

Its purpose is to prevent repeated AI/agent/chat prompts from producing disconnected edits that never become a validated PR, never reach `main`, or are reported as complete before the required release evidence exists.

## Owner intent

A new owner prompt about an existing feature, bug, UI surface, workflow, or release is normally a request to **continue the one canonical GitHub carrier for that work**, not permission to start another independent implementation.

Every agent/chat session must determine the exact lifecycle state of the requested work before mutation and must leave the work in a visible GitHub state before claiming progress.

The lifecycle is not complete merely because code was edited. The following are distinct states and must never be conflated:

```text
edited locally
  != committed
  != pushed branch
  != branch CI green
  != PR ready/open
  != PR CI/protected candidate green
  != merged to main
  != exact-main validated
  != released/published
```

## Mandatory continuation check for every owner prompt

Before changing source, tests, docs, workflows, configuration, or release metadata for a requested behavior:

1. Fetch/read the exact current `origin/main` SHA.
2. Search for a semantically matching open or recently relevant GitHub Issue and determine the stable Lane-Key.
3. Search for the canonical branch for that Lane-Key or equivalent behavior.
4. Search for the canonical open PR and its current head SHA/status.
5. Check the minimum active claim/reservation metadata needed to detect another owner/session.
6. Determine whether the requested behavior already landed on current `main`.
7. Determine whether applicable CI, exact-main validation, packaging, or release work is still pending.
8. When the task is product/release relevant, determine the currently published release/version and whether any published release already contains the exact landed change.

Use semantic behavior, Lane-Key, expected files/symbols and acceptance criteria—not only titles or branch names—to decide whether work is the same lane.

## Existing carrier wins

If the requested work already has a valid canonical Issue/branch/PR carrier:

- continue or update that carrier when this session is its authorized owner;
- do not create a second Issue, branch, implementation or PR for the same lane;
- if another session owns it, stop overlapping mutation as `DUPLICATE_CARRIER / NO MUTATION` unless the owner/coordinator explicitly reassigns it;
- if the carrier is behind `main`, red, queued, stale-looking or incomplete, that does not release ownership;
- if the implementation already landed on current `main`, do not recreate it; inspect current `main` and create a narrowly scoped follow-up lane only for a real remaining gap.

A repeated owner prompt does not reset GitHub state and does not create a new lane automatically.

## No existing carrier

If no equivalent active carrier exists:

1. create or reuse one uniquely identifying GitHub Issue;
2. assign the stable Lane-Key, normally `issue-<number>`;
3. create exactly one dedicated canonical task branch from the latest valid `main` baseline;
4. record scope, exclusions, expected validation and carrier identity;
5. implement all related code/tests/docs on that branch;
6. validate, commit and push real changes to that branch.

An unpushed local edit or chat-only explanation is not a completed task and is not a visible reservation.

## Required delivery sequence

For watched/integration-relevant changes, the normal delivery path is:

```text
owner prompt
  -> current main + semantic Issue/branch/PR collision check
  -> continue existing canonical carrier OR register one new carrier
  -> implement + regression coverage/docs as needed
  -> validate locally/remotely within actual capability
  -> coherent commit(s)
  -> push canonical task branch
  -> exact branch SHA shared CI SUCCESS
  -> refresh current main
  -> reconcile same carrier if main moved
  -> fresh exact branch SHA CI SUCCESS if reconciliation changed the tree
  -> open/update one canonical PR
  -> PR/protected-main checks on the current candidate
  -> owner-authorized merge only
  -> refresh and record exact resulting main SHA
  -> applicable exact-main validation/release pipeline
  -> verify release/publish outcome when release is part of acceptance
  -> identify the exact published version/tag that first contains the change
  -> report exact state using the mandatory form below
```

A watched branch must not use a new PR or draft PR as its first CI attempt. Fix branch failures on the canonical branch until the exact current branch SHA is green, then create/update the PR according to repository policy.

## CI gates are lifecycle dependencies, not idle time

When repository policy says a branch must **wait for CI**, **await CI**, or cannot advance until CI is `SUCCESS`, that wording describes a lifecycle/admission condition. It does **not** instruct an AI agent/chat session to sit idle, repeatedly poll GitHub Actions, or spend the remainder of the prompt doing nothing while a queued/in-progress run executes.

For every queued or running CI gate:

1. Check and record the exact run/status/head SHA once when the current work reaches that gate.
2. In the owner-facing report, make the pending state visually obvious with the mandatory waiting marker, for example `⏳ Branch CI: IN_PROGRESS — run 123 / abc1234` or `⏳ PR checks: QUEUED — candidate abc1234`. Do not write a plain unmarked sentence such as `đang chờ CI`, `waiting for CI`, or `CI is running` as the lifecycle status.
3. Continue other **already authorized, non-overlapping, race-safe work** that does not depend on the pending CI result when useful work exists. Examples include same-lane audit/regression review, current-main/collision review, handoff/report preparation, or another explicitly assigned non-overlapping lane. Pending CI does not grant permission to take over someone else's lane or invent filler work.
4. Do not bypass the gate: do not open a PR before required exact-head branch CI is green, do not claim a pending run passed, and do not manually rerun/dispatch/cancel Actions unless separately authorized.
5. If no other safe authorized work remains, end the current prompt with the exact `⏳` pending status instead of idling or polling indefinitely. A future continuation prompt/session rechecks the run and advances the same canonical carrier from the new evidence.

A normal queued/in-progress CI run is therefore usually an `ACTIVE` lifecycle with a `⏳` gate, not a reason to report the whole task as `BLOCKED`. Use `BLOCKED` only when a real blocker prevents all currently authorized progress.

## Mandatory red-CI self-remediation loop

A red/failed CI result on the **current canonical carrier owned by this session** is an automatic remediation trigger, not a terminal handoff, whenever the session has the repository authorization, tooling, and execution capability to correct the failure safely.

When branch CI, PR/protected checks, integration CI, or another required repository-safe check reaches terminal `FAILURE`, `ERROR`, or an equivalent red state, the owning agent/session must continue the same carrier through this loop without requiring the owner to repeat `fix CI`, `continue`, or `fix lại`:

1. Verify the exact failing run/check, tested SHA or merge candidate, failed job/step, and the smallest useful log/error evidence. Never diagnose a stale run as if it tested the current head.
2. Classify the root cause before editing: current-lane regression; missing/incorrect regression coverage; stale `main`/merge-candidate incompatibility; workflow/build/tooling defect; deterministic environment/configuration problem; transient CI infrastructure/service failure; external secret/credential/service dependency; LOCAL_ONLY/licensed-runtime dependency; or a separately owned lane.
3. For every safely fixable cause inside the current Lane-Key, **fix it automatically on the same canonical branch**. Update implementation, tests, guards, diagnostics, workflow/configuration, or documentation as actually required by the evidence; do not apply a cosmetic workaround that merely hides the failing check.
4. Commit/push the remediation to the same canonical carrier, let the repository's authorized automatic CI validate the new exact SHA/candidate, and repeat diagnosis → fix → push → revalidate until the required checks are green. A second or later red run re-enters the same loop; the first attempted fix is not a stopping point.
5. A PR/protected-check failure is repaired through the PR's canonical source branch and revalidated on the fresh candidate. If `main` moved and strict freshness or compatibility is involved, reconcile the same carrier safely and obtain fresh evidence; do not create a replacement carrier merely because CI is red.
6. Do not ask the owner to manually repair, retry, or reissue authorization for work that this session is already authorized and technically able to perform. `CI red -> inspect -> fix -> revalidate` is the default automatic next action.
7. Manual workflow rerun/dispatch/cancel remains governed by `CI_POLICY.md`. If the correct retry requires a manual CI operation that is not authorized and there is no code/configuration change to make, do **not** manufacture a no-op commit merely to retrigger CI. Record the exact authorization/external blocker instead.
8. If logs prove the failure belongs to another active Lane-Key, a third-party outage, unavailable secret/credential, unsupported runner/platform, or required LOCAL_ONLY/licensed environment, do not violate ownership or invent a fake fix. Capture the smallest evidence, classify the blocker precisely, create/identify a separate follow-up carrier when policy permits, and keep the current task state truthful.
9. While a fixable red CI is being remediated, report it as `❌` at the failed check line but normally keep the overall lifecycle `ACTIVE`; use `BLOCKED` only when no safe authorized remediation remains. Never report a fixable red run as the final state merely because the first CI attempt failed.

The success condition for a repository-safe red-CI loop is fresh green evidence on the exact current SHA/candidate required by the lifecycle gate, not merely a plausible local edit or a manually dismissed failure.

## Merge authorization boundary

Normal prompts such as `fix`, `continue`, `update code`, `commit push git`, `fix CI`, or repeated requests for the same feature authorize work on the canonical task carrier but do not by themselves authorize a write/merge to `main`.

Only explicit owner merge/integration authorization permits the session to merge the named PR/batch/task. Branch protection and required checks must still be satisfied. Never bypass protection merely to finish the prompt.

If merge authorization is absent, the correct endpoint is a validated canonical branch/PR plus an exact report that merge/release remain pending.

## Release completion and version provenance boundary

After an authorized merge, first refresh `main` and record the exact landed SHA.

When the task's acceptance includes packaging, cloud build, publish, tag, release, installer/package artifact, or another release side effect:

- branch CI is not release proof;
- PR CI is not release proof;
- merge success is not release proof;
- an older successful release run is not proof for the newly landed SHA;
- the latest public release is not automatically proof that it contains the change;
- verify the applicable exact-main release pipeline and its artifact/tag/publish result for the landed SHA before reporting `RELEASED`;
- identify the exact release/version/tag that first contains the landed change whenever such a release exists;
- record the release commit/source SHA and enough ancestry/manifest evidence to show that the release actually contains the change.

Release identity is **publication-based, not reservation- or ordinal-based**:

- a preview ordinal/tag reservation may be consumed even when the release workflow later fails or is cancelled and no published GitHub Release is created;
- never report the highest reserved ordinal, attempted tag, dispatcher success, child workflow start/run, or draft release as `Current/latest published release` by itself;
- `Current/latest published release` means the newest **non-draft GitHub Release object whose publication completed successfully**;
- if higher preview ordinals have no such published Release object, walk backward until the most recent actually published Release is found;
- `First release containing this change` is a separate provenance claim and additionally requires evidence that the published release contains the target landed change/SHA.

Every release-relevant per-prompt report must distinguish:

```text
Current/latest published release: <version/tag or none>
First release containing this change: <version/tag | PENDING | NONE/N/A>
Release source/commit: <sha or N/A>
```

If the change is merged to `main` but has not yet appeared in a published release, report `First release containing this change: ⏳ PENDING`; do not name a future version unless it is already formally defined by repository/release metadata.

If the change does not require a product release under `CI_POLICY.md`, report `Release required: ➖ N/A` and `First release containing this change: ➖ N/A` with the reason instead of pretending a release occurred.

Licensed BricsCAD runtime validation remains separate. Remote/source-only agents must follow `docs/LOCAL-ONLY-RUNTIME-REPORTING.md`: once a LOCAL_ONLY gate is parked, do not recheck it remotely **and do not routinely include a LOCAL/runtime status line in owner-facing reports**. Routine local/runtime evidence is reported by compatible local-machine agents when they actually execute or report local work. A remote agent may mention an exact local gate only when the owner explicitly asks about local validation/status or when that evidence is an explicit current acceptance/blocking gate for the request. Only compatible local evidence tied to the exact tested SHA may be reported as `LOCAL_PASS`.

## Mandatory visual status markers

Every lifecycle/status line in the final per-prompt report must begin with one of these markers so the owner can scan the state without interpreting prose:

- `✅` — verified satisfied/successful/reached using current evidence;
- `❌` — verified failed, red, rejected, or a required condition is currently unsatisfied;
- `⏳` — pending, queued, in progress, waiting for an allowed next gate, or an explicitly task-gating `PENDING_LOCAL`;
- `➖` — genuinely not applicable; include the reason when it is not obvious.

Do not use `✅` for assumptions, stale runs, chat-memory claims, or work that merely appears likely to pass. Do not use `❌` for ordinary in-progress work when the correct state is `⏳`. A queued/running CI line **must** use `⏳`; do not omit the marker even when the surrounding prose already says the run is pending.

For yes/no lifecycle questions, make the meaning visually explicit. Examples:

```text
✅ Branch pushed: YES — abc1234
✅ Branch CI: SUCCESS — run 123 / abc1234
⏳ Branch CI: IN_PROGRESS — run 124 / def5678; report the gate and continue other authorized work instead of idling
⏳ PR: not opened yet — required exact-head branch CI is still pending
❌ PR/protected checks: FAILURE — required check core failed; owning agent automatically diagnoses/fixes/revalidates the same carrier
❌ Merged to main: NO — PR not merged
⏳ First release containing this change: PENDING — merged but release pipeline not complete
➖ Local/runtime evidence: N/A — docs-only change (local-agent report or explicitly requested local status only)
```

## Durable owner corrections and owner-facing brevity

When the owner corrects how agents should work, report, continue, merge, release, or communicate, treat a durable correction as repository policy work rather than as a chat-only preference.

- Persist a durable correction in the relevant canonical `.md` policy on the same task carrier when repository policy is the intended source of truth.
- Do not substitute a chat promise such as `từ giờ mình sẽ...`, `from now on I will...`, or a repeated explanation of the workflow for actually updating the policy Markdown.
- After the policy change is recorded, report the concrete repository state/evidence only; do not lecture the owner by restating rules they just supplied.
- Do not assign the owner unnecessary next steps. If the current session is authorized and technically able to perform the next repository lifecycle action, perform or continue that action on the canonical carrier instead of telling the owner to do it.
- Ask the owner for an action or decision only when repository policy genuinely requires owner-only authorization/input that is not already present.
- When no repository action remains, `Next exact action` is `none`; do not append procedural advice, a workflow promise, or instructions telling the owner what to do next.

## Mandatory per-prompt status report

At the end of **every owner prompt that asks an agent/chat session to change, continue, fix, validate, integrate, merge, or release repository work**, report the exact current state in this form. Do not replace it with a generic `done`, `fixed`, or `completed` statement.

```text
<marker> Prompt result: <ACTIVE | DUPLICATE_CARRIER | BRANCH_GREEN | PR_OPEN | PR_GREEN | MERGED_MAIN | RELEASED | BLOCKED | PENDING_LOCAL>
<marker> Issue: #<number> — <title/status>
<marker> Lane-Key: issue-<number>
<marker> Canonical owner/session: <id>
<marker> Canonical branch: <branch or N/A>
<marker> Baseline/current main: <sha used to start/currently reconciled base>
<marker> Latest task commit: <sha(s) or N/A>
<marker> Branch pushed: <YES/NO + exact head SHA>
<marker> Branch CI: <run/job + exact tested SHA + SUCCESS/FAILURE/PENDING/N/A>
<marker> PR: #<number or N/A> — <OPEN/DRAFT/READY/MERGED/CLOSED/NOT_OPENED>
<marker> PR/protected checks: <SUCCESS/FAILURE/PENDING/N/A + exact candidate when known>
<marker> Merged to main: <NO | YES, main@sha>
<marker> Exact-main validation: <run + landed SHA + SUCCESS/FAILURE/PENDING/N/A>
<marker> Release required: <YES | NO/N/A + reason>
<marker> Current/latest published release: <version/tag/none/N/A>
<marker> First release containing this change: <version/tag/PENDING/NONE/N/A>
<marker> Release source/commit: <sha or N/A>
<marker> Release: <run/tag/artifact/deployment + SUCCESS/FAILURE/PENDING/N/A>
<marker> Remaining blocker: <exact blocker or none>
<marker> Next exact action: <one concrete next lifecycle action or none>
```

For a compatible local-machine agent, append the local evidence line when local work is actually in scope or being reported:

```text
<marker> Local/runtime evidence: <LOCAL_PASS | PENDING_LOCAL | exact local failure/blocker | N/A; never infer LOCAL_PASS>
```

For a remote/hybrid/source-only agent, **omit the local/runtime field entirely by default**. This is the required exception to any generic minimum-field wording in `AGENTS.md`. Do not print `LOCAL_ONLY/PARKED` merely to prove awareness of a parked local gate. Mention local status only if the owner explicitly asks for it or the exact local evidence is an explicit current blocker/acceptance requirement; in that exceptional case, report only the exact task-gating state needed for the current request. A parked local gate must not become the overall `Prompt result`, `Remaining blocker`, or reason to withhold an otherwise eligible remote PR/merge unless the prompt's explicit acceptance requires the local evidence before completion/merge.

The report must use real GitHub/CI/release evidence from the current carrier and current published state. Do not fill unknown fields with guessed identifiers, predicted versions, or stale conversation state.

## Conditional `Hướng đề xuất làm việc tiếp theo là gì?` section

Include a dedicated section with the exact heading below **only when a real next repository action remains or an explicit owner decision/input is genuinely required**:

```text
## Hướng đề xuất làm việc tiếp theo là gì?
```

Do not include this section merely to restate the workflow, promise future behavior, or tell the owner to repeat an instruction already authorized.

- State the **required next lifecycle action first**, and make it agree with the `Next exact action` lifecycle field above. Then make the remainder a **program-sized / mega-scope roadmap**, not a short punch list. When the current evidence provides any meaningful adjacent surface to investigate, the default planning target is roughly **6–12 distinct workstreams and 20–50+ concrete work packages**, with no hard maximum. Organize them into execution waves such as immediate stabilization, correctness/data integrity, regression/test expansion, CI/diagnostics/observability, architecture/maintainability, UX/workflow/product consistency, compatibility/migration, performance/security/reliability, documentation/contracts, and packaging/release readiness as relevant. The roadmap should be large enough to represent substantial multi-hour or multi-day follow-up work rather than a handful of tiny chores.
- A mega-scope roadmap is **planning breadth, not blanket implementation authorization**. Every unrelated work package still requires semantic collision checking and, when pursued, its own appropriate Issue/Lane-Key/canonical carrier. Never stretch the current Lane-Key merely to make the recommendation section look large.
- **Do not invent fake defects to reach the large scope.** Where current evidence is insufficient to assert a bug or fix, define an explicit discovery/audit/profiling/validation work package with a target, hypothesis/question, evidence to collect, decision gate, and exit criteria. Convert it into a remediation package only after evidence supports the finding. This lets the roadmap remain genuinely broad and deep without fabricating facts.
- Before writing the roadmap, perform a **relevance-based coverage sweep of the evidence already available for the current task/report** and deliberately search for adjacent risk classes that could expand into independent workstreams. Consider, when implicated: lifecycle/authorization/blockers; correctness/regression/data integrity; tests/CI/diagnostics/observability; architecture/maintainability/technical debt; UX/workflow/product consistency/accessibility; docs/contracts/migration/backward compatibility; packaging/versioning/deployment/release; and performance/security/reliability/resource-use concerns. The recommendation may propose separate evidence-gathering audits for high-value unknowns, but it must distinguish known findings from hypotheses.
- Structure the mega-scope hierarchically so the owner can see both the big picture and executable units: **Program goal -> Workstream -> Work package -> concrete target/action -> dependency -> definition of done -> proof/evidence**. Prefer phased waves and dependency ordering over one flat list of dozens of bullets. Call out which workstreams can run in parallel and which are sequencing gates.
- Prefer breadth across **separate real problems or evidence-gathering questions** rather than repeating several bullets that are different wordings of one root cause. Group findings only when they share the same root cause, target, and remediation path; otherwise keep them distinct so the owner can see the actual problem count and trade-offs.
- Classify and order roadmap items by urgency/value, for example `Required now`, `P0 Stabilization`, `P1 High-value`, `P2 Structural`, and `P3 Optional/Exploratory`. The first item remains the lifecycle action required to advance the current carrier; lower-priority workstreams must not be presented as equal blockers unless current evidence shows that they are.
- Make each recommendation **decision-ready rather than label-only**. For every proposed action, include the decision-critical detail that applies: the problem/finding or question and concrete evidence; severity/impact and affected user/system surface; the exact target/carrier/gate/file/symbol/surface; the concrete work to perform; why it is worth doing now; the expected outcome or definition of done; important dependencies, collision boundaries, risks, compatibility/authorization constraints; and the validation/evidence that would prove success. If a root cause is only a hypothesis, label it explicitly as a hypothesis and state what evidence would confirm or reject it instead of presenting it as fact.
- Prefer an information-dense format such as `Priority/Class — Problem or question & evidence — Impact — Target — Action — Why now — Done when — Constraints/Risks/Dependencies — Proof`, nested under named workstreams. Omit a field only when it genuinely does not apply; do not shorten a recommendation so aggressively that the owner must ask a follow-up merely to understand what would be changed, why it matters, or how success would be judged.
- When the current report exposes multiple concerns, include **adjacent evidence-backed follow-ups** instead of discussing only the immediate CI/merge/release gate. Examples include a regression test gap exposed by the fix, an error-handling/diagnostic gap seen in the same failure, a compatibility or migration risk created by the same contract, a UX inconsistency revealed by the changed flow, or a release/documentation gap required for users to consume the change. Keep each asserted finding grounded in evidence from the current work; use discovery packages for unverified adjacent risks.
- For each workstream, identify meaningful **dependencies and parallelization**: which packages are prerequisites, which can run concurrently on separate non-overlapping Lane-Keys, which require local/runtime evidence, which are gated by CI/release infrastructure, and which should wait until an architecture/product decision lands. A huge scope without sequencing is not decision-ready.
- Include **proof gates and rollback/containment thinking** for high-risk workstreams: exact tests/checks, telemetry or diagnostics, compatibility matrices, data-migration checks, performance/security thresholds, release evidence, and rollback criteria where applicable. The roadmap should explain how the repository proves each wave safe, not merely what code to change.
- If a recommendation is outside the current Lane-Key/scope or would collide with separately owned work, **do not silently expand the current task**. Mark it as a separate follow-up requiring its own collision check and, when implementation is pursued, a separate Issue/Lane-Key/carrier. The recommendation may explain the dependency or sequencing, but current-lane authorization must not be stretched to absorb unrelated work.
- If multiple next items are alternatives rather than sequential lifecycle steps, say so explicitly, state the meaningful trade-off, and identify which option is recommended and why. Do not present mutually exclusive choices as if all must be performed.
- If progress depends on a gate or blocker, name the exact condition first (for example: branch CI `SUCCESS` on the current head, PR protected checks green, exact-main validation, or required LOCAL_ONLY evidence), then state what action follows when that condition is satisfied.
- Distinguish a **required next lifecycle action** from high-value workstreams and optional/exploratory packages. Do not elevate speculative cleanup, generic hardening, or unverified hypotheses into required blockers merely to make the scope large.
- If this session can perform the next lifecycle action under current repository authorization and tooling, continue it on the same canonical carrier rather than asking the owner to repeat an already-authorized instruction. In particular, a fixable red CI result automatically routes back into the mandatory self-remediation loop above.
- Do not use vague wording such as `continue`, `check later`, `wait`, `monitor`, or `do more` without naming the concrete carrier/gate/action, the reason it matters, and the success condition.
- If the lifecycle is terminal and `Next exact action: none`, omit this section entirely. The terminal status report is sufficient.

This section is conditional owner-facing guidance. `Next exact action` remains the compact lifecycle field used for traceability and must stay consistent with it whenever the section is present. The roadmap is intentionally expected to have a **very large planning scope** when follow-up work remains, but its size must come from real findings plus explicitly labeled discovery/validation packages—not fabricated defects. Preserve repository scope/ownership boundaries while making the follow-up program broad, deep, phased, and executable.

## Completion wording rules

Use completion language precisely:

- `BRANCH_GREEN`: implementation is committed/pushed and applicable exact-branch CI is green, but PR/main/release may still be pending.
- `PR_OPEN`: canonical PR exists; do not imply its protected candidate is green unless verified.
- `PR_GREEN`: current PR/protected candidate is green, but it is not merged.
- `MERGED_MAIN`: owner-authorized merge completed and exact current `main` contains the work; release may still be pending.
- `RELEASED`: only when release is required and the applicable exact-main release/publish outcome for the landed SHA is verified successful, including the exact release/version/tag that contains it.
- `PENDING_LOCAL`: use only when required licensed/private/runtime evidence is explicitly the current completion gate for this prompt or is actually pending under a compatible local agent; a merely parked LOCAL_ONLY item is not enough and remote agents do not emit it routinely.
- `DUPLICATE_CARRIER`: another canonical owner/carrier already owns the same lane; no overlapping mutation was performed.

Never say `ALL MERGED TO MAIN`, `released`, `production complete`, or equivalent unless the repository's stricter definitions and evidence requirements are actually satisfied.

## Success criterion for repeated prompts

Repeated prompts about one feature should advance the **same canonical lifecycle** toward completion, not create an expanding collection of disconnected Issues/branches/PRs.

A future agent receiving another prompt for the same function should be able to read GitHub and answer immediately:

1. what Issue/Lane-Key owns it;
2. which one branch/PR is canonical;
3. what exact commit and CI evidence exist;
4. whether it is merged into current `main`;
5. what the current published release is;
6. whether a release is required and, if so, the exact first published version/tag that contains the landed change, or that it is still pending;
7. what single next action remains.

That traceability is part of the deliverable, not optional reporting overhead.