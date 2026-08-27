# Work claim — first V25 preview containing #4047 runtime

- Status: `COMPLETE / LOCAL_PASS — PREVIEW PACKAGE NETLOAD SMOKE`
- Issue: `#4083`
- Parent local qualification issue: `#72`
- Source issue / PR: `#4043` / `#4047`
- Lane-Key: `issue-4047-next-preview-v25-runtime`
- Canonical owner/session: `codex-local-20260826-01a03d8a`
- Canonical branch: `agent/codex/issue4083-preview4047-v25-runtime`
- Exact registration baseline: `origin/main@3d13f9f84a33819164beffdc2a90673f31c215c0`

## Reserved scope

Qualify the first canonical published BricsCAD V25 preview after
`v0.1.0-preview.10222` whose exact release target contains merge commit
`3d13f9f84a33819164beffdc2a90673f31c215c0` from PR #4047.

The lane will:

1. wait for that canonical preview instead of manufacturing a local release;
2. verify the release target, #4047 ancestry, official ZIP digest, checksum
   sidecar, package manifest, ProductVersion and DLL hash before host launch;
3. require an exclusive zero-process licensed BricsCAD V25 x64 host;
4. snapshot and restore the scoped profile, Loader and DemandLoad state;
5. `NETLOAD` the exact packaged `QS3D.BricsCAD.V25.dll` and verify exact loaded
   assembly identity plus the canonical runtime/Ribbon/Palette smoke;
6. retain only sanitized evidence and finish with zero test-owned BricsCAD
   processes and no scoped residue.

Beam formwork behavior from #4043 will be reported only if it is actually
exercised on the exact released artifact. Generic NETLOAD success is not a
full quantity-behavior qualification.

## Exact candidate and package evidence

- The first eligible canonical preview is `v0.1.0-preview.10223`, published by
  successful release workflow run `32962228330` at exact target/source
  `1363f9be69ebc8ca8a865ccdd41639346f55f6ee`.
- Git ancestry verification proves merge commit
  `3d13f9f84a33819164beffdc2a90673f31c215c0` from PR #4047 is an ancestor of
  the exact release target.
- Official `QS3D-BricsCAD-V25.zip` API digest, downloaded-byte hash and checksum
  sidecar all agree on SHA-256
  `A83BC92A1F90B00ADF7DFE0B1C92DF2EF7A3286D7ED99E4307ED8E0B87F22222`.
  The sidecar asset itself matches its API digest
  `409C5AD2D79202B29C0CA80C6715F5936E37453DB68B39853A61CBE892A5D394`.
- Archive inspection found 18 entries, no duplicate names and no path-traversal
  entry. The root `SHA256SUMS.txt` verified all 17 other regular files with
  exact set coverage and no missing, extra or mismatched payload.
- `PACKAGE-METADATA.json` records product version
  `0.1.0-preview.10223`, Git commit
  `1363f9be69ebc8ca8a865ccdd41639346f55f6ee`, target `BricsCAD V25 x64` and
  command count `536`.
- The exact packaged `QS3D.BricsCAD.V25.dll` records FileVersion
  `0.1.0.10223`, ProductVersion `0.1.0-preview.10223` and SHA-256
  `3F0156A8DFD9BB31ECE43665D5D8334DA320172A6EAFB929967268218168F22F`.

## Licensed V25 NETLOAD result

- Runner source was the unchanged canonical
  `scripts/test-bricscad-v25-runtime.ps1` on lane commit
  `71c54e92acaf6cb1b115c67157b74610a8a8560b`.
- The first diagnostic start failed closed as intended because the pre-existing
  `LoadCtrls=2` registration loaded the installed DLL before explicit NETLOAD;
  the runner rejected that different assembly path. This diagnostic is not
  counted as qualification evidence.
- With zero BricsCAD processes, the lane retained registry snapshots, cloned a
  nonce profile and guarded the installed registration from `LoadCtrls=2` to
  `LoadCtrls=4` for startup isolation. The second invocation NETLOADed the exact
  packaged DLL and produced `status=PASS` at
  `2026-08-26T11:30:58.3065861Z`.
- Sanitized in-host marker: BricsCAD `25.2.10`; CLR `4.0.30319.42000`; x64
  `true`; native runtime major/label `25` / `V25`; native match `true`; exact
  packaged assembly hash as above; Ribbon ready `true`; aggregate,
  Workspace and Right palettes visible `true`; Quantity palette visible
  `false` as required by the canonical probe.
- The runner recorded `load_mode=NETLOAD`, interactive session `true` and
  `PrintWindow(hwnd)` screenshot capture. Local visual review confirmed that
  the ignored image contains only the target BricsCAD window with the QS3D
  model and drawing/layer palettes visible. Its SHA-256 is
  `F2493F00BC43344FBD5886B395A14E4D51FC8A78CBF85255671087CE21A79332`.

## Cleanup and result boundary

- The runner-owned host exited and three spaced process samples were `0,0,0`.
- The guarded application registration was restored to its original Loader and
  `LoadCtrls=2`; the before/after QS3D application exports have identical
  SHA-256
  `47A4AF66D69E1588BA54E4D331EB1D52C342C9F4ACEA1CABDE1DF823B7E0112D`.
- The current profile returned to its pre-run value, both nonce profiles were
  removed, and a comparison of the full locale registry exports found zero
  differing lines. No test-owned BricsCAD process or scoped profile residue
  remains.
- Raw registry exports, marker metadata and the screenshot remain gitignored.
  Only these sanitized identities and aggregates are committed.
- Result is `LOCAL_PASS` for the exact preview.10223 package's licensed
  NETLOAD/runtime/Ribbon/Palette identity smoke only. Beam side/bottom/end/top
  quantity behavior was not exercised and is not claimed. DemandLoad install,
  signing, customer-DWG and commercial-release qualification also remain out
  of scope.

## Exclusions

- no production-source patch from this local lane;
- no manual Actions dispatch, rerun or cancel;
- no write or merge to `main`;
- no reuse of preview.10222;
- no private/customer drawing, raw CAD identity, license data or machine path
  in committed evidence;
- no signing, commercial-release or full customer-matrix claim.

## Completion condition

The exact eligible preview is published, its provenance and package bytes pass
verification, the exact packaged V25 DLL passes licensed NETLOAD plus canonical
runtime/Ribbon/Palette identity smoke, scoped host state is restored, zero
test-owned BricsCAD processes remain, and sanitized exact-tag/SHA/hash evidence
is committed and pushed on this branch for PR review.
