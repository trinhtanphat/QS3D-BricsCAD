# Planning — issue #3298 — NETLOAD invalid-sidecar lifecycle resilience

Lane-Key: `issue-3298`  
Canonical branch: `agent/chatgpt-gpt56sol/netload-sidecar-memo-3298`  
Baseline: `ca8f22416d63c841a4a7d19ddc34f3538d4a8d80`

## Session review

- [ĐÃ XÁC NHẬN] The owner wants the current source bug fixed through the repository's issue/branch/CI/PR workflow and explicitly authorizes merge to `main` after protected checks are green.
- [ĐÃ XÁC NHẬN] `DocumentLifecycleCoordinator.Start()` already defers project/selection/UI reconciliation to an ApplicationIdle dispatcher so the initial `NETLOAD` call does not synchronously perform sidecar/palette work.
- [ĐÃ XÁC NHẬN] `EnsureProject(...)` invokes `ProjectContextCoordinator.TryGetReadOnly(...)` on lifecycle reconciliation/activation.
- [ĐÃ XÁC NHẬN] `TryGetReadOnly(...)` performs `LoadExistingProjectOrThrow(...)` whenever a named drawing has an existing `.qsdb`/`.bak` and no canonical in-memory project is already cached.
- [ĐÃ XÁC NHẬN] An unreadable existing sidecar is fail-closed: `LoadExistingProjectOrThrow(...)` throws instead of creating a default project, and the existing sidecar is left unchanged.
- [ĐÃ XÁC NHẬN] Before this change, lifecycle reconciliation did not remember a stable failed sidecar generation; therefore later activations/reconciliations could repeat the same load/fallback/parse work and repeat the same command-line error.
- [SUY LUẬN] On a sufficiently large, inaccessible, or malformed `.qsdb`/`.bak`, repeated read/fallback/deserialization on lifecycle activation can present to the user as continued NETLOAD/startup lag even though `Start()` itself has already returned.
- [CHƯA RÕ] Remote CI cannot prove the exact interactive latency improvement inside the owner's licensed BricsCAD workstation/runtime; that acceptance remains LOCAL_ONLY and must not be fabricated.
- [ĐỀ XUẤT] Memoize only lifecycle failures whose pre-read and post-failure `ProjectSidecarRevisionStamp` are identical. Skip repeated reads for that exact generation, but retry automatically if `.qsdb`/`.bak` changes.
- [ĐỀ XUẤT] Check the canonical in-memory project cache before honoring a failure memo so an explicit successful reload/recovery immediately invalidates the lifecycle skip.
- [ĐỀ XUẤT] Preserve the first actionable command-line diagnostic, suppress repeat command-line spam for unchanged failed generations, and still refresh unavailable-project palette state on UI-refresh activations.

## Implementation plan

1. Add a per-Document lifecycle failure memo in `DocumentLifecycleCoordinator` keyed by the existing `ProjectSidecarRevisionStamp` contract.
2. Capture the attempted sidecar generation before the read and re-capture after an `InvalidDataException`; memoize only when the generation is stable and contains an existing sidecar/backup.
3. On later reconciliation, skip `TryGetReadOnly` only when the exact generation is unchanged and no canonical project has appeared in memory.
4. Remove the memo on success/no-project, document teardown, lifecycle stop, external sidecar generation change, capture failure, or successful explicit reload/cache.
5. Keep the lifecycle path read-only: no `GetOrCreate`, `Save`, replacement project, or sidecar mutation.
6. Add an automatically discovered `preflight-*.py` regression guard covering ordering, invalidation, fail-closed behavior and command-line spam suppression.
7. Obtain automatic exact-head branch CI before opening the PR; then require protected `preflight` + `core` on the current merge candidate before merging.

## Acceptance

- Unchanged unreadable sidecar generation: first lifecycle attempt reads/fails/reports; subsequent lifecycle reconciliation skips the expensive read and does not re-write the same editor diagnostic.
- Changed `.qsdb` or `.bak` generation: memo is invalidated and a fresh read is attempted.
- Successful explicit reload: canonical cache invalidates the memo on the next lifecycle pass.
- Corrupt sidecar remains untouched; no default/replacement project is created by lifecycle reconciliation.
- V25 plugin compiles in CI; V26 consumes the shared V25 coordinator source through the repository's existing linked-source architecture.
- Licensed interactive BricsCAD NETLOAD timing remains LOCAL_ONLY evidence after merge.
