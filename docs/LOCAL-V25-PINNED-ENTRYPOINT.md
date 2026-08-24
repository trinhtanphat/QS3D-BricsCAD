# LOCAL V25 pinned exact-SHA entrypoint

This document defines the source-side entrypoint contract for local BricsCAD V25 qualification. It does not grant LOCAL_PASS and does not replace the scenario matrix in `docs/LOCAL-V25-QUALIFICATION.md` or the live status queue in `docs/LOCAL-AGENT-INBOX.md`.

For any LOCAL_ONLY handoff that names an exact source SHA, the local worker must first fetch/pull that SHA, check it out in a clean workspace, then invoke the pinned entrypoint:

```powershell
.\scripts\run-local-v25-pinned-qualification.ps1 `
  -ExpectedSourceSha "<exact 40-hex source SHA from handoff>" `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST"
```

The wrapper fails closed before aggregate preflight/build/runtime work when `HEAD` does not equal the requested SHA or when the worktree is dirty. After the delegated qualification finishes, it reads `qualification.json` and requires the emitted `exactSha` to equal the same handoff SHA.

`scripts/run-local-v25-qualification.ps1` is an implementation detail behind this pinned entrypoint for handoff execution. Do not invoke the unpinned implementation directly when validating a repository handoff that requires exact-SHA evidence.

The existing local runbook remains authoritative for prerequisites, scenario coverage, cleanup, sanitized evidence, and PASS/FAIL interpretation. This entrypoint only hardens source identity so a clean but wrong checkout cannot be mistaken for the requested candidate.
