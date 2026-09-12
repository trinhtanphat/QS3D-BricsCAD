# Protection-Safe Automatic V25 Version PR Design

Date: 2026-09-12
Status: Approved in chat; awaiting written-spec review
Reservation: Issue #6522

## Context

The V25 automatic dispatcher now correctly derives its preview series from the committed ProductVersion, recognizes the latest published release as its batch baseline, and publishes an admitted committed identity without reusing a burned ordinal. The remaining gap appears after a release is published: protected `main` still carries that just-published ProductVersion. Once the next batch becomes eligible, the dispatcher must fail closed because it cannot reserve or publish an identity that has not first been committed to protected source.

Historical automation solved this by creating a release-preparation commit and pushing it directly to `main`. Issue #1800 intentionally removed that behavior. The repository contract now requires release preparation to remain workspace-only and forbids release workflows from committing or pushing directly to protected `main`. This design preserves that protection boundary.

## Goals

- Make the V25 preview pipeline operationally full-auto after the batch threshold is reached.
- Preserve protected-main review, Shared CI, Reservation-v2, and native auto-merge.
- Select a monotonically safe next preview identity without reusing published, tagged, reserved, fenced, or otherwise burned ordinals.
- Make repeated or concurrent dispatcher executions idempotent.
- Keep the existing exact-source reservation/fence/release workflow unchanged once a committed unpublished identity exists.
- Never force-push, bypass branch protection, or write a release-version commit directly to `main`.

## Non-goals

- Do not change the commercial V25 release workflow or V26 public-release policy.
- Do not auto-bump versions immediately after every successful release when the next batch is not eligible.
- Do not weaken exact ProductVersion/tag/package identity checks.
- Do not replace the existing release reservation/fence ledger on Issue #1441.
- Do not add a second publication lane or dispatch releases from an unmerged version branch.
## Dispatcher State Machine

The existing dispatcher remains the single decision point after debounce, current-main admission, published-baseline discovery, and batch evaluation.

1. If the batch is not eligible, exit successfully with no reservation, issue, branch, PR, or release dispatch.
2. If the batch is eligible and the committed ProductVersion is an unpublished, untagged identity with no conflicting prior reservation/fence owner, continue through the existing reservation/fence/final-main admission and release dispatch path. An exact retry reservation/fence already owned by the same admissible source remains on the existing safe-retry path.
3. If the batch is eligible and the committed ProductVersion is not newer than the latest published matching-series preview, enter the automatic version-PR preparation path instead of silently starving.
4. If the batch is eligible but the committed identity has become unusable because its tag/release already exists or its reservation/fence belongs to another source, enter the same automatic version-PR preparation path and select a later free identity. This covers an ordinal that becomes burned while an earlier version PR is pending or after it merges.
5. If a canonical automatic version PR already exists for the same intended next identity, report it and exit successfully without creating another issue, branch, or PR.
6. If a prior preparation attempt left a valid open Reservation-v2 Issue but did not finish branch/PR creation, resume that exact reservation rather than allocate a second one.

A successful version-PR merge modifies the three product project files under `src/**`, so the resulting protected-main push naturally re-enters the dispatcher. The dispatcher then observes the same published baseline and eligible batch, but now sees a committed unpublished identity and follows the existing release path.

## Next Preview Identity

The next identity is derived only after the batch is READY. The series prefix comes from the current canonical V25 ProductVersion, not a hard-coded release family.

The allocator must inspect all bounded authoritative evidence for that matching series:

- published GitHub Releases and their tags;
- repository Git tags;
- valid V25 preview reservation comments on Issue #1441;
- valid V25 preview dispatch-fence comments on Issue #1441;
- the currently committed ProductVersion.

The candidate starts strictly above the greater of the latest published ordinal and the currently committed ordinal. From there, the allocator selects the smallest canonical positive integer absent from every matching-series Git tag, published release, reservation, and dispatch fence; any occupied or burned candidate is skipped monotonically. Malformed matching-series evidence, conflicting ownership, numeric overflow, leading-zero identities, or incomplete bounded enumeration fail closed. The allocator never repairs, deletes, retags, or reassigns historical state.

Example: with published `.6`, committed `.6`, and an old burned reservation/fence for `.7`, the generated PR must prepare `.8`, not `.7`.
## Reservation-v2 and Generated PR

The automatic preparation path must create a normal, visible Reservation-v2 carrier before source mutation. It must not use a hidden workflow-only lock as a substitute for repository ownership.

The generated Issue uses a stable automation owner such as `qs3d-release-automation` and records:

- `Lane-Key: issue-<number>`;
- `Reservation-Protocol: v2`;
- a canonical owner/session bound to the automation owner;
- `Canonical carrier: agent/qs3d-release-automation/issue-<number>-v25-preview-<ordinal>`;
- an Ownership-Key scoped to the V25 preview identity series;
- Expected-Paths containing only the three aligned project files.

The generated branch starts from an exact, freshly rebound protected-main SHA. One atomic commit changes only:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`;
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`;
- `src/QS3D.Core/QS3D.Core.csproj`.

All three projects receive the same canonical ProductVersion and matching numeric FileVersion. Stable AssemblyVersion behavior remains unchanged. The commit message is deterministic, for example `chore(release): prepare v0.2.0-preview.8`.

The PR targets `main`, is non-draft, includes `Lane-Key: issue-<number>` and `Closes #<number>`, and does not opt out of automerge. It is intentionally an ordinary same-repository PR so Reservation-v2, Shared CI, coordinator reconciliation, and native GitHub auto-merge all remain authoritative.
## Authentication and Event Semantics

Automatic repository mutations must use `QS3D_AUTOMERGE_TOKEN`, the same repository secret the Hybrid PR Coordinator requires for mutations that must emit ordinary synchronize/CI events. `github.token` is not an acceptable fallback for issue/branch/commit/PR creation because workflow-created events may be suppressed from starting the required downstream workflows.

If `QS3D_AUTOMERGE_TOKEN` is absent or cannot perform the required same-repository mutations, the preparation path fails red before source mutation. It never falls back to direct-main writes or branch-protection bypass.

The workflow must avoid printing the token, embedding it in commit content, or leaving it in persisted Git configuration. Repository API calls remain limited to this repository and the exact automation carrier.

## Atomic Branch Commit

The preferred implementation uses GitHub Git Data APIs through authenticated `gh api` calls to create one atomic tree/commit on the generated branch. This avoids persisting push credentials and avoids a sequence of partial Contents-API commits.

The sequence is:

1. Re-read protected `main` and require it to equal the admitted preparation base immediately before durable mutation.
2. Create the canonical automation branch ref at that exact main SHA.
3. Materialize three replacement blobs containing only the validated version changes.
4. Create one tree using the exact main tree as its base.
5. Create one commit with the exact admitted main commit as its sole parent.
6. Update only the generated branch ref from the admitted base to that commit.
7. Read the branch commit back and verify its parent, changed-path set, version identity, and clean three-file diff before opening the PR.

If branch creation races with an already-existing canonical branch, the workflow reconciles it as an idempotency case; it does not overwrite an unknown branch head.
## Idempotency and Recovery

The preparation transaction uses the target preview identity as a deterministic automation key. Before creating a new Issue it searches bounded open Reservation-v2 Issues and open same-repository PRs for that exact automation key and validates their metadata rather than trusting titles alone.

A valid existing open Issue with no PR is resumable. A valid existing PR for the same identity is authoritative and causes a no-op. Multiple valid carriers for the same target identity are an error and fail closed. A malformed carrier that visibly claims the same identity is not silently ignored; it requires reconciliation before new mutation.

The dispatcher concurrency group remains serialized, but idempotency does not depend solely on Actions serialization. Every durable transition rechecks repository state. If `main` advances before the first mutation, the run exits or retries from a fresh decision without creating stale version state. If `main` advances after the generated PR exists, the Hybrid PR Coordinator owns rebase-first reconciliation and emits fresh Shared CI for the new PR head.

Issue creation is the first durable preparation-side mutation. Therefore a failure after Issue creation remains visible and resumable. The workflow never hides partial state in runner-local files.

## Interaction with Existing Release Safety

The generated PR does not reserve the preview ordinal in the release ledger and does not dispatch a release. It only commits the next source identity through the protected PR path.

After merge, the existing dispatcher remains responsible for the checks below. If the freshly committed identity became unavailable while its PR was in flight, the dispatcher routes back to automatic version-PR preparation for the next free ordinal instead of reserving or publishing the stale identity.

The existing dispatcher remains responsible for:

- proving the batch is still READY against the published baseline;
- proving the committed tag is still unpublished and untagged;
- validating reservation/fence ownership on Issue #1441;
- rebinding protected main before the first durable release side effect;
- persisting reservation and dispatch fence;
- dispatching `release-v25-cloud.yml` for the exact admitted source SHA.

This separation prevents an unmerged version PR from owning or publishing a release and preserves the current exact-source publication contract.
## Failure Handling and Security

Malformed release history, exhausted bounded enumeration, conflicting ledger ownership, invalid SemVer, unavailable mutation token, API ambiguity, unexpected changed paths, stale main admission, or inconsistent branch readback all fail closed. No case authorizes a direct protected-main write.

The generated branch is same-repository only. No fork event can request automatic version preparation. The automation branch commit may contain only the three declared project files, and the PR must pass Reservation-v2 before Shared CI is trusted for integration.

A failed or cancelled generated PR does not cause the dispatcher to manufacture a replacement identity automatically while its valid Reservation-v2 Issue or canonical carrier remains unresolved. Historical tags, releases, reservations, and fences are immutable inputs.

## Verification Strategy

Implementation must begin with failing regression coverage for the new behavior and keep the existing release regressions green. Required deterministic cases include:

- batch below threshold creates no preparation state;
- batch READY with committed version newer than published follows the existing release path unchanged;
- batch READY with a newer committed identity that is already tagged or owned by another source requests a replacement version PR;
- batch READY with committed version equal to published requests one version PR;
- a burned next ordinal is skipped;
- malformed or conflicting ordinal evidence fails closed;
- an existing valid Issue/PR is resumed instead of duplicated;
- duplicate/malformed carriers fail closed;
- generated Reservation-v2 metadata and canonical branch naming pass the repository parser;
- generated commit changes exactly the three project files and keeps their identities aligned;
- the workflow requires `QS3D_AUTOMERGE_TOKEN` and contains no direct-main commit/push primitive;
- main drift before durable mutation cannot create a stale preparation branch.

Focused tests must be followed by `git diff --check`, changed-script syntax/parse checks, relevant release/dispatcher/Reservation-v2 preflights, and the aggregate feature preflight. The implementation PR must then pass exact-head Shared CI and merge only through normal coordinator/native-auto-merge policy.

## End-to-End Acceptance

After integration, a real dispatcher run is observed rather than artificially forcing a release. If the current batch is below threshold, the correct result is no version PR. When a natural batch reaches the threshold, acceptance requires exactly one generated Reservation-v2 version PR, Shared CI on its exact head, normal merge, and then the existing dispatcher/release path for the merged committed identity. No duplicate tag, release, issue, branch, PR, reservation, or dispatch fence may be produced.