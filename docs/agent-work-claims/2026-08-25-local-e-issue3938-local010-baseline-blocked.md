# Issue #3938 — LOCAL-010 canonical baseline blocked

Status: `BLOCKED / SOURCE_FIX_REQUIRED`

Local campaign: #3938

Runner-preparation dependency: #3928

Source defect: #3945

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

## Disposition

Issue #3945 owns the remote/source-safe correction. The regression must become deterministic without assuming a complete Python child can start and exit within 50 ms, while preserving real aggregate wall-clock bounds, visible timeout classification and owned process-tree cleanup.

The local worker made no production, test-runner or qualification-runner source edit. Per the local-worker boundary, the LOCAL-010 performance/UI matrix stopped before build/runtime instead of bypassing the failed canonical gate. Resume #3938 only after the source fix is merged, refresh to one clean exact intended SHA, and rerun the canonical qualification from the beginning before collecting DependencyGraph/regeneration, Rooms, wall junction, Auto Host, Curtain, BQ/BBS/ED2/Interchange, Health/ownership, rebar-limit or DPI/layout evidence.
