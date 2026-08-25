# LOCAL-001 exact-main V25 qualification attempts

- Status: `NO_RESULT`
- Local execution issue: #3924
- Source correction: #3930 / PR #3932 / merge `ab0202194e33a1a27dbdf322b9b6d73b9d56778a`
- Parent qualification: #72 / `LOCAL-001`
- Initial source-gate SHA: `fba0342bf4cbd528fedffb475e6a30f9b68f3e6e`
- Exact one-shot rerun SHA: `ab0202194e33a1a27dbdf322b9b6d73b9d56778a`
- Branch: `agent/local003/issue-3924-local001-v25-current-main`
- Host available: licensed BricsCAD V25.2.10, Windows x64
- Unchanged runner SHA-256: `2D949D1046E109D10AA9772794E399098A63A9B599C73CBCAB62B736C9B0D009`

## Initial source-gate attempt

- exact clean Git SHA and pinned Platform submodule: PASS;
- manual-only CI policy: PASS;
- generic source preflight: PASS;
- aggregate feature preflight: FAIL because `scripts/preflight-smoke-registration.py` exceeded the canonical 180-second per-child budget;
- Core Release build and smoke: NOT RUN;
- installed-reference V25 `Release|x64` build: NOT RUN;
- offline WPF smoke: NOT RUN;
- licensed V25 NETLOAD/runtime marker: NOT RUN;
- complete interactive/private-DWG matrix and closeout: NOT RUN.

The aggregate step ran for 526.747 seconds before the child-timeout verdict. Current repository scale contains 1,515 C# smoke-test source files, including 891 files exposing a static `Run()`, plus 1,034 discovered feature-gate files. The smoke-registration gate builds one class-specific regular expression per runnable smoke and rescans every other source for each class. That repeated quadratic scan did not finish inside the existing bounded child budget on this workstation.

## Exact `ab020219` one-shot rerun

- exact clean detached Git SHA and pinned Platform submodule: PASS;
- manual-only CI policy and generic source preflight: PASS;
- aggregate feature preflight: PASS, all `1035/1035` discovered gates;
- Core Release build: PASS, `0 warnings / 0 errors`;
- Core deterministic smoke: PASS, `ALL PASS`;
- installed-reference V25 `Release|x64` build: PASS, `0 warnings / 0 errors`;
- offline WPF theme / Workspace / RightPanel qualification: PASS;
- licensed V25 NETLOAD / Ribbon / Palette runtime probe: `NO_RESULT` because no `QS3DRUNTIMEPROBE` marker appeared within the unchanged 120-second runner bound;
- package/signing: NOT REQUESTED;
- full interactive/private-DWG matrix and closeout: NOT RUN.

Adapter and Core ProductVersion were both `0.1.0-preview.10081`. Adapter SHA-256 was `1CBD959B4C6B4E66D37CA6681C2DABADE624DD7A1C18B9F7563BAB20A103299A`; Core SHA-256 was `30D57AC10F809576B11ECD074529AD2565C5B95D0E3A034A419B64E6AE847702`. Runtime metadata remained empty. The detached checkout and runner stayed unchanged, raw evidence remained ignored, and a post-run scan found zero BricsCAD processes.

## Disposition

The initial pre-runtime source-gate blocker was corrected remotely by #3930 and verified by the exact rerun's `1035/1035` aggregate PASS. The licensed marker timeout is `NO_RESULT`, not a BricsCAD product `FAIL`, `LOCAL_PASS`, or evidence that the complete interactive matrix ran. No source defect was established by this rerun, so no new `SOURCE_FIX_REQUIRED` handoff was created.

The exact candidate was invoked once only and was not retried. LOCAL-001 remains `IN_PROGRESS`; the complete interactive/private-DWG matrix remains pending and the machine-checkable closeout continues to fail closed.

Raw qualification JSON and any future runtime artifacts remain Git-ignored. This sanitized claim contains no proprietary binary, private/customer drawing, machine path, ProjectId, CAD Handle, fingerprint or raw exception detail.
