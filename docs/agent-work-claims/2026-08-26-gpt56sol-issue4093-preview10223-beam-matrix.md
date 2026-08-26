# Work claim — #4093 preview.10223 Beam behavior matrix

- Status: `IN_PROGRESS / PREP_READY / NO_RESULT — LOCAL_RUNTIME_REQUIRED`
- Issue: `#4093`
- Parent local qualification issue: `#72`
- Source issue / PR: `#4043` / `#4047`
- Related NETLOAD smoke: `#4083` / `#4085`
- Lane-Key: `issue-4093`
- Canonical owner/session: `gpt56sol / owner-requested GitHub session`
- Canonical branch: `agent/gpt56sol/issue-4093-beam-preview10223-matrix`
- Exact registration baseline: `origin/main@32dbdab8847087a6efc2be48ef5a94f4c37bb783`

## Reserved scope

Qualify the Beam formwork behavior merged by PR #4047 on the exact published
BricsCAD V25 preview package that already passed the separate #4083/#4085
NETLOAD/runtime identity smoke.

This lane is behavior-only. It must not modify the #4085 smoke claim, and it
must not turn source/preflight coverage into a runtime PASS.

## Exact artifact pin

- Preview: `v0.1.0-preview.10223`.
- Exact release target/source: `1363f9be69ebc8ca8a865ccdd41639346f55f6ee`.
- Required source ancestor: PR #4047 merge
  `3d13f9f84a33819164beffdc2a90673f31c215c0`.
- Official V25 ZIP SHA-256:
  `A83BC92A1F90B00ADF7DFE0B1C92DF2EF7A3286D7ED99E4307ED8E0B87F22222`.
- Exact packaged `QS3D.BricsCAD.V25.dll` SHA-256:
  `3F0156A8DFD9BB31ECE43665D5D8334DA320172A6EAFB929967268218168F22F`.
- Required host: licensed interactive BricsCAD `25.2.10` x64.

The product DLL is immutable for this qualification. A companion harness may
drive or inspect the exact loaded product assembly, but rebuilding, replacing,
or instrumenting `QS3D.BricsCAD.V25.dll` invalidates the preview.10223 runtime
claim. The lane must fail closed on package, assembly-path, version, hash, or
native-host identity mismatch.

## Beam fixture and behavior matrix

Canonical Beam fixture: `B=0.30 m`, `H=0.50 m`, plan vector `5.0 m x 5.0 m`,
therefore `L=sqrt(50)=7.0710678 m`.

| Cell | Runtime rule / observation | Expected result |
| --- | --- | --- |
| M1 | Side ON / Bottom OFF | gross `~= 7.0710678 m²` |
| M2 | Side ON / Bottom ON | gross `~= 9.1923881 m²` |
| M3 | Top face contribution | `0 m²`; never included |
| M4 | End / Other contribution | `0 m²`; never silently classified as Side |
| M5 | M1 plus Side deductions `0.15 + 0.15 m²` | net `~= 6.7710678 m²` |
| M6 | M2 plus the Side deductions and Bottom deduction `0.09 m²` | net `~= 8.8023881 m²` |
| M7 | Quantity Insight aggregate vs Detail exact-face ledger | same net `FormworkM2` |
| M8 | Diagonal 5 m x 5 m Beam classification | End caps stay out of Side; horizontal faces classify Top/Bottom from live Z bounds |

Numeric comparisons must use the fixed Beam-lane tolerance; the expected values
themselves must not be weakened or recalculated from a fallback implementation
to make a failing cell pass.

## Runtime execution boundary

Qualification requires all of the following in one exact-artifact evidence
chain:

1. start from zero pre-existing BricsCAD processes;
2. verify preview tag/source, package digest and packaged V25 DLL hash before
   host launch;
3. isolate startup so an installed DemandLoad registration cannot preload a
   different QS3D assembly;
4. NETLOAD the exact packaged V25 DLL and verify the loaded assembly path/hash,
   BricsCAD V25 native identity and interactive session;
5. exercise M1 through M8 in the real host and capture sanitized per-cell
   rules, face classes/contributions, gross/net totals and M7 parity evidence;
6. restore all scoped profile/Loader/DemandLoad state and remove nonce state;
7. finish with zero test-owned BricsCAD processes and no scoped residue.

The existing `scripts/test-bricscad-v25-runtime.ps1` / `QS3DRUNTIMEPROBE`
identity smoke is reusable for exact-host setup and cleanup, but by itself it
does not exercise M1–M8 and therefore cannot satisfy this lane.

## GitHub-side preparation completed

The carrier now includes two behavior-specific, product-DLL-safe artifacts:

- `docs/LOCAL-V25-BEAM-FORMWORK-MATRIX.md` — exact-artifact runbook for the
  licensed local M1–M8 execution, evidence hygiene and cleanup boundary.
- `scripts/test-local-v25-beam-formwork-matrix-evidence.ps1` — strict sanitized
  evidence verifier pinned to preview.10223, source SHA, ZIP digest and packaged
  DLL digest. It verifies V25 interactive/NETLOAD identity, the canonical Beam
  geometry, all M1–M8 expected values/parity, cleanup, zero blockers and a
  positive attestation that the matrix was actually exercised.

The verifier is intentionally incapable of converting source inspection or the
existing NETLOAD smoke into a behavior PASS. It requires real local evidence
and rejects mismatched artifact identity, missing PASS cells, changed regression
numbers, nonzero Top/End/Other contribution, M7 mismatch, M8 classification
failure, incomplete cleanup or known blockers.

## Current evidence and result boundary

#4083/#4085 already prove `LOCAL_PASS / NETLOAD_SMOKE_ONLY` for the exact
preview.10223 package and exact packaged V25 DLL above. That evidence is
accepted only for package/runtime identity and cleanup provenance.

No M1–M8 cell has been exercised in a licensed interactive V25 host by this
GitHub-only preparation session. The repository connector can create and review
GitHub carriers, source and sanitized evidence, but it does not control the
licensed local BricsCAD process. Therefore the current behavior result is:

`NO_RESULT / LOCAL_RUNTIME_REQUIRED`

This status is intentional and fail-closed. Do not report
`LOCAL_PASS / BEAM_BEHAVIOR_MATRIX` from CI, source inspection, deterministic
preflight, the existing NETLOAD marker, or an analytically recomputed expected
value.

## Result vocabulary

- `LOCAL_PASS / BEAM_BEHAVIOR_MATRIX`: M1–M8 all pass on the exact artifact and
  cleanup passes.
- `LOCAL_FAIL / BEAM_BEHAVIOR_MATRIX`: at least one exercised cell violates the
  fixed contract; record the first concrete mismatch and preserve the expected
  value.
- `NO_RESULT / LOCAL_RUNTIME_REQUIRED`: the matrix has not yet been exercised
  on the exact licensed interactive host.
- `NO_RESULT / LOCAL_RUNTIME_BLOCKED`: an attempted local run cannot establish
  the required exact-artifact/host boundary.

## Exclusions

- no production-source patch in this behavior-evidence lane;
- no rebuild or replacement of the preview.10223 product DLL;
- no DemandLoad-install qualification;
- no signing qualification;
- no customer/private DWG qualification;
- no V26 behavior claim;
- no commercial-release qualification;
- no merge or direct write to `main`.

## Completion condition

The lane may move to COMPLETE only after sanitized evidence demonstrates that
M1–M8 were actually exercised against the exact preview.10223 packaged V25 DLL
in licensed interactive BricsCAD V25.2.10 x64, all cells meet the fixed
contract, scoped state is restored, and zero test-owned host processes/residue
remain. Any real behavior mismatch is a LOCAL_FAIL, not a reason to relax the
matrix.
