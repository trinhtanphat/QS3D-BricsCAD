# V26 update-manifest cleanup primary-failure runbook

Carrier: issue-5831 / C05. This scenario is REMOTE_SAFE infrastructure validation; it does not claim licensed BricsCAD runtime evidence.

## Contract

The V26 update-manifest wrapper must preserve the transformer/generated-manifest exception when that primary operation fails. Cleanup after a primary failure is secondary and must never replace the primary exception. When the primary operation succeeds, cleanup remains strict and fail-closed: the generated script and workspace must be ordinary/non-reparse objects, residue is rejected, unlink failures propagate, and postconditions prove both paths are absent.

Existing held-generation guarantees remain intact: the generated script is opened with `FileShare.Read`, read as bounded strict UTF-8, revalidated before/after execution, invoked by its canonical pathname, and cleanup remains non-recursive.

## Deterministic checks

Run `python scripts/preflight-v26-update-manifest-cleanup-primary-failure.py`. Then run the repository aggregate preflight and normal protected `preflight`/`core` contexts on the exact candidate SHA.

For a PowerShell failure-injection pass, use a disposable test checkout and intercept the transformer or generated implementation so it throws a distinctive primary exception while also causing cleanup admission/removal to fail. The surfaced failure must remain the distinctive primary exception. Do not treat leftover temp artifacts in this failure-injection case as a success-path acceptance result.

For the success-path cleanup pass, run the wrapper against disposable valid package inputs, verify the generated operation completes, and verify the generated temp script and its `qs3d-v26-manifest-*` workspace are absent afterwards. Inject unexpected workspace residue or a reparse-backed path and verify cleanup fails closed rather than recursively deleting it.

No `LOCAL_PASS` or licensed V25/V26 host claim is produced by these checks.