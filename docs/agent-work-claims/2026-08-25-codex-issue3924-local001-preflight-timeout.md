# LOCAL-001 exact-main V25 qualification attempt

- Status: `NO_RESULT / SOURCE_FIX_REQUIRED`
- Local execution issue: #3924
- Source correction: #3930
- Parent qualification: #72 / `LOCAL-001`
- Tested exact Git SHA: `fba0342bf4cbd528fedffb475e6a30f9b68f3e6e`
- Branch: `agent/local003/issue-3924-local001-v25-current-main`
- Host available: licensed BricsCAD V25.2.10, Windows x64
- Runtime launched: `false`
- Plugin SHA-256: not produced

## Gates executed

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

## Disposition

This is a pre-runtime source-gate blocker, not a BricsCAD product verdict and not `LOCAL_PASS` or `LOCAL_FAIL`. The local worker did not change production, test, runner or workflow source and did not increase, bypass or disable the timeout. Zero BricsCAD processes remained after the attempt.

Source issue #3930 must preserve the exact registration checks while replacing repeated repository-wide scans with a bounded deterministic index or equivalent source-safe correction. After that correction is merged, publish a new exact source-ready `main` SHA and rerun the unchanged canonical LOCAL-001 runner once. The existing full interactive/private-DWG matrix remains pending and the machine-checkable closeout must continue to fail closed.

Raw qualification JSON and any future runtime artifacts remain Git-ignored. This sanitized claim contains no proprietary binary, private/customer drawing, machine path, ProjectId, CAD Handle, fingerprint or raw exception detail.
