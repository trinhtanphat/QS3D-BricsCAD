# Reference Search modeless publication qualification

## Scope

This runbook qualifies the V25 `QS3DREFSEARCH` modeless publication lifecycle introduced by issue #4809. Hosted/source checks prove source ordering and compile safety only. Licensed BricsCAD runtime rows remain LOCAL_ONLY and must never be inferred from hosted CI.

## Source contract

- Exactly one authoritative loaded `ReferenceSearchWindow` is published by `ReferenceSearchCommands`.
- Re-invocation for the exact same managed `Document` and same non-zero native database identity reuses and activates the published window.
- Same-native managed-wrapper drift is not blindly reused because the window captures the original wrapper and its browser-launch affinity contract is wrapper-specific.
- Wrapper drift or cross-DWG invocation must terminal-close the old owner before a replacement can publish.
- Close exception or close veto (`IsLoaded` remains true) fails closed: no duplicate window is published.
- Stale unloaded publication may be released defensively.
- A candidate is published only after `Application.ShowModelessWindow` returns and `IsLoaded` is true.
- Only the exact matching `Closed` callback may release the authoritative publication.
- `ReferenceSearchWindow` preserves both managed-wrapper and native-database affinity before browser launch.
- Existing query length bound, SafeSearch provider URLs, technical-context suffix bounds and `UseShellExecute=true` behavior remain unchanged.

## Hosted/source validation

Run the auto-discovered source guard:

```text
python scripts/preflight-reference-search-window-publication.py
```

Then run the repository-required Shared CI. Green hosted CI is not licensed runtime evidence.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Execute on one exact integrated SHA/product identity in a compatible licensed BricsCAD V25 Windows x64 host. Preserve repository/local-agent evidence rules and sanitized output only.

1. **Same document repeat invoke** — open drawing A, run `QS3DREFSEARCH` twice; the second command activates the existing window and does not create another loaded Reference Search surface.
2. **Cross-DWG replacement** — with A's window loaded, activate drawing B and invoke; A's window must reach terminal Closed before B's replacement is shown/published.
3. **Managed-wrapper drift** — reproduce only with an evidence-backed host path that yields a new managed wrapper for the same live native database. The old wrapper-bound window must not be reused; terminal close precedes replacement.
4. **Close veto/exception** — when the host/window close path refuses or throws, invocation must fail closed and retain at most the original loaded owner.
5. **Document destroy** — closing/destroying the bound drawing exercises `DocumentBoundWindowLifetime`; no stale loaded Reference Search window may retain usable callbacks for the destroyed document.
6. **Browser-launch affinity** — exact bound drawing active + same native database permits search; another active drawing or changed native identity is rejected before `Process.Start`.
7. **Provider regression** — images/web/video/shopping/shorts/news continue to use the bounded encoded query and SafeSearch where supported; technical-context and shorts suffixes respect the 512-character cap.
8. **Cleanup** — close all test windows/drawings and verify no owned process/private-state residue beyond the browser launched intentionally by the tester.

Record `LOCAL_PASS`, `FAIL`, or `NO_RESULT` only for rows actually observed on the stated exact SHA. Do not promote compile, source inspection, mock execution, or hosted CI to `LOCAL_PASS`.
