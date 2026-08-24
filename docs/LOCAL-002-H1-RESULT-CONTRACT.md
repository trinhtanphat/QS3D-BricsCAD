# LOCAL-002 H.1 licensed-result contract

Lane-Key: `issue-3656`

This document defines the **sanitized closeout boundary** for the licensed BricsCAD V25 LOCAL-002 H.1 modeless multi-DWG/final-host qualification owned by #3593 and diagnosed remotely through #3621.

It does **not** replace the existing private/ignored H.1 A/B/C + exact-final-host harness. The private harness remains the source of runtime truth. This contract only gives the local worker a deterministic, fail-closed way to summarize that truth without publishing raw paths, customer/private data, dumps, Handles, ProjectIds, proprietary binaries or unsanitized stack traces.

## Current disposition

Licensed P06 already ran against exact runtime source:

`ec4384eb6a12ff6763dfdd19d4e4b84747ab60f3`

The functional matrix passed completely — A `13/13`, B `2/2`, managed-wrapper drift/native database identity, C-bound windows, dynamic hubs, project isolation and repeat cycle. Final-host acceptance still failed with exact-PID `ucrtbase.dll / c0000409` Application Error, WER `BEX64`, and a BricsCAD `C0000005` report in `brx25.dll` whose normalized reactor/WPF family matched P05.

Therefore P06 is **`FAIL / SOURCE_DIAGNOSIS_REQUIRED`**. Do not rerun the unchanged P06 binary. #3593 and #3621 stay open while a genuinely new source correction is developed and merged.

The local licensed host is free to continue independent P0 work; if #1744 is not already inside a bounded run, it is the next prepared P0 row. #3613 is a P1 fallback after higher-priority executable P0 work. Passing or failing H.1 does not by itself close umbrella #72.

## Validator

The repository validator is:

```text
scripts/validate-local002-h1-result.py
```

The static regression/preflight is:

```text
scripts/preflight-local002-h1-result-contract.py
```

The validator consumes one **already-sanitized JSON object** and emits one small routing JSON object. It never reads raw WER dumps, BricsCAD crash reports, private DWGs or machine-local capture roots.

Example from PowerShell after a future exact-SHA licensed run:

```powershell
$sha = (git rev-parse HEAD).Trim()
python .\scripts\validate-local002-h1-result.py `
  --input .\artifacts\local-v25-qualification\local002-h1-result.sanitized.json `
  --expected-sha $sha
```

The file under `artifacts/` remains local/ignored evidence. Only the validator's allowlisted routing output and a concise sanitized summary should be copied into GitHub.

## Schema

`schemaVersion` is fixed to:

```text
qs3d.local002-h1-result/v1
```

`lane` is fixed to:

```text
LOCAL-002-H1
```

Required top-level fields:

| Field | Contract |
|---|---|
| `schemaVersion` | exact schema string above |
| `lane` | `LOCAL-002-H1` |
| `attempt` | bounded attempt identifier such as `P07` |
| `verdict` | `PASS`, `FAIL`, or `NO_RESULT` |
| `exactSha` | full 40-character tested Git SHA |
| `bricscadProductVersion` | numeric V25 ProductVersion, for example `25.2.10.1` |
| `pluginSha256` | exact loaded adapter SHA-256 |
| `coreSha256` | exact loaded Core SHA-256 |
| `precheck` | focused guards/build/Core/SourceLink/zero-process evidence |
| `functional` | A/B/C + wrapper drift/dynamic/project/repeat acceptance |
| `finalHost` | exact-host shutdown and exact-PID Windows event counts |
| `safety` | fixture/user-DWG/DemandLoad/private-state/process/tree evidence |
| `noResult` | required only for `NO_RESULT` |

Unknown fields are rejected. This is intentional: a new/raw field must be explicitly reviewed before it can become publishable evidence.

### `precheck`

Required keys:

- `focusedGuardsPassed`
- `focusedGuardsTotal`
- `v25BuildWarnings`
- `v25BuildErrors`
- `helperBuildWarnings`
- `helperBuildErrors`
- `coreSmokePass`
- `sourceLinkExact`
- `zeroBricscadProcessesBefore`

`PASS` and `FAIL` are valid licensed product verdicts only after all focused guards pass, both builds are `0 warnings / 0 errors`, Core smoke passes, SourceLink resolves the exact candidate and no BricsCAD process is present before launch. A failure before those prerequisites is a `NO_RESULT`/precheck outcome, not a product-runtime verdict.

### `functional`

Required keys:

- `aBound`: `{ status, closed, expected }`, with `expected=13`
- `bBound`: `{ status, closed, expected }`, with `expected=2`
- `wrapperDriftNativeIdentity`
- `cBound`
- `dynamicHubs`
- `projectIsolation`
- `repeatCycle`

Statuses are `PASS`, `FAIL`, or `NOT_RUN`. `PASS` requires A `13/13`, B `2/2` and every remaining functional row `PASS`.

### `finalHost`

Required keys:

- `status`
- `hostMatched`
- `processExitCode`
- `gracefulExit`
- `applicationErrorCount`
- `werCount`
- `applicationHangCount`
- `dotNetRuntimeErrorCount`
- `accessViolationCount`

A `FAIL` additionally requires a sanitized `failure` object containing only:

- `class`
- `faultModule`
- `exceptionCode`
- `werEventName`
- `bricscadReportCode`
- `signatureFamily`

These are coarse classification tokens/module basenames only. Do not put a path, dump name, user/machine name, raw stack, raw exception text or raw report body in them.

A `PASS` requires the exact host to be matched, graceful final shutdown and **zero** exact-PID Application Error, WER, Application Hang, `.NET Runtime` and AccessViolation evidence.

### `safety`

Required keys:

- `publicFixtureUnchanged`
- `protectedUserDwgUnchanged`
- `demandLoadLoaderUnchanged`
- `demandLoadBytesUnchanged`
- `loadCtrls`
- `privateStateRestored`
- `zeroBricscadProcessesAfter`
- `zeroHelperProcessesAfter`
- `trackedTreeClean`
- `rawEvidenceIgnored`
- `sanitizedOnly`

A validated H.1 result requires every boolean above to be `true` and `loadCtrls=2`. A product observation that leaves unsafe residue is not publishable closeout evidence until cleanup/restoration is complete.

### `NO_RESULT`

`NO_RESULT` is for a bounded attempt that never produced a trustworthy product verdict. Its `noResult.reasonCode` must be one of the validator's allowlisted coarse causes, such as startup timeout, harness failure, missing marker, host mismatch or precheck failure.

Do not convert `NO_RESULT` to PASS. Repair only the bounded startup/harness/precheck condition, then retry under the same exact-SHA rule unless source itself changes.

## Routing

| Validated verdict | Validator route | What happens next |
|---|---|---|
| `PASS` | `LOCAL_PASS_ELIGIBLE` | Publish sanitized evidence to #3593; only then close #3593 as completed. Reconcile/close the current source issue only if its own acceptance is fully satisfied. Update canonical LOCAL-002 state and continue the next P0 row. |
| `FAIL` | `SOURCE_DIAGNOSIS_REQUIRED` | Keep #3593/source issue open. Publish the smallest sanitized failure classification to the source lane. Do not patch production source from the local evidence branch and do not rerun an unchanged binary. |
| `NO_RESULT` | `BOUNDED_RETRY_REQUIRED` | Keep issues open. Fix only the bounded environment/startup/harness/precheck problem and retry. Never reinterpret the attempt as product PASS/FAIL. |

`LOCAL_PASS_ELIGIBLE` means **the sanitized manifest is complete enough to support an already-observed licensed PASS**. It does not create licensed evidence and it is not sufficient by itself to close #72 or qualify a customer release.

## Minimal sanitized templates

A local helper may construct the manifest from ignored private evidence, but the resulting JSON must contain only the allowlisted schema. The following shapes illustrate the routing contract; they are not runtime evidence.

### PASS shape

```json
{
  "schemaVersion": "qs3d.local002-h1-result/v1",
  "lane": "LOCAL-002-H1",
  "attempt": "P07",
  "verdict": "PASS",
  "exactSha": "0000000000000000000000000000000000000000",
  "bricscadProductVersion": "25.2.10.1",
  "pluginSha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "coreSha256": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
  "precheck": {
    "focusedGuardsPassed": 16,
    "focusedGuardsTotal": 16,
    "v25BuildWarnings": 0,
    "v25BuildErrors": 0,
    "helperBuildWarnings": 0,
    "helperBuildErrors": 0,
    "coreSmokePass": true,
    "sourceLinkExact": true,
    "zeroBricscadProcessesBefore": true
  },
  "functional": {
    "aBound": {"status": "PASS", "closed": 13, "expected": 13},
    "bBound": {"status": "PASS", "closed": 2, "expected": 2},
    "wrapperDriftNativeIdentity": "PASS",
    "cBound": "PASS",
    "dynamicHubs": "PASS",
    "projectIsolation": "PASS",
    "repeatCycle": "PASS"
  },
  "finalHost": {
    "status": "PASS",
    "hostMatched": true,
    "processExitCode": "0",
    "gracefulExit": true,
    "applicationErrorCount": 0,
    "werCount": 0,
    "applicationHangCount": 0,
    "dotNetRuntimeErrorCount": 0,
    "accessViolationCount": 0
  },
  "safety": {
    "publicFixtureUnchanged": true,
    "protectedUserDwgUnchanged": true,
    "demandLoadLoaderUnchanged": true,
    "demandLoadBytesUnchanged": true,
    "loadCtrls": 2,
    "privateStateRestored": true,
    "zeroBricscadProcessesAfter": true,
    "zeroHelperProcessesAfter": true,
    "trackedTreeClean": true,
    "rawEvidenceIgnored": true,
    "sanitizedOnly": true
  }
}
```

### Current P06 failure classification shape

The current P06 failure can be represented without raw crash material using a coarse object like:

```json
{
  "class": "FINAL_HOST_NATIVE_WPF_TEARDOWN",
  "faultModule": "ucrtbase.dll",
  "exceptionCode": "0xc0000409",
  "werEventName": "BEX64",
  "bricscadReportCode": "C0000005",
  "signatureFamily": "ACRX_WPF_TEARDOWN"
}
```

That classification is enough to route the result back to #3621. The ignored raw evidence remains local.

## After P06 / before the next H.1 rerun

1. **Remote/source #3621** owns the next source hypothesis. It must add RED coverage for that hypothesis, land a normal branch/PR fix and obtain protected CI.
2. **#3593 stays parked**. Do not create P07 or spend licensed time on H.1 until a genuinely new merged source SHA is explicitly repinned.
3. **The licensed host continues independent P0 work**. #1744 is already prepared on an exact-main carrier; finish it first if it was already in progress, otherwise it is the next high-value P0 row after terminal P06. #3613 remains a P1 fallback.
4. When a new H.1 source fix lands, create/repin exactly one next bounded carrier, verify its HEAD equals the intended merged `main`, rebuild exact binaries and rerun the unchanged H.1 acceptance.
5. Feed the new sanitized result through this validator before publishing closeout metadata.

This keeps expensive licensed-host time focused on runtime facts while ordinary source diagnosis, regression guards, CI and merge work remain remote/source responsibilities.
