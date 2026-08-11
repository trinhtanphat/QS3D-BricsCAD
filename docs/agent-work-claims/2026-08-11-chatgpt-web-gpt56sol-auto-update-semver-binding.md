# Agent Work Claim — auto-update product SemVer binding hardening

- Claim ID: `AUTO-UPDATE-PRODUCT-SEMVER-BINDING-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T21:17:20+07:00`
- Released: `2026-08-11T21:27:30+07:00`
- Baseline main SHA: `5189d11a7658e2a2ff8566c7bb8a48db7c7629cd`
- Parent lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811` (`RELEASED`)

## Verified defect

The plugin already performed strict GitHub SemVer selection before one-click scheduling, but `update-v25.ps1` ultimately authorized the package by `AssemblyVersion`. Prereleases such as `0.1.0-preview.2` and `0.1.0-preview.3` can intentionally share `AssemblyVersion 0.1.0.0`. The existing `-AllowSameVersion` compatibility handoff therefore permitted newer prereleases but did not independently prove that the downloaded package product SemVer was newer than the installed product SemVer.

That left a replay/downgrade gap within one AssemblyVersion family: a same-publisher package with an older `productVersion` could satisfy the assembly-version layer unless the updater also bound and compared product SemVer.

## Reserved scope

This lane hardened only:

- `scripts/new-v25-update-manifest.ps1`
- `scripts/update-v25.ps1`
- `scripts/preflight-update-product-version-binding.py`
- this claim file

It intentionally did not edit `SecureUpdateLauncher.cs` or `scripts/preflight-auto-update.py`, which were concurrently reserved by the independent updater Authenticode-verification lane.

## Completed hardening

1. **Manifest schema 2 / signed product identity** — `4e523ab3ded0576c2517de5f6bd6753d5637b86e`
   - generated update manifests now use `schemaVersion = 2`;
   - include canonical `productVersion`;
   - require `PACKAGE-METADATA.productVersion` to be strict SemVer;
   - require metadata productVersion to equal `FileVersionInfo.ProductVersion` from the Authenticode-verified QS3D plugin DLL before manifest generation;
   - retain ZIP/staged payload equality, assembly-version and publisher checks.

2. **Monotonic product SemVer authorization** — `d3b574602525c0e2345a42f787d17a86a7101262`
   - `update-v25.ps1` consumes schema 2 and requires manifest `productVersion`;
   - strict SemVer parsing/comparison mirrors the plugin's stable/prerelease precedence including numeric prerelease ordering and leading-zero rejection;
   - target product SemVer must be strictly newer than installed product SemVer;
   - `-AllowSameVersion` now applies only to equal `AssemblyVersion` compatibility and explicitly cannot authorize same-product replay/repair or downgrade;
   - downloaded `PACKAGE-METADATA.productVersion`, signed DLL ProductVersion and manifest productVersion must match exactly;
   - AssemblyVersion, SHA-256, Authenticode, archive-safety, host allowlist and atomic installer checks remain independent mandatory layers;
   - installed assembly/product versions are re-read before installer invocation so concurrent/stale updater races fail closed.

3. **Independent regression gate** — `95ac79d29c3ffd87934a441cc88e3b0b2783da51`
   - added auto-discovered `scripts/preflight-update-product-version-binding.py`;
   - locks schema 2 generation, strict product SemVer validation, product downgrade/replay rejection, signed-DLL/metadata/manifest binding and preservation of AssemblyVersion checks;
   - intentionally avoids the Authenticode lane's `scripts/preflight-auto-update.py` reservation.

## Coordination result

The separate `updater Authenticode verification` lane completed independently and now requires `WinVerifyTrust` before the running plugin's signer thumbprint can become the one-click trust anchor. Together the two lanes provide complementary controls: valid running-publisher trust plus monotonic signed product-version binding.

After this lane's regression commit, another agent opened `updater-manifest-v2-compat` from a stale interpretation claiming the updater still rejected schema 2. Current `main` was re-read: `update-v25.ps1` already contains `if ($schemaVersion -ne 2)` and the full product SemVer checks from `d3b57460...`. Because that later claim overlaps this lane's script paths, this lane made no further script writes after discovering the overlap.

## Validation / evidence

- Re-fetched current `main` after implementation and confirmed `update-v25.ps1` still requires schema 2, strict target-vs-installed product SemVer advancement, exact downloaded metadata/signed-DLL/manifest productVersion equality, and pre-install concurrent-state recheck.
- Compare from `95ac79d29c3ffd87934a441cc88e3b0b2783da51` to then-current `main` reported `behind_by: 0`; subsequent commits were preserved without force/rebase/reset.
- No GitHub Actions workflow was dispatched.
- No Windows PowerShell/BricsCAD signed runtime was available in this connector session, so no native execution PASS is claimed.
- Existing `LOCAL-009 — clean-machine install/sign/update qualification` remains the production/runtime boundary. Its signed update matrix should include same-AssemblyVersion prerelease advancement (for example preview.2 -> preview.3) plus replay/downgrade rejection of an older productVersion under the same AssemblyVersion.

## Result

Source-side product-version authorization is complete and released on `main`: one-click updates can still advance prereleases that share an AssemblyVersion, but the package must carry a strictly newer product SemVer and that SemVer must be bound consistently across the manifest, package metadata and signed plugin payload. Native signed-machine qualification remains `LOCAL-009 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
