# MCP dual-control capabilities v2 — implementation plan

## Goal

Complete the released dual-control design from a fresh post-#5032 main while respecting active reservation #5047.

## Tasks

1. Reserve only collision-free source/docs paths under Issue #5051.
2. Add `scripts/preflight-mcp-dual-control-capabilities-v2.py` first and observe a protected-CI RED caused by missing production capability contract.
3. Update `McpBackgroundHostRuntime.cs`:
   - explicit Background Control tool descriptions;
   - local foreground enable/disable helpers;
   - strict combined foreground availability;
   - simultaneous capability JSON status;
   - explicit-only/no-implicit-fallback wording;
   - local-consent recheck before global input.
4. Update `McpPersistentAgentCenterAugmenter.cs` without touching #5047's canonical Agent Center path:
   - show Background and Foreground summaries;
   - synchronize Resume/Pause/Emergency with foreground policy;
   - use direct local policy helpers for the dedicated toggle;
   - detect consent revocation and disarm stale foreground policy;
   - fail closed on synchronization errors;
   - preserve Credential Manager key persistence.
5. Add focused feature runbook and design record.
6. Run exact-head protected CI and inspect any failure logs; fix root causes without bypassing reservation/quality gates.
7. Review the PR diff for unrelated changes and safety regressions.
8. Bring the branch up to date with main if necessary, re-run required checks, mark ready, and merge only when protected gates are green.
9. Record source PASS separately from licensed BricsCAD `LOCAL_ONLY` runtime qualification.

## TDD evidence

The test-first commit `ad72f12f73cbdac964f8e4e0ba7f1c4818c2cc0f` produced protected run `33393536576`. Reservation/path collision checks passed, and the discovered feature-source-guard job failed specifically because the runtime/augmenter did not yet contain the dual-control contract. This is the expected RED baseline for the subsequent production changes.
