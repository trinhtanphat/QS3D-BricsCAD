# LOCAL-001 exact d52 canonical V25 NO_RESULT

- Status: `NO_RESULT / BLOCKED_ONCE / NO_RERUN`
- Carrier issue: `#3924`
- Parent queue: `#72`
- Lane-Key: `issue-3924`
- Owner/session: `codex-local003-20260825-d52`
- Canonical branch: `agent/local003/issue-3924-local001-d52-v25`
- Tested exact Git SHA: `d52a0065a3f63575885761bc59fab2c08a32f4a4`
- Tested exact tree: `cd35b4a6f2133dd238a6c1462cc5086e54b7b609`
- Platform submodule: `a5778f4abcf3b5c308c5d6854040dbc0c3082390`
- BricsCAD host: V25.2.10 Windows x64, licensed installation
- Canonical runner Git blob: `844d9a1a247d1e16fcdbcdb75795ae05e4e212ef`
- Canonical runner SHA-256: `2D949D1046E109D10AA9772794E399098A63A9B599C73CBCAB62B736C9B0D009`
- Built V25 plugin ProductVersion: `0.1.0-preview.10081`
- Built V25 plugin SHA-256: `847A13246E4AA6139C9F570DEC646574C23983320BD7CA2EBC449BF412E4BACE`
- Sanitized report identity SHA-256: `E8C55ECCA2DE147F33E6EEE65D875530C63515F1650C4378EDA435CD0A584233`
- Bounded attempt: `2026-08-25T15:48:51Z` through `2026-08-25T15:54:52Z`

## One-shot result boundary

The source-ready candidate was checked out detached and clean. The unchanged
`scripts/run-local-v25-qualification.ps1` was invoked exactly once. It completed
all repository-safe phases before the hosted boundary:

| Gate | Result |
| --- | --- |
| Exact Git SHA / clean tree | `PASS` |
| Manual-only CI policy | `PASS` |
| Generic source preflight | `PASS` |
| Aggregate feature preflights | `PASS (1043/1043)` |
| Core Release build | `PASS (0 warnings / 0 errors)` |
| Core deterministic smoke | `PASS / ALL PASS` |
| BricsCAD V25 adapter Release build | `PASS (0 warnings / 0 errors)` |
| Offline WPF / Workspace / RightPanel smoke | `PASS` |
| Licensed NETLOAD / Ribbon / Palette runtime | `NO_RESULT` |
| Post-#3985 Interchange continuation | `NOT RUN` |
| Remaining LOCAL-001 runtime matrix | `NOT RUN` |
| Customer/release qualification | `NOT QUALIFIED` |

At the licensed-runtime boundary the canonical runner detected one concurrently
opened BricsCAD V25 process and stopped before launching its own host, using the
dedicated-runner precondition error. A single post-run blocker audit confirmed
the process. It was not closed, automated, or otherwise touched. In accordance
with the owner instruction, no second blocker check and no rerun were performed.

The report's machine gate records the precondition as `FAIL`; the scoped licensed
product verdict is `NO_RESULT` because no QS3D DLL was loaded and no hosted
command executed. This is environmental interference and supplies no evidence of
a normal source defect, so `SOURCE_FIX_REQUIRED` is not claimed.

## Preservation and resume condition

DemandLoad was restored to `LoadCtrls=2`; the installed loader remained present
with unchanged SHA-256
`0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`.
The tested worktree remained clean. No production source, test source, canonical
runner, or committed harness was modified. Raw machine-path-bearing evidence
remains outside Git; this claim contains only sanitized identity and status data.

Keep #3924 and parent #72 open. A future owner-scheduled continuation needs an
exclusive licensed V25 host and a newly selected exact candidate. It must not
reinterpret this attempt as `LOCAL_PASS` or rerun this session's one-shot
candidate under the current instruction.
