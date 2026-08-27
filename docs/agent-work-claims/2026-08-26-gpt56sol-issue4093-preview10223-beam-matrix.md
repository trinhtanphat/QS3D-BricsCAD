# Work claim — #4093 preview.10228 Beam behavior matrix

- Status: `COMPLETE / 100% / LOCAL_PASS / BEAM_BEHAVIOR_MATRIX`
- Issue: `#4093`
- Parent local qualification issue: `#72`
- Source issue / PR: `#4043` / `#4047`
- Historical NETLOAD smoke only: `#4083` / `#4085` on `.10223`; not transferable
- Lane-Key: `issue-4093`
- Canonical owner/session: `gpt56sol / owner-requested GitHub session`
- Canonical branch: `agent/gpt56sol/issue-4093-beam-preview10223-matrix`
- Retarget baseline: `origin/main@305daec904d9ae93ade1e0d907a3ec8269e5b105`
- Final pre-push sync: `origin/main@02ff7806738de2738ae978ce5c5bdce700c3a269`
- Carrier note: the canonical branch name retains `preview10223` to avoid a duplicate carrier; its current immutable runtime pin is `.10228`.

## Reserved scope

Qualify the Beam formwork behavior merged by PR #4047 on the exact published
BricsCAD V25 preview package selected by the owner's 2026-08-27 runtime
repoint. The `.10228` run must re-establish NETLOAD/runtime identity because
the separate #4083/#4085 smoke belongs only to `.10223`.

This lane keeps the #4085 `.10223` smoke claim historical and unchanged. It
must not turn source/preflight coverage into a `.10228` runtime PASS.

## Exact artifact pin

- Preview: `v0.1.0-preview.10228`.
- Exact release target/source: `7dacdce17a6403d19681732ca7bad22cdb6f1499`.
- Required source ancestor: PR #4047 merge
  `3d13f9f84a33819164beffdc2a90673f31c215c0`.
- Official V25 ZIP SHA-256:
  `EC7385FC6085A838B94F84FC20B77E61E728952CC3A580FEC695031280FBC39E`.
- Exact packaged `QS3D.BricsCAD.V25.dll` SHA-256:
  `010F729470B0644CD0ECBFF7395F4DCFAE39E81AA1B230C7219AC18C11C1340A`.
- Required host: licensed interactive BricsCAD `25.2.10` x64.

The product DLL is immutable for this qualification. A companion harness may
drive or inspect the exact loaded product assembly, but rebuilding, replacing,
or instrumenting `QS3D.BricsCAD.V25.dll` invalidates the preview.10228 runtime
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
  evidence verifier pinned to preview.10228, source SHA, ZIP digest and packaged
  DLL digest. It verifies V25 interactive/NETLOAD identity, the canonical Beam
  geometry, all M1–M8 expected values/parity, cleanup, zero blockers and a
  positive attestation that the matrix was actually exercised.

The verifier is intentionally incapable of converting source inspection or the
existing NETLOAD smoke into a behavior PASS. It requires real local evidence
and rejects mismatched artifact identity, missing PASS cells, changed regression
numbers, nonzero Top/End/Other contribution, M7 mismatch, M8 classification
failure, incomplete cleanup or known blockers.

## Current evidence and result boundary

#4083/#4085 prove `LOCAL_PASS / NETLOAD_SMOKE_ONLY` only for historical
preview.10223. They are useful procedure precedent but provide no identity or
behavior evidence for the exact preview.10228 package pinned above.

On 2026-08-27 the owner-directed local session exercised M1–M8 against the
exact `.10228` package in licensed interactive BricsCAD V25.2.10 x64. The
released adapter was NETLOADed unchanged, and an ignored companion verified
the loaded assembly path, ProductVersion and SHA-256 before invoking the
released `BeamFormworkQuantityPolicy` on a live native diagonal `Solid3d`.

- Core DLL SHA-256:
  `FF4801217E123275D1F2E92313C6FFF4A95DECCF3BBF656ACF5AB1FC580A1F86`.
- Ignored companion DLL SHA-256:
  `6AF2BD95E18B0DCB73DAAFB09D1A359AAC390FC5A72C4E670A89320B58942DF7`.
- M1 gross: `7.07106781186549 m²` — `PASS`.
- M2 gross: `9.19238815542513 m²` — `PASS`.
- M3 Top: `0 m²` — `PASS`.
- M4 End / Other: `0 / 0 m²` — `PASS`.
- M5 Side deduction / net: `0.30 / 6.77106781186549 m²` — `PASS`.
- M6 Side deduction / Bottom deduction / net:
  `0.30 / 0.09 / 8.80238815542513 m²` — `PASS`.
- M7 Aggregate / Detail:
  `8.80238815542513 / 8.80238815542513 m²` — exact parity, `PASS`.
- M8 native classes: `Side=2`, `End=2`, `Top=1`, `Bottom=1`, `Other=0`;
  diagonal axis resolved, End caps are not Side, and horizontal classes use
  live Z bounds — `PASS`.

The strict verifier returned:

`LOCAL_PASS / BEAM_BEHAVIOR_MATRIX: preview=v0.1.0-preview.10228, source=7dacdce17a6403d19681732ca7bad22cdb6f1499, pluginSha256=010F729470B0644CD0ECBFF7395F4DCFAE39E81AA1B230C7219AC18C11C1340A, BricsCAD=25.2.10, cells=8`

Cleanup restored the protected profile and exact QS3D DemandLoad tree, removed
the nonce profile, and completed ten stable zero-process samples. The sanitized
evidence had all cleanup booleans true and `knownBlockers=[]`. Two earlier
attempts invoked the UI-oriented `QS3DRUNTIMEPROBE` and failed before the Beam
command in BricsCAD/WPF palette layout; they produced no Beam PASS evidence and
were excluded. The qualifying command-only run re-established exact NETLOAD
identity inside the in-host companion without opening the unrelated palette.

Therefore the final behavior result is:

**`100% / LOCAL_PASS / BEAM_BEHAVIOR_MATRIX`**

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
- no rebuild or replacement of the preview.10228 product DLL;
- no DemandLoad-install qualification;
- no signing qualification;
- no customer/private DWG qualification;
- no V26 behavior claim;
- no commercial-release qualification;
- no merge or direct write to `main`.

## Completion condition

The lane may move to COMPLETE only after sanitized evidence demonstrates that
M1–M8 were actually exercised against the exact preview.10228 packaged V25 DLL
in licensed interactive BricsCAD V25.2.10 x64, all cells meet the fixed
contract, scoped state is restored, and zero test-owned host processes/residue
remain. Any real behavior mismatch is a LOCAL_FAIL, not a reason to relax the
matrix.
