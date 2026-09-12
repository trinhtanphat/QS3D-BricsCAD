# Protection-Safe Automatic V25 Version PR Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When the natural V25 batch is READY but protected `main` does not carry a usable unpublished preview identity, create exactly one protected-branch Reservation-v2 version PR instead of starving or writing directly to `main`.

**Architecture:** Keep the existing dispatcher authoritative for batching and exact-source release publication. Add `scripts/v25-auto-version-pr.py` as a deterministic allocator/rendering/mutation helper: pure functions classify identity state and choose a next-free ordinal; a `prepare` command performs bounded GitHub API reconciliation and creates/resumes one Reservation-v2 Issue, branch, atomic three-file commit, and PR. The workflow calls this helper only after the batch is READY and again on late tag/owner races, using `QS3D_AUTOMERGE_TOKEN` only for preparation mutations.

**Tech Stack:** GitHub Actions YAML, Bash, Python 3 stdlib, `gh api`, GitHub Git Data API, existing Reservation-v2 parser/gates.

**Spec:** `docs/superpowers/specs/2026-09-12-v25-auto-version-pr-design.md`

## Global Constraints

- Never commit or push a version change directly to protected `main`.
- Keep Issue #1441 release reservation/fence formats and exact-source release dispatch semantics unchanged.
- The natural batch threshold stays `MINIMUM_RELEASE_CHANGES=10`; no eager post-release bump below threshold.
- `QS3D_AUTOMERGE_TOKEN` is mandatory only for the preparation mutation path; `github.token` remains sufficient for read-only inspection and existing release-ledger writes.
- Generated version PRs change exactly `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`, `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`, and `src/QS3D.Core/QS3D.Core.csproj`.
- `Version` and `InformationalVersion` are identical canonical `X.Y.Z-preview.N`; `FileVersion` is `X.Y.Z.N`; `AssemblyVersion` is unchanged.
- Preview ordinals are canonical positive decimal integers in `1..65535`; malformed, ambiguous, incomplete, or conflicting evidence fails closed.
- A generated PR must pass Reservation-v2, Shared CI, and normal Hybrid Coordinator/native auto-merge. No force/bypass/direct merge endpoint.

---## File Structure

- Create `scripts/v25-auto-version-pr.py`: canonical preview parsing, bounded evidence model, next-free allocation, project rendering, Reservation-v2 carrier reconciliation, Git Data API branch commit, PR creation/resume.
- Create `scripts/preflight-v25-auto-version-pr.py`: executable regression tests for allocator/rendering/carrier metadata plus static workflow/helper safety guards.
- Modify `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`: route READY-but-unusable identities to the helper and pass a mutation token only to that path.
- Modify `scripts/preflight-v25-dispatch-committed-version.py`: replace the old neutral-starvation assertions with prepare-PR routing assertions while retaining exact release ownership checks.
- Modify `scripts/preflight-v25-preview-dispatch-idempotence.py`: model prior-owner state as preparation-required rather than terminal neutral starvation.
- Modify `scripts/preflight-release-preview-sequence.py`, `scripts/preflight-v25-cloud-release-stale-source-handoff.py`, and `scripts/preflight-v25-published-release-baseline.py` only where their source-order/token contracts legitimately change.

### Task 1: Lock RED regression coverage

**Files:**
- Create: `scripts/preflight-v25-auto-version-pr.py`

**Interfaces:**
- Consumes: approved spec and the not-yet-created `scripts/v25-auto-version-pr.py`.
- Produces: direct executable regression gate returning `0` only when allocator, renderer, metadata, workflow routing, token, and no-direct-main contracts hold.

- [ ] **Step 1: Write the failing preflight loader**

```python
HELPER = ROOT / "scripts" / "v25-auto-version-pr.py"
if not HELPER.exists():
    fail("automatic V25 version-PR helper is missing")
spec = importlib.util.spec_from_file_location("v25_auto_version_pr", HELPER)
```

- [ ] **Step 2: Add deterministic RED cases before production code exists**```python
assert module.choose_next_ordinal(committed=6, published=6, occupied={6, 7}) == 8
assert module.classify_committed_identity(committed=7, published=6, exact_tag=False, prior_owner=False) == "release"
assert module.classify_committed_identity(committed=7, published=6, exact_tag=True, prior_owner=False) == "prepare"
assert module.classify_committed_identity(committed=7, published=6, exact_tag=False, prior_owner=True) == "prepare"
expect_error(lambda: module.parse_preview_identity("0.2.0-preview.07"))
```

Also assert one valid generated Reservation-v2 body, deterministic branch naming, exact three rendered project paths, preserved `AssemblyVersion`, `QS3D_AUTOMERGE_TOKEN` workflow routing, and absence of `git push ... main`, direct PR merge API calls, or force updates.

- [ ] **Step 3: Run the new gate and prove RED**

Run: `python scripts/preflight-v25-auto-version-pr.py`
Expected: non-zero with `automatic V25 version-PR helper is missing` (or the first named missing interface), never a syntax/import accident.

- [ ] **Step 4: Commit only the RED regression plus approved docs if desired after evidence capture**

Do not weaken assertions to make later production code pass.

### Task 2: Implement deterministic identity and project rendering core

**Files:**
- Create/Modify: `scripts/v25-auto-version-pr.py`
- Test: `scripts/preflight-v25-auto-version-pr.py`

**Interfaces:**
- Produces `PreviewIdentity`, `parse_preview_identity`, `choose_next_ordinal`, `classify_committed_identity`, `rewrite_project_text`, `build_reservation_body`, and `canonical_branch_name`.
- Later workflow/mutation code consumes those exact names.

- [ ] **Step 1: Implement canonical identity type and parser**```python
@dataclass(frozen=True)
class PreviewIdentity:
    major: int
    minor: int
    patch: int
    ordinal: int

    @property
    def product_version(self) -> str:
        return f"{self.major}.{self.minor}.{self.patch}-preview.{self.ordinal}"

    @property
    def tag(self) -> str:
        return "v" + self.product_version
```

`parse_preview_identity()` must reject whitespace, leading-zero numeric components, ordinal `0`, ordinal `>65535`, missing `preview`, and malformed suffixes.

- [ ] **Step 2: Implement monotonic next-free allocation**

```python
def choose_next_ordinal(*, committed: int, published: int, occupied: set[int]) -> int:
    candidate = max(committed, published) + 1
    while candidate in occupied:
        candidate += 1
    if candidate > 65535:
        raise AutoVersionError("no preview ordinal remains in FileVersion range")
    return candidate
```

Treat every valid unscoped Issue #1441 reservation/fence ordinal as globally burned for safety, while tags/releases are filtered to the committed series.

- [ ] **Step 3: Implement exact project renderer**```python
PROJECTS = (
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
    "src/QS3D.Core/QS3D.Core.csproj",
)

def rewrite_project_text(text: str, target: PreviewIdentity) -> str:
    replacements = {
        "Version": target.product_version,
        "FileVersion": f"{target.major}.{target.minor}.{target.patch}.{target.ordinal}",
        "InformationalVersion": target.product_version,
    }
    # Require exactly one full-line element per key and leave AssemblyVersion untouched.
```

Reject source projects whose current three identities are not aligned before rendering the target.

- [ ] **Step 4: Implement exact Reservation-v2 metadata helpers**

`canonical_branch_name(issue, ordinal)` returns `agent/qs3d-release-automation/issue-<issue>-v25-preview-<ordinal>`. `build_reservation_body()` emits exact `Lane-Key`, `Reservation-Protocol: v2`, canonical owner/session, carrier, semantic Ownership-Key, one semicolon-separated three-file `Expected-Paths`, `Automation-Key`, and `Target-Version`.

- [ ] **Step 5: Run RED gate until pure/core cases are GREEN**

Run: `python scripts/preflight-v25-auto-version-pr.py`
Expected now: allocator/rendering/metadata cases PASS; workflow/mutation assertions may still fail because Task 3 is not implemented.

### Task 3: Add resumable GitHub preparation transaction

**Files:**
- Modify: `scripts/v25-auto-version-pr.py`
- Modify: `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`
- Test: `scripts/preflight-v25-auto-version-pr.py`

**Interfaces:**
- `prepare --repository OWNER/REPO --source-sha SHA --base-sha SHA --published-tag TAG --ledger-issue 1441` returns `0` after creating/resuming one canonical version PR, and non-zero on ambiguous/malformed state.
- Workflow invokes `prepare` only after natural batch READY and with command-scoped `GH_TOKEN="$QS3D_AUTOMERGE_TOKEN"`.- [ ] **Step 1: Add bounded read-only evidence collection**

Use `gh api --paginate` through a small `GhClient` wrapper to read published releases, open Issues, open same-repository PRs, Issue #1441 comments, branch refs, and current `main`. Bound open-Issue/PR enumeration to 10 pages of 100 and fail if a full final page means completeness is unknown.

- [ ] **Step 2: Reconcile a deterministic automation carrier before new mutation**

Search body metadata, not titles, for `Automation-Key: v25-auto-version-pr:<target-tag>`. Exactly one valid open Issue is resumable; one valid open PR for its canonical branch is authoritative/no-op; multiple or malformed claimants fail closed. If a valid open automation carrier exists for the same series, do not allocate a replacement while it remains unresolved.

- [ ] **Step 3: Create/update the Reservation-v2 Issue, then run the existing reservation precheck before source mutation**

Create the Issue as the first durable side effect, immediately patch its body with the actual Issue number/branch, then invoke:

```python
subprocess.run([
    sys.executable, "scripts/agent-reservation-precheck.py",
    "--issue", str(issue_number), "--repository", repository,
], check=True, env={**os.environ, "GH_TOKEN": token})
```

A crash between Issue creation and body normalization is recovered only by validating the exact automation key and expected title; never allocate a second Issue silently.

- [ ] **Step 4: Rebind protected main immediately before branch mutation**

Require two API reads of `main` to equal `--base-sha`; if they differ, fail before branch/blob/tree/commit mutation. The workflow must compute `base-sha` only after proving the original release source is still an ancestor and no newer release-relevant main change supersedes it. Main drift before durable mutation therefore aborts preparation instead of creating a stale carrier.

- [ ] **Step 5: Build one atomic three-file commit via Git Data API**

Create the canonical branch ref at the admitted base; create three blobs from rendered project text; create one tree with the base tree; create one commit with the admitted base as sole parent; PATCH only `refs/heads/<canonical-branch>` with `force=false`. Never update `refs/heads/main`.Use these endpoint families only:

```text
POST /repos/{repo}/git/refs
POST /repos/{repo}/git/blobs
POST /repos/{repo}/git/trees
POST /repos/{repo}/git/commits
PATCH /repos/{repo}/git/refs/heads/{branch}
GET /repos/{repo}/compare/{base}...{head}
POST /repos/{repo}/pulls
```

- [ ] **Step 6: Verify branch readback before opening a PR**

Read the branch head and compare it with the admitted base. Require one parent equal to base, exactly the three project paths, the three expected new blob SHAs, no deletion/rename, and all three rendered identities aligned to the target. Unknown pre-existing branch heads fail closed rather than being overwritten.

- [ ] **Step 7: Open/resume one ordinary PR**

Create a non-draft PR targeting `main` with deterministic title `chore(release): prepare <tag>`, body containing `Lane-Key: issue-<n>`, `Automation-Key`, and `Closes #<n>`. Do not call merge APIs or auto-merge APIs directly; the Hybrid Coordinator owns native auto-merge.

- [ ] **Step 8: Wire READY-state routing into the dispatcher**

Add `QS3D_AUTOMERGE_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}` to the dispatch step environment without replacing read-only `GH_TOKEN: ${{ github.token }}`. Define one Bash helper that validates the mutation token, freshly admits current `main`, then calls Python with command-scoped `GH_TOKEN`.

```bash
GH_TOKEN="${QS3D_AUTOMERGE_TOKEN}" python scripts/v25-auto-version-pr.py prepare \
  --repository "${GITHUB_REPOSITORY}" --source-sha "${source_sha}" \
  --base-sha "${preparation_base_sha}" --published-tag "${published_preview_tag}" \
  --ledger-issue 1441
```

Route both `committed_preview_ordinal <= published_preview_ordinal` and late exact-tag/prior-owner unusable states into this helper; leave exact same-source reservation/fence retry on the existing release path.### Task 4: Update legacy guards for the new non-starving state machine

**Files:**
- Modify: `scripts/preflight-v25-dispatch-committed-version.py`
- Modify: `scripts/preflight-v25-preview-dispatch-idempotence.py`
- Modify as required by actual source-order changes: `scripts/preflight-release-preview-sequence.py`
- Modify as required by actual source-order changes: `scripts/preflight-v25-cloud-release-stale-source-handoff.py`
- Modify as required by actual source-order changes: `scripts/preflight-v25-published-release-baseline.py`

**Interfaces:**
- These gates remain static/deterministic policy contracts around the dispatcher and must not duplicate the allocator implementation.

- [ ] **Step 1: Replace old neutral-starvation assertions**

In committed-version/idempotence models, a complete prior owner for the current ordinal now yields `prepare` rather than `prior-owner-neutral`; incomplete/mismatched/multiple owner state remains fail-closed.

- [ ] **Step 2: Preserve exact-release invariants**

Keep assertions for canonical committed ProductVersion parsing, same-source exact reservation reuse, exact dispatch-fence recovery, final protected-main admission, downstream release serialization, and `release-v25-cloud.yml` exact `source_sha`/`release_tag` inputs.

- [ ] **Step 3: Update only genuinely stale token/order checks**

Do not touch a legacy preflight merely to silence failure. For each changed guard, document in its failure text that READY-but-unusable identity must enter protected version-PR preparation while no-batch and exact-retry paths remain unchanged.

- [ ] **Step 4: Run focused suite**

Run all changed/new gates plus `scripts/preflight-ci-manual-only.py`, `scripts/preflight-v25-preview-reservation.py`, `scripts/preflight-v25-dispatch-reservation-enumeration.py`, `scripts/preflight-v25-dispatch-reservation-owner.py`, `scripts/preflight-v25-dispatch-final-source-admission.py`, and `scripts/preflight-v25-cloud-dispatch-concurrency.py`. Expected: all PASS.### Task 5: Fresh verification, commit, PR, and merge gate

**Files:**
- All files reserved by Issue #6524 only.

- [ ] **Step 1: Run syntax/format checks**

Run `python -m py_compile scripts/v25-auto-version-pr.py scripts/preflight-v25-auto-version-pr.py` and every modified Python preflight. Parse the dispatcher YAML with PyYAML and run `git diff --check`.

- [ ] **Step 2: Re-run Reservation-v2 precheck**

Run `python scripts/agent-reservation-precheck.py --issue 6524 --repository trinhtanphat/QS3D-BricsCAD` with authenticated token. Expected: PASS and no unreserved changed path.

- [ ] **Step 3: Run aggregate feature preflight**

Run `python scripts/preflight-all.py`. If the global aggregate wall-clock budget expires at an unrelated late gate, record that separately and run the named failed gate directly; never call the aggregate PASS unless it actually returns zero.

- [ ] **Step 4: Commit and refresh from protected main without force**

Commit normally on `agent/chatgpt-gpt56sol-releaseauto/issue-6524-v25-auto-version-pr`, fetch current `origin/main`, inspect overlap, and merge current main into the branch if needed. Re-run focused exact-head verification after any refresh.

- [ ] **Step 5: Push and open the implementation PR**

Push normally, open one PR titled `fix(release): automate protection-safe V25 preview version PR` with `Lane-Key: issue-6524` and `Closes #6524`, and do not use `no-automerge`.

- [ ] **Step 6: Require exact-head Shared CI before merge**

Wait for required Shared preflight/core/Hybrid Coordinator checks on the exact PR head. Merge only through normal coordinator/native-auto-merge after all required contexts are green; never force, bypass, or use direct merge API.

- [ ] **Step 7: Post-merge acceptance**

Verify `main` contains the helper/workflow/spec/plan, Issue #6524 is closed, and the natural dispatcher behavior is correct. Below threshold, no version PR is expected. At a natural READY batch with a published/burned committed identity, exactly one Reservation-v2 version PR must be created; do not force an artificial release solely for acceptance.