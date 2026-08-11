# Direct Draw preview-project freshness

Direct Draw quick/advanced commands may read Family defaults before they mutate the canonical QS3D project. Any flow that then waits for user input must not assume the same project is still authoritative at commit time.

## Source contract

`DirectDrawProjectPreviewContext` records whether a QS3D project existed when defaults were read and, when present, its `ProjectId`.

Before `ProjectStateSnapshot`, source CAD creation, semantic capture or native generation:

- a preview that started with a project must bind the canonical existing project and require the same `ProjectId` (**same-ProjectId** continuity);
- a preview that started projectless must still be projectless; if a project appears while the user is confirming parameters, the command refuses and asks for a rerun;
- a missing/replaced project is never silently replaced by `GetOrCreate` after project-derived defaults were shown;
- projectless authoring remains creation-capable only when no project appeared before the mutation boundary.

The projectless resolver checks both the primary `.qsdb` and `.bak` backing store immediately before `GetOrCreate`, then checks them again after binding. This closes the narrow check-to-bind race where a sidecar could appear after the read-only absence probe but before `GetOrCreate` captured its baseline. If that happens, the speculative cached bind is forgotten and the command refuses before source/semantic/native mutation.

The contract is intentionally about project identity, not unrelated `ChangeVersion` changes. User-confirmed numeric values are explicit command input; an unrelated semantic edit does not by itself invalidate them.

## Covered advanced flows

The freshness boundary covers the prompt-bearing advanced commands:

- `QS3DDRAWWALLADV`, `QS3DDRAWBEAMADV`, `QS3DDRAWSLABADV`, `QS3DDRAWCOLUMNADV`;
- `QS3DDRAWGLASSWALLADV`, `QS3DDRAWWALLPIERADV`, `QS3DDRAWSTRUCTWALLADV`, `QS3DDRAWFOUNDATIONADV`;
- `QS3DDRAWDOORADV`, `QS3DDRAWOPENINGADV`, `QS3DDRAWWINDOWADV`;
- `QS3DDRAWWALLREFADV`.

Door/Opening and Window share their quick/ADV executor, so both quick and advanced paths resolve through the same preview context. Reference Wall also resolves its preview before creating the new owned source LINE; the selected reference LINE remains read-only.

## Expected failures

An advanced command must fail closed before any command-owned source/semantic/native mutation when:

- the preview used an existing project and that project is removed or replaced before commit;
- the command started projectless and a project appears before commit, including in the narrow backing-store check-to-bind window;
- the canonical existing project rebind returns a different `ProjectId`.

A rerun then obtains the current Family defaults from the new authoritative state.

## Regression and local proof

`scripts/preflight-direct-draw-project-preview-freshness.py` statically guards the shared resolver, the pre/post-bind backing-store check, speculative-cache cleanup, and covered command families.

Exact BricsCAD V25 interaction proof remains in `LOCAL-008` of `docs/LOCAL-AGENT-INBOX.md`: hold each `*ADV` command at a numeric prompt, replace/remove/reload the project or make a project appear, then confirm the command refuses before source CAD, semantic or native mutation. This already covers the source-hardening scenario above; no duplicate local queue item is required. Source review or static preflight does not constitute `LOCAL_PASS`.
