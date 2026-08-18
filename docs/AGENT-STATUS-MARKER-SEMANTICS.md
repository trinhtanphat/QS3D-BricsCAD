# Agent status-marker semantics

Status: **ACTIVE OWNER CORRECTION / MANDATORY INTERPRETATION RULE**  
Owner correction: 2026-08-18 (UTC+7)  
Applies to: AI/agent/chat progress reporting, lifecycle summaries, controller/worker status, and interpretation of owner shorthand such as `X đỏ / V xanh`.

This document supplements `AGENTS.md` and `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`. For the narrow question of what owner-facing progress/status markers mean, this file is authoritative unless the repository owner explicitly gives a product/UI-specific instruction that overrides it for the named task.

## Core rule: status markers are control-plane/reporting semantics by default

When the owner uses progress shorthand such as:

- `X đỏ`, `❌`, `red X`;
- `V xanh`, `✅`, `green V`, `green check`;
- `⏳`, `pending`, `đang làm`, `đang chờ`;

in the context of asking what an agent has completed, what is failing, whether CI is green, whether a PR is merged, whether a release is done, or otherwise tracking ChatGPT/agent work, the wording means **owner-facing status/reporting only**.

Default meanings are:

- `❌` / `X đỏ` — verified failure, not done, rejected, red, or a required condition is currently unsatisfied;
- `✅` / `V xanh` — verified success, done, green, or a required condition is satisfied;
- `⏳` — pending, queued, in progress, or waiting for an allowed next lifecycle gate;
- `➖` — genuinely not applicable.

These markers are **control-plane/reporting symbols**. They are not product requirements.

## Forbidden inference into product/source work

An agent must **not** infer from status-marker wording alone that QS3D needs any of the following:

- a red-X or green-V logo;
- a Ribbon, topbar, menu, Workspace, palette, shell, Validate, command, or status icon;
- new or changed SVG/ICO/PNG/vector/raster artwork;
- branding or application-shell changes;
- production source changes;
- tests/source guards that enforce red-X/green-V product artwork;
- release/package work merely to surface those colors or shapes.

In particular, an owner message such as `X đỏ V xanh đâu?`, `X đỏ / V xanh đâu hết rồi?`, or equivalent wording inside an agent-progress discussion must be interpreted as **“show me the red/green progress markers so I can see what ChatGPT has or has not completed”**, not as “add red-X/green-V artwork to QS3D”.

Do not create an Issue, source branch, code patch, UI asset, test, guard, PR, merge, release, or LOCAL_ONLY runtime task solely from that status shorthand.

## Product/UI interpretation requires explicit product context

Treat red-X/green-V wording as a product/UI requirement only when the owner explicitly anchors it to a product surface or independently supplied product evidence makes that meaning unambiguous.

Examples of explicit product context include wording such as:

- `thêm icon X đỏ/V xanh vào nút Validate`;
- `logo QS3D phải có X đỏ và V xanh`;
- `Ribbon/Workspace/shell icon này phải hiển thị X đỏ/V xanh`;
- an attached product screenshot with a direct instruction to reproduce or change that named product icon/artwork.

Even then, normal product-boundary, clean-room, Lane-Key, CI, protected-main, runtime and release rules still apply.

A nearby conversation about UI does not by itself override this rule when the actual phrase is being used to ask for **agent progress status**. Interpret the immediate request and its grammatical target, not merely adjacent repository nouns.

## Ambiguity must fail closed before source mutation

If the wording could plausibly mean either:

1. ChatGPT/agent progress reporting; or
2. a QS3D product/UI artifact,

then the agent must fail closed on **product/source mutation**.

Before changing source, assets, tests, guards, workflows, packaging or release behavior, require one of these evidence classes:

- explicit owner wording naming the product/UI surface and requested artifact/behavior; or
- independent current-repository evidence showing an already-registered product requirement that is not derived from the ambiguous status phrase itself.

Absent that evidence, keep the meaning in the reporting/control plane and use the mandatory lifecycle markers from `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`.

Do not bootstrap product evidence from an Issue/PR that was itself created by the same mistaken interpretation. Circular agent-generated evidence is not owner intent.

## Historical misinterpretation is not precedent

Repository history may contain Issues, branches, PRs, source code, guards, assets, release notes, or comments that previously interpreted owner-facing `X đỏ / V xanh` status shorthand as QS3D product artwork.

Those historical artifacts **must not be used as precedent** for future owner intent.

When current owner clarification conflicts with an older agent-authored interpretation, the current owner clarification wins for intent. Existing landed product changes are handled deliberately:

- do not blindly revert a mixed-scope PR that also contains independent valid fixes;
- audit the exact changed files/symbols and separate valid behavior from status-shorthand-derived behavior;
- any remediation that changes production source requires its own collision-checked Issue/Lane-Key/carrier and normal CI/protected-main lifecycle;
- do not label an existing product change as owner-requested merely because an earlier agent wrote that claim into an Issue or PR body.

## Pending CI must be reported with exact visible detail

When any required CI/check is queued, pending, or in progress, the owner-facing report must say **which CI is waiting/running and what remains**. A generic line such as `⏳ CI pending`, `đang chờ CI`, `waiting for CI`, or `CI is running` is not sufficient by itself.

For each currently pending CI gate, report the following whenever the evidence/tooling exposes it:

- the CI category/gate, such as branch CI, PR/protected checks, integration CI, exact-main validation, or release validation;
- workflow/run identifier and exact tested head SHA or PR candidate;
- current job name/id;
- current queued/running step name when step-level status is available;
- important relevant jobs/steps already verified successful;
- exact jobs/steps/gates still remaining before the lifecycle may advance;
- whether the next lifecycle action is blocked solely by that pending gate or whether other authorized work can continue.

Use `⏳` for the pending line. If run/job/step detail is not observable through the current connector/tooling, state that observability limitation explicitly instead of inventing an identifier or collapsing the report back to an unexplained generic `CI pending`.

A concise acceptable example is:

```text
⏳ PR/protected checks: IN_PROGRESS — run 6167 / head 30d9eee; preflight ✅; core job 95555190798 currently `Acquire trusted BricsCAD V25 compile references`; remaining: validate V25 refs -> build V25 plugin -> terminal core SUCCESS before merge.
```

The purpose is that the owner can tell at a glance **what is currently running, what already passed, what is still waiting, and which exact condition unlocks the next lifecycle action**.

## Reporting examples

Correct:

```text
❌ Branch CI: FAILURE — core failed on abc1234
⏳ PR: NOT_OPENED — exact-head branch CI is still pending
✅ Merged to main: YES — main@def5678
```

Correct owner interpretation:

```text
Owner: "X đỏ V xanh đâu hết rồi?"
Context: reviewing ChatGPT/GitHub progress
Meaning: show ❌ / ✅ status markers clearly in the agent report
Repository mutation caused by that phrase: none
```

Incorrect:

```text
Owner: "X đỏ V xanh đâu hết rồi?"
Agent: creates a QS3D red-X/green-V shell logo, Workspace mark, Ribbon icon,
       source guard, PR and release without an explicit product-artwork request.
```

## Enforcement in future prompts and schedules

Every interactive or scheduled agent that uses the repository's mandatory status-reporting contract must apply this semantic boundary before turning owner wording into repository requirements.

The status-marker requirement is satisfied by **clear owner-facing reporting**. It is not satisfied by changing QS3D product pixels.

If a later owner instruction explicitly changes this rule, persist that new correction in the applicable canonical Markdown policy using the repository's normal branch/PR process.
