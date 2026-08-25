# #3681 exact a4f1a536 V25 live-BREP qualification PASS

- Status: `LOCAL_PASS / COMPLETE`
- Lane-Key: `issue-3681-881f-local-evidence`
- Evidence PR: #3849
- Parent local queue: #72
- Licensed qualification: #3681
- Supersedes: the earlier `447ba980...` unit-binding failure recorded on this carrier
- Source blocker resolved: #3846, merged by PR #3854
- Tested exact Git SHA: `a4f1a53683a9296532a0290fcb79bc49b9d4b892`
- Tested exact tree: `99cd99d1ca565e5b594548f0fe1210366ac61190`
- Required source-ready ancestor: `c64eb8c1b83761e155da670904a72e64669464b7`
- Evidence carrier refreshed through: `origin/main@119124ee0112be206e3008354503e7563c996e1a` (documentation descendant only; not the tested runtime binary)
- BricsCAD host: V25.2.10 Windows x64, licensed native runtime
- Plugin/Core ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `6821338295E82008DB9DA165BCC98AB3E35EC243ADCADE101B2CD1FEA5425338`
- Co-located Core SHA-256: `D65401EBCE849DD59B68B7163AF1F0F5194E0B73CD758378BE1FCF86D1414699`
- Local qualification harness SHA-256: `1AEEFC03BF75A7B31203F5DA44FED825F248DC97A1092C7813579CA388A93359`
- Sanitized report SHA-256: `2654599BD4A947B3DC12923A1E866A2437BB5A403B11DA868BF120C60B282E7F`
- Bounded run: `2026-08-25T02:44:39.2518773Z` through `2026-08-25T03:02:17.1229342Z`

## Exact identity and repository-safe gates

The committed `scripts/run-local-v25-wall-contact-3681.ps1` runner executed from a clean detached worktree at the exact tested SHA with the pinned Platform submodule initialized. The source/build baseline passed:

- manual-CI policy and generic source preflight;
- all `1028/1028` discovered feature preflight gates;
- Core Release build with zero warnings and zero errors;
- deterministic Core smoke with `ALL PASS`;
- BricsCAD V25 adapter `Release|x64` build with zero warnings and zero errors;
- offline WPF theme/Workspace/RightPanel checks;
- local qualification harness build with zero warnings and zero errors.

The general V25 runtime smoke remained intentionally skipped because the dedicated #3681 runner owns the licensed native phases. This claim qualifies #3681 only and does not promote the broader #72 matrix or customer-release status.

## Licensed native result

The exact source-fix gate passed both mandatory controls with no native failure:

| Control | Gross m2 | Deduction m2 | Net/residual m2 | Native path | Result |
|---|---:|---:|---:|---|---|
| touching-only one end | 2.6688 | 0.1600 | 2.5088 | contact cuts `1`, volume cuts `0`, failed native `0` | PASS |
| penetration 0.05 m | 2.6688 | 0.1600 | 2.5088 | contact cuts `0`, volume cuts `1`, failed native `0` | PASS |

The complete broader geometry marker then passed:

| Scenario | Sanitized result |
|---|---|
| no-neighbor baseline | deduction `0`, gross `2.6688` |
| full end-face contact | deduction `0.1600`, contact probe exercised |
| partial end-face contact | deduction `0.0800` at the unchanged `1e-6 m2` tolerance |
| overlapping/multiple neighbors | union deduction `0.1600`, no double subtraction |
| top/bottom contact | deduction `0` |
| semantic capture + refresh | deduction/net `0.1600 / 2.5088` |
| missing target BREP | stale deduction cleared to `0`, net restored to `2.6688` |
| read-only measurement | PASS; native Undo/Redo is explicitly not applicable to this non-mutating cell |
| two-end BLT control | gross/deduction/net `2.6688 / 0.3200 / 2.3488` |

A second fresh BricsCAD process/DWG repeated the complete geometry phase. Both sanitized markers were byte-identical with SHA-256 `8F3F334998D6074D058866D4A37ADC6F13F9194C0E3362789A133264BF288570`, proving process/DWG isolation for the asserted values.

## Persistence and cold reopen

The persistence phase saved a disposable DWG plus canonical QSDB with three solids and retained gross/deduction/net `2.6688 / 0.3200 / 2.3488`. A fresh BricsCAD process cold-opened that DWG/QSDB pair and returned:

```text
case.cold_reopen=PASS
reopen.gross_m2=2.6688
reopen.deduction_m2=0.32
reopen.net_m2=2.3488
status=PASS
```

The final committed runner report returned `LOCAL_PASS` for exact SHA `a4f1a53683a9296532a0290fcb79bc49b9d4b892`.

## Host supervision and evidence boundary

No production source, harness source, tolerance, workflow, or committed runner was patched. The licensed host required bounded external supervision only:

- the fixed installed plugin was temporarily switched from OnStartup to OnCommand (`LoadCtrls 2 -> 4`) so the exact worktree DLL could be NETLOADed instead of the already-installed same-identity assembly;
- test-owned BricsCAD windows were closed by exact PID after their managed markers completed; only disposable unsaved Drawing1 sessions were discarded;
- the persistence session's `SAVEAS` prompt required accepting the default DWG version before the runner's intended file path/NETLOAD/persist/QSAVE sequence could continue;
- cold reopen displayed BricsCAD's `Incompatible Units` dialog because the disposable drawing combined millimeter `INSUNITS` with the default Architectural display mode; `Change LUNITS` was selected so millimeter geometry/INSUNITS remained authoritative.

These host interactions did not manufacture or edit marker values. The production contact service, semantic capture/refresh, persistence and cold-reopen commands produced the asserted evidence.

## Cleanup

DemandLoad was restored to `LoadCtrls=2`. The installed loader path and its SHA-256 `0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30` remained unchanged. Final BricsCAD process count was zero. The disposable DWG, QSDB, BAK and lock residue were removed; raw scripts, local paths and runtime markers remain Git-ignored. The exact tested worktree was clean, and pre-existing user crash artifacts in the root worktree remained untouched.

#3681 may close as completed. Parent #72 remains open for its other LOCAL_ONLY rows.
