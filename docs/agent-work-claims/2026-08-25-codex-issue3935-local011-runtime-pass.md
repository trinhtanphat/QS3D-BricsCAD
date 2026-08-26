# LOCAL-011 licensed-runtime qualification PASS

- Carrier issue: `#3935`
- Lane-Key: `issue-3935`
- Canonical branch: `agent/local003/issue-3935-local011-v25-qualification`
- Exact tested SHA: `fbf1e7923cbde9037637e5b6b1339b31f491c87a`
- Tested source baseline ancestor: `20d029859ddd69ad25b8db40111b297bbfaa374a`
- Product DLL SHA-256: `2072421697D8C33FE29A857822EB67AB9EBD6C634C2CFA38C7AA12DD19B42B03`
- Environment: Windows x64; BricsCAD Ultimate V25.2.10; V25 `Release/net48`
- Outcome: `LOCAL_PASS`

## Qualification boundary

The canonical `scripts/run-local-v25-local-011.ps1` report returned `PASS` with all 21 required case IDs recorded as `PASS`. Its exact-SHA baseline also returned `PASS`, including 1042/1042 aggregate source gates, Core deterministic smoke, V25 `Release|x64` build with 0 warnings and 0 errors, offline WPF checks, and the licensed V25 NETLOAD/Ribbon/Palette smoke. The report retains `localPassClaimedByRunner=false` by design; this local claim promotes the result only because the full evidence-backed matrix passed on the same exact SHA.

This qualification applies only to `fbf1e7923cbde9037637e5b6b1339b31f491c87a`. Later source descendants are not implicitly qualified.

## Sanitized exact-SHA evidence

| Scope | Result | Sanitized proof |
| --- | --- | --- |
| Canonical LOCAL-011 runner | `PASS` | 21/21 required rows passed; report error was empty; exact baseline and licensed runtime-smoke SHA matched the tested SHA. |
| Generated exact-set and modeless harness | `PASS` | Ten stale/missing exact-set cases and ten complete-live controls covered Grid, Curtain LINE/PATH, and seven representative Rebar owner families. Refusals preserved semantic snapshots, surviving native objects and ownership metadata with no partial replacement; complete controls removed old sets exactly and created complete new sets. Malformed and duplicate-canonical Rebar metadata refused, and foreign/unmarked objects survived. |
| Modeless/cache/reload lifecycle | `PASS` | Door, Room, BBS, BQ and Rebar Mesh windows crossed canonical cache replacement and `QS3DRELOAD`. Door/Room/BBS detached safely, BQ rebound and wrote only the canonical replacement, and Rebar Mesh stale Save refused without semantic/native mutation. With project state unavailable, stale Recognition Apply and all stale modeless writes refused; document switching left the other drawing unchanged; reactivation rebound the canonical project while palette visibility remained available. |
| Curtain P06 ownership refusal | `PASS` | Four production refusal modes covered missing expected handle, duplicate canonical alias, cross-owner claim and foreign/unmarked live object. All refused before erase/append from a 90-panel baseline; the valid control produced 21 healthy replacement panels. |
| Curtain P08 staged rollback | `PASS` | Seven one-shot boundaries covered semantic regeneration plus six LINE/PATH host/frame/panel phases. Every abort preserved the whole semantic/native batch and source geometry from a 63-object baseline; the valid control produced 87 healthy objects with complete old-set removal. |
| Curtain P09 post-commit failures | `PASS` | Both fingerprint-stamp and UI-refresh failures retained committed replacements. Generated counts were 30 baseline, 34 fingerprint-failure, 34 clean recovery and 38 UI-failure; the final UI-failure state had zero Health issues. |
| Curtain P11 Undo/save/reopen | `PASS` | Undo removed the generated after-set and restored semantic before-state; Redo restored coherent after-state. Sidecar save, cold reopen and ownership-scoped rebuild remained coherent at 1 host, 10 frames and 15 panels; private-state cleanup and disposable drawing restoration passed. |
| Curtain P12 multi-DWG/modeless | `PASS` | Wrong-DWG routed actions refused, both project states remained unchanged, reactivation restored the A-bound action, destroying A closed its bound window, and B remained active and unchanged. Both disposable drawings remained byte-identical and cleanup passed. |
| Host and private-state cleanup | `PASS` | BricsCAD PID count stayed zero for seven samples over six seconds. DemandLoad returned to the pre-campaign installed DLL with `LoadCtrls=2`; installed DLL SHA-256 returned to `0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`. Scoped disposable DWG/QSDB/backup/lock/script residue and Issue-3935 test profiles were removed; sanitized JSON/marker evidence remained ignored locally. |

## Adjacent defect boundary

Issue `#3989` remains open for direct `RecognitionWindow` construction failing on a TwoWay binding to read-only `RequiresReview`. LOCAL-011 did not patch production source. The required `recognition.stale_apply_no_project` row still passed because the stale Apply path was invoked against unavailable project state and proved no replacement cache or project mutation. This `LOCAL_PASS` does not claim that the separate Recognition window display defect is fixed.

## Repository boundary

No production source was changed by the local worker. The task branch carries only sanitized documentation. Raw handles, nonces, disposable drawing/project state, scripts and machine paths are not committed. PR `#3964` is the documentation carrier and remains subject to normal review; this lane has no authorization to merge `main`.
