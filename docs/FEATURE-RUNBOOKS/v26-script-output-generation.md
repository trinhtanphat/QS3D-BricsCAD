# V26 generated-script output generation safety

## Scope

This runbook covers `scripts/new-v26-script-from-v25.ps1` publication of generated V26 PowerShell scripts. It is REMOTE_SAFE build/release infrastructure; it does not constitute licensed BricsCAD runtime evidence.

## Required contract

1. Keep V25 template admission bound to the held source-file generation and preserve strict UTF-8/hash validation.
2. Validate the V26 output ancestor chain as ordinary/non-reparse before publication work.
3. Create a missing output parent without `-Force`, then open and hold that exact ordinary parent directory generation with a native directory handle that does not share delete access.
4. Keep the admitted parent handle alive from before sibling staging through final publication and cleanup. Revalidate handle identity/path binding before staging, publication, and cleanup.
5. The final output leaf is fresh-only for this generator. If it already exists or appears during generation, fail closed rather than replacing an unadmitted destination generation.
6. Stage UTF-8-no-BOM output as a unique sibling ordinary file and publish atomically with `File.Move` only after the held-parent binding remains valid.
7. Never fall back to direct final-leaf writes, `File.Replace`, force-overwrite moves, recursive cleanup, or reparse-backed output paths.

## Deterministic guards

Run from repository root:

```text
python scripts/preflight-v26-script-output-generation.py
python scripts/preflight-v26-script-generation-filesystem-safety.py
```

Both guards include mutation probes and must reject removal of the held-parent/fresh-only/atomic-move contract.

## CI / merge evidence

The canonical candidate still requires fresh exact-head protected `preflight` and `core`, latest-main reconciliation, expected-head merge, and exact protected-main verification. Remote/static CI must never be reported as licensed BricsCAD `LOCAL_PASS`.
