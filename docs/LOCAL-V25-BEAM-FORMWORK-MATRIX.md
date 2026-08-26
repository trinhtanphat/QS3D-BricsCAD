# Local V25 Beam formwork behavior matrix — issue #4093

This runbook qualifies the Beam formwork behavior merged by PR #4047 on the
**exact released preview bytes** that already passed the separate #4083/#4085
NETLOAD smoke.

This is a `LOCAL_ONLY` licensed-host qualification. GitHub Actions, source
inspection, deterministic preflight, or a rebuilt product DLL cannot replace
the interactive BricsCAD V25 execution required here.

## Immutable candidate

- Preview: `v0.1.0-preview.10223`
- Exact release target/source: `1363f9be69ebc8ca8a865ccdd41639346f55f6ee`
- Required #4047 merge ancestor:
  `3d13f9f84a33819164beffdc2a90673f31c215c0`
- Official V25 ZIP SHA-256:
  `A83BC92A1F90B00ADF7DFE0B1C92DF2EF7A3286D7ED99E4307ED8E0B87F22222`
- Packaged `QS3D.BricsCAD.V25.dll` SHA-256:
  `3F0156A8DFD9BB31ECE43665D5D8334DA320172A6EAFB929967268218168F22F`
- Required host: licensed interactive BricsCAD V25 x64; #4085 qualified
  V25.2.10 x64.

If any package/source/plugin/native-host identity differs, stop with
`NO_RESULT / LOCAL_RUNTIME_BLOCKED`. Do not rebuild, replace, instrument or
copy a different QS3D product DLL into the candidate and carry the result back
to preview.10223.

## Host boundary

Use the same startup-isolation and cleanup discipline as the canonical V25
runtime qualification:

1. require zero pre-existing BricsCAD processes before the owned run;
2. snapshot the scoped profile, QS3D Loader/LoadCtrls and DemandLoad state;
3. prevent an installed QS3D registration from preloading a different DLL;
4. NETLOAD the exact packaged preview.10223 V25 DLL and verify its loaded
   assembly hash before the matrix;
5. keep all raw host paths, registry data, license material and screenshots
   containing unrelated/private content outside git;
6. restore scoped state and prove zero test-owned BricsCAD processes/residue at
   the end.

A NETLOAD/runtime marker is prerequisite identity evidence only. It is not a
Beam matrix result.

## Canonical Beam fixture

Use a non-customer test drawing and a Beam with:

- width `B = 0.30 m`;
- height `H = 0.50 m`;
- plan delta `dx = 5.0 m`, `dy = 5.0 m`;
- length `L = sqrt(5^2 + 5^2) = 7.0710678 m`.

Keep the same Beam identity/geometry while toggling the formwork rules. If a
cell requires regeneration/refresh, complete that refresh before reading both
Detail and aggregate values.

## M1–M8 contract

| Cell | Real-host action / observation | PASS value |
| --- | --- | --- |
| M1 | `ExtractSide=true`, `ExtractBottom=false`; read gross formwork | `7.0710678 m²` |
| M2 | `ExtractSide=true`, `ExtractBottom=true`; read gross formwork | `9.1923881 m²` |
| M3 | Inspect exact-face ledger classification/contribution for Top | Top contribution `0 m²` |
| M4 | Inspect End and Other classifications/contributions | End `0 m²`; Other `0 m²`; neither silently becomes Side |
| M5 | M1 rules plus two directed Side deductions `0.15 + 0.15 m²` | Side deduction `0.30 m²`; net `6.7710678 m²` |
| M6 | M2 rules plus M5 Side deductions and Bottom deduction `0.09 m²` | net `8.8023881 m²` |
| M7 | Under the M6 rule/deduction state, compare Quantity Insight aggregate/table with Detail exact-face net | both `8.8023881 m²` |
| M8 | Inspect the same diagonal Beam's face classification evidence | diagonal axis resolved; End caps are not Side; horizontal classification uses live Z bounds |

Use numeric tolerance `1e-6 m²` for the verifier. Do not relax the expected
values to match an observed failure.

## Sanitized evidence schema

Write one strict UTF-8 JSON file no larger than 1 MiB. The Beam-specific
verifier requires these fields:

- top level: `schema`, `previewTag`, `sourceSha`, `zipSha256`, `pluginSha256`;
- `environment`: `windowsX64`, `interactive`, `licensedBricsCadV25`,
  `bricsCadProductVersion`, `loadMode`, `loadedPluginSha256`;
- `attestation`: `executedOnLicensedV25`, `sameExactReleasedPlugin`,
  `matrixActuallyExercised`, `sanitized`;
- `beam`: `widthM`, `heightM`, `deltaXM`, `deltaYM`, `lengthM`;
- `cells.M1`: `status`, `sideEnabled`, `bottomEnabled`, `grossM2`;
- `cells.M2`: `status`, `sideEnabled`, `bottomEnabled`, `grossM2`;
- `cells.M3`: `status`, `topContributionM2`;
- `cells.M4`: `status`, `endContributionM2`, `otherContributionM2`;
- `cells.M5`: `status`, `sideDeductionM2`, `bottomDeductionM2`, `netM2`;
- `cells.M6`: `status`, `sideDeductionM2`, `bottomDeductionM2`, `netM2`;
- `cells.M7`: `status`, `aggregateFormworkM2`, `detailNetFormworkM2`;
- `cells.M8`: `status`, `diagonalAxisResolved`,
  `endCapsClassifiedAsSide`, `horizontalClassificationUsesLiveZBounds`;
- `cleanup`: `profileRestored`, `loaderRestored`, `demandLoadRestored`,
  `zeroTestOwnedProcesses`, `noScopedResidue`;
- `knownBlockers`: an empty JSON array for PASS.

All `status` fields M1–M8 must be the exact string `PASS`. Boolean attestations
and cleanup properties must be real JSON booleans. `loadMode` must be
`NETLOAD`; `environment.loadedPluginSha256` must equal the immutable packaged
DLL digest.

Raw machine paths, UNC paths, `private/` or `customer/` path material are
rejected by the verifier.

## Verify evidence

From a PowerShell session in the repository checkout, after the interactive
run has produced the sanitized JSON:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-local-v25-beam-formwork-matrix-evidence.ps1 `
  -EvidencePath <sanitized-beam-matrix.json>
```

The script pins the preview tag, source SHA, ZIP digest and packaged plugin
digest; verifies the licensed/interative V25 attestation and exact loaded DLL;
checks the canonical Beam fixture; validates all M1–M8 values and parity; and
requires cleanup plus zero blockers.

Only a successful verifier result backed by the actual interactive run may be
recorded as:

`LOCAL_PASS / BEAM_BEHAVIOR_MATRIX`

If an exercised matrix cell violates the contract, record
`LOCAL_FAIL / BEAM_BEHAVIOR_MATRIX` with the first concrete mismatch. If the
host/artifact boundary cannot be established, record
`NO_RESULT / LOCAL_RUNTIME_BLOCKED`.

## Evidence hygiene and close-out

Do not commit the raw evidence file by default. Commit only a sanitized summary
that preserves exact tag/SHA/digests, host major/version, per-cell PASS/FAIL
facts, relevant numeric values, verifier result and cleanup result. Keep PR
#4094 draft until real M1–M8 evidence exists. This lane does not qualify
DemandLoad installation, signing, customer DWGs, V26, or a commercial release,
and it grants no authority to merge `main`.
