# V25 Cloud Stale-Source Recovery Implementation Plan

> **For agentic workers:** use the repository's verification and finishing workflow before claiming completion or merging.

**Goal:** Prevent a V25 cloud release from publishing an exact source SHA that has become stale while allowing the canonical dispatcher to re-evaluate protected `main` without ever reassigning a previously reserved preview ordinal.

**Architecture:** Keep release-relevant drift classification in `.github/workflows/release-v25-cloud.yml`. When drift is proven before the first persistent release mutation, the release emits an auditable superseded marker and completes as a successful no-op. The existing `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` already listens for successful completion of the canonical release workflow, resolves that event to current protected `main`, and preserves immutable preview ownership: if the committed ordinal belongs to another source SHA, it performs no reservation/dispatch and requires protected-main `ProductVersion` to advance. Issue #1441 remains the append-only reservation ledger.

**Tech Stack:** GitHub Actions YAML, PowerShell 7, GitHub REST/CLI calls already used by the workflows, Python 3.12 source/preflight guards.

**Spec:** GitHub Issue #6149 (`V25 cloud release: reconcile stale source without stale publication`).

## Global Constraints

- Never publish a V25 release built from a source SHA when protected `main` has release-relevant changes after that SHA.
- Never rebind, recycle, delete, or reinterpret a historical preview reservation/fence to make an ordinal reusable.
- The release workflow must not dispatch its own retry.
- Retry authority stays in the canonical post-integration dispatcher and Issue #1441 ledger.
- Existing ancestry, tag, published-release, version, package-integrity, and exact-source checks remain authoritative.
- Licensed BricsCAD V25/V26 runtime evidence is not fabricated or promoted by this cloud workflow.

---

### Task 1: Lock the stale-source recovery contract

**Files:**
- Modify: `scripts/preflight-v25-cloud-release-stale-source-handoff.py`

- [x] Replace the unsafe same-ordinal handoff expectations with a successful-no-op contract.
- [x] Require `V25_RELEASE_SUPERSEDED`, a GitHub Actions notice, and `exit 0` before `$body = @{`.
- [x] Require the dispatcher to wake only from a successful canonical release `workflow_run` and resolve it to current `main`.
- [x] Require existing immutable reservation/fence ownership logic and forbid rebind tokens.
- [x] Require the committed V25 preview ordinal to be strictly newer than burned ordinal `10307`, with `Version`, `FileVersion`, and `InformationalVersion` bound to the same ordinal.

### Task 2: Advance the burned preview identity

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`

- [x] Verify `10308` is absent from Issue #1441 reservations, Git tag refs, and GitHub releases.
- [x] Advance the committed V25 identity from `0.1.0-preview.10307` to `0.1.0-preview.10308` across all product/file/informational version surfaces.
- [x] Leave historical `10307` ownership unchanged even though its old tag/release is no longer present.

### Task 3: Make pre-mutation stale release completion a safe no-op

**Files:**
- Modify: `.github/workflows/release-v25-cloud.yml`

- [x] Preserve exact `SOURCE_SHA` ancestry and API/fetch identity validation.
- [x] Preserve release-relevant path classification.
- [x] When `git diff --quiet` returns `1` before the first persistent release mutation, emit an auditable warning/notice/summary and `V25_RELEASE_SUPERSEDED source_sha=<old> current_main=<new>`.
- [x] Exit the publish step with status `0` before draft-release creation.
- [x] Preserve malformed ancestry, API/fetch mismatch, git-diff errors, and post-mutation publication races as hard failures.

### Task 4: Verify dispatcher ownership remains immutable

**Files:**
- Production dispatcher change: none expected.

- [x] Confirm successful release workflow completion already triggers the canonical dispatcher.
- [x] Confirm `workflow_run` is rebound to exact current protected `main` for a fresh release decision.
- [x] Confirm a committed ordinal owned by another source exits without reserve/dispatch and explicitly requires `ProductVersion` advance.
- [x] Confirm no same-ordinal handoff/rebind code is introduced.

### Task 5: Exact-head CI, review, and merge

- [ ] Reconcile the canonical PR branch with current protected `main` without dropping Reservation-v2 metadata.
- [ ] Run the aggregate feature source guards and authoritative PR CI on the exact candidate head.
- [ ] Inspect the final PR diff and ensure only the intended release workflow, guard, plan, and V25 version identity changed.
- [ ] Require current exact-head GREEN and mergeability before merge.
- [ ] Merge PR #6150 and verify protected `main` contains the merge result.
- [ ] Inspect immediate post-merge CI/release automation for a new blocking regression; keep licensed runtime status unchanged without real licensed-host evidence.

## Self-Review

- Stale pre-mutation publication remains impossible.
- Successful stale no-op wakes the existing dispatcher instead of recursively dispatching from the release workflow.
- Burned ordinal `10307` is not reused; fresh committed identity `10308` is used only after ledger/tag/release checks showed it was unused.
- Historical reservation/fence rows stay append-only and immutable.
- The change does not weaken package integrity, exact source/tag binding, or licensed-runtime evidence policy.
