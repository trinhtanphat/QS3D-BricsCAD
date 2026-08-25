# Issue #3938 — LOCAL-010 canonical campaign partial evidence

Status: `BLOCKED / ENVIRONMENT_AND_MATRIX_PREREQUISITES`

Local campaign: #3938

Runner-preparation dependency: #3928

Initial source defect: #3945 (resolved by #3950)

Current source defect: none observed by R4. #3959 is closed and runner PR #3969 is merged.

Lane-Key: `local-agent-e-local010-runtime-20260825`

## Lane selection

LOCAL-009 was evaluated first as requested. The Windows certificate stores contained zero currently valid Code Signing certificates with an accessible private key and the Code Signing EKU. No signing PFX, password, expected-thumbprint or timestamp-server variables were configured. No key, certificate subject, thumbprint or secret was read into evidence, and no signing or release operation was attempted.

The campaign therefore selected LOCAL-010. No GitHub Actions workflow, release publication, `main` write or merge was authorized or performed.

## Exact execution identity

- Tested source: exact clean `dffb7e334f997a981a9d918c23b67592e232d61f`.
- Branch: `agent/local-e/issue-3938-local010-runtime-20260825`.
- Host: Windows 11 Pro x64 build 26200, interactive desktop.
- BricsCAD: licensed V25.2.10 x64 with the installed V25 managed references present.
- Hardware class: Intel Core i5-8250U, 8 logical processors, 7.9 GiB RAM, Intel UHD Graphics 620, 1366 x 768 display.
- Initial display scale: 100 percent (`AppliedDPI=96`).
- Pre-run and post-run BricsCAD process count: zero.

The raw runner report remains under ignored `artifacts/`. No customer/private drawing, raw CAD Handle, ProjectId, local path, screenshot, user identity, proprietary BricsCAD binary or signing material is committed.

## Canonical runner result

`scripts/run-local-v25-qualification.ps1` ran from 2026-08-25T09:17:28Z through 2026-08-25T09:26:38Z.

| Stage | Result |
|---|---|
| Exact Git SHA / clean tree | PASS |
| Manual-only CI policy | PASS |
| Generic source preflight | PASS |
| Aggregate feature preflights | FAIL |
| Core Release build/smoke | NOT RUN |
| Offline WPF smoke | NOT RUN |
| V25 adapter build | NOT RUN |
| Licensed NETLOAD/Ribbon/palette smoke | NOT RUN |
| LOCAL-010 performance/UI matrix | NOT RUN |

The report correctly remained `status=FAIL`, `sourceBuildStatus=FAIL_OR_INCOMPLETE`, `runtimeSmokeStatus=FAIL_OR_INCOMPLETE`, `fullInteractiveMatrixStatus=NOT_RUN`, and `customerReleaseQualified=false`. This is not a LOCAL-010 product-performance FAIL and must not be promoted to `LOCAL_PASS`.

## Exact blocker

The focused command

```powershell
py -3 .\scripts\preflight-preflight-all-discovery.py
```

fails deterministically with:

```text
AssertionError: timeout reason must remain visible
```

The self-test sets `runner.CHILD_TIMEOUT_SECONDS = 0.05` before running both a supposed fast control and the intended slow timeout case. On this normal Windows workstation, launching the fast Python child takes longer than 50 ms. The aggregate runner therefore times out `preflight-a-ok.py` first. The child exits during the Windows cleanup race, cleanup returns exit 128, and the aggregate stops before `preflight-b-slow.py`; the test then requires a timeout summary for the slow child that never ran.

The relevant source blobs were unchanged on the subsequently fetched `origin/main@ff6c298d33da2aec5e3e0f38503571899e185686`:

- `scripts/preflight-preflight-all-discovery.py`: `1b01f883adf2e777d2504d00c68187ca52fdf433`;
- `scripts/preflight-all.py`: `3febdd1e3eb6ac9fff766b0bde602b41026c15d3`.

Fast-forwarding to that newer main would therefore not remove the blocker or create a valid new candidate.

## Resumption after #3945 / #3950

Issue #3945 was corrected by merged PR #3950. The focused discovery regression then passed. LOCAL-010 refreshed source and restarted the canonical qualification from a fully materialized, clean detached checkout at exact candidate `10ced6bef3f7fb3b59d2d760e694fddfc4e6348a`, including the exact registered Platform submodule revision.

| Stage | Result |
|---|---|
| Exact Git SHA / clean tree | PASS |
| Manual-only CI policy | PASS |
| Generic source preflight | PASS |
| Aggregate feature preflights | FAIL |
| Core Release build/smoke | NOT RUN |
| Offline WPF smoke | NOT RUN |
| V25 adapter build | NOT RUN |
| Licensed NETLOAD/Ribbon/palette smoke | NOT RUN |
| LOCAL-010 performance/UI matrix | NOT RUN |

The aggregate next timed out `scripts/preflight-external-orchestration-aliases.py` at its 180-second child bound. A later `scripts/preflight-interchange-use-source-target-binding.py` timeout occurred with only about 1.975 seconds of the 900-second aggregate budget remaining; it is a derivative budget-exhaustion symptom, not evidence of a second source defect.

The external-orchestration gate was then run by itself without the aggregate timeout:

```powershell
py -3 .\scripts\preflight-external-orchestration-aliases.py
```

It passed, but required `270.338 s`. The gate recursively enumerates the checkout, performs per-path file checks and reads matching text files while excluding only `.git` path parts. The clean materialized checkout exposed about 5,618 files and 3,106 matching text candidates, so the focused measurement independently exceeds the production 180-second child limit.

At handoff, subsequently fetched `origin/main@0f0169ab23e71ea382614a5f89f167542ae6bed0` still contained the same gate blob as the tested candidate:

- `scripts/preflight-external-orchestration-aliases.py`: `810e288978ac984df009eeb6b5097289702b2e9e`.

This second run also remained `status=FAIL`; it did not produce build, licensed runtime, performance, DPI or layout qualification evidence. Raw reports remain ignored under `artifacts/`.

## Historical disposition before R4

Issue #3945 and PR #3950 resolved the initial nondeterministic 50 ms regression blocker. Issue #3959 now owns the remote/source-safe correction for the 270.338-second external-orchestration discovery gate. The correction must preserve full governance coverage while making enumeration and reads deterministic and bounded below the existing production child limit.

The local worker made no production, test-runner or qualification-runner source edit. Per the local-worker boundary, the LOCAL-010 performance/UI matrix stopped before build/runtime instead of bypassing either failed canonical gate. Resume #3938 only after the #3959 source fix is merged, refresh to one clean exact intended SHA, and rerun the canonical qualification from the beginning before collecting DependencyGraph/regeneration, Rooms, wall junction, Auto Host, Curtain, BQ/BBS/ED2/Interchange, Health/ownership, rebar-limit or DPI/layout evidence.

## R4 resumption after #3959 / #3969

#3959 is closed and runner PR #3969 is merged. Local Agent E refreshed the intended source and ran the canonical runner from a clean detached checkout at exact source:

```text
e9c1b4bdbc2199de562895b79d0fde7e1e0ff33c
```

The merged runner head `c841cf707d0c6b90eb2fd6bf0e499698d6b95837` is represented by that candidate. The R4 baseline ran from `2026-08-25T13:32:43Z` through `2026-08-25T13:36:06Z`.

| Canonical baseline stage | Result | Elapsed |
|---|---:|---:|
| Exact Git SHA / clean tree | PASS | 0.260 s |
| Manual-only CI policy | PASS | 0.148 s |
| Generic source preflight | PASS | 1.201 s |
| Aggregate feature preflights | PASS — 1041/1041 | 124.440 s |
| Core Release build | PASS — 0 warnings / 0 errors | 5.363 s |
| Core deterministic smoke suite | PASS — `ALL PASS` | 46.469 s |
| BricsCAD V25 adapter Release build | PASS — 0 warnings / 0 errors | 6.067 s |
| Offline WPF theme / Workspace / RightPanel smoke | PASS | 0.605 s |
| Licensed V25 exact-DLL NETLOAD / Ribbon / Palette runtime probe | PASS | 18.035 s |

The baseline report correctly records `status=PASS`, `sourceBuildStatus=PASS`, `wpfSmokeStatus=PASS`, `runtimeSmokeStatus=PASS`, `fullInteractiveMatrixStatus=NOT_RUN`, and `customerReleaseQualified=false`. Baseline PASS does not replace LOCAL-010's full performance/UI matrix and is not a customer-release claim.

### Supplemental Core performance evidence

The exact candidate also completed deterministic Core-only profiling with seven measured iterations after two warmups:

| Scenario | Scale | Median | p95 |
|---|---:|---:|---:|
| Dependency graph rebuild | 10,000 elements | 17.681 ms | 21.845 ms |
| Dependency closure | 10,000 elements | 7.012 ms | 17.399 ms |
| Mark changed | 10,000 elements | 67.797 ms | 97.677 ms |
| Targeted regeneration | 10,000 elements | 79.419 ms | 102.641 ms |
| Quantity trace generation | 10,000 elements | 25.859 ms | 30.638 ms |
| Quantity trace generation | 100,000 elements | 88.026 ms | 97.295 ms |

An attempted 20,000-element all-scenario profile failed closed at the documented 10,000-element DependencyGraph rebuild limit. This is a supported-boundary observation, not a crash and not a native V25 performance PASS. Core profiling remains supplemental to the required representative in-host matrix.

### Partial licensed runtime observations

R4 used only tracked synthetic data copied to ignored disposable evidence. The synthetic sidecar contained 11 semantic elements, so it was explicitly not treated as a representative large model.

- Start Center rendered inside BricsCAD at the machine's real 100 percent scale. The `Dự án` and `Cấu hình` Ribbon groups, hosted two-column shell and bottom `Mô hình` / `BQ` affordances were visible. The `Mô hình` route opened the BricsCAD-hosted model workspace.
- Small-fixture command-completion observations were Health 1214 ms, Regeneration 198 ms, BQ 386 ms and BBS Review 454 ms. They are smoke observations only.
- BQ and Health modeless windows rendered under the licensed V25 process. Two disposable drawings produced independent per-document BQ/Health windows. Re-running BQ for the same drawing did not add another BQ window, and closing one drawing removed its bound windows.
- The Windows session exposed only DPI 96 / 100 percent at 1366 x 768. No genuine 125, 150 or 200 percent display session was available.
- The RDP framebuffer later stopped rendering, and the system volume reached zero free bytes. The campaign stopped opening new cases immediately to avoid a crash or corrupt evidence.

The interactive report ran from `2026-08-25T13:32:42Z` through `2026-08-25T14:05:11Z` and records all 21 rows as `BLOCKED`. No row is promoted from partial/static evidence, `localPassClaimedByRunner=false`, and no `LOCAL_PASS` is claimed.

### DemandLoad and host cleanup

The installed registration used `LoadCtrls=2` before the campaign. To prevent the installed OnStartup DLL from preempting the exact candidate, R4 used a guarded, reversible `2 -> 4` isolation only for its owned run. Final cleanup verified:

- normal BricsCAD application close and stable process count zero;
- guarded `LoadCtrls` restoration from 4 to the original 2;
- installed Loader path unchanged;
- installed DLL SHA-256 unchanged at `0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`;
- before/after DemandLoad registry exports byte-identical;
- the R4-specific BricsCAD profile exported to ignored evidence and then removed.

Sanitized evidence identities:

- interactive report SHA-256: `4B1F193FCC069F9E84F04AD2DAC341718F2B86CAACFA9E4D5D48E060656D3F69`;
- canonical baseline report SHA-256: `FE44ACC5470D4A0A7D24A554D6C5033E1BDE17DAF6572832CCB51F35627E5FC7`;
- licensed runtime marker SHA-256: `17631691F2A229B19B87EA1B61F4ABE02CF70DC3346E452F68622240216EB25`.

Raw reports and screenshots remain ignored. No customer/private drawing, path, CAD Handle, ProjectId, user identity, proprietary BricsCAD binary or signing material is committed.

## Current disposition

LOCAL-010 remains open and `BLOCKED`, but no longer on the earlier source-runner defects. Resume only with adequate system-volume headroom, a representative sanitized/disposable large V25 project, and genuine 125/150/200 percent display configurations. Re-run all 21 rows against one clean exact candidate; only a complete all-PASS report may support a later human `LOCAL_PASS` decision.

No workflow was dispatched, no release/signing operation was attempted, no production source was edited, and no merge or write to `main` was performed.
