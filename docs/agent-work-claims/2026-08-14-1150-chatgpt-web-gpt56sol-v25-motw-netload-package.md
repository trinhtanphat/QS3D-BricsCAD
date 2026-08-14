# Work claim — V25 Mark-of-the-Web manual NETLOAD recovery

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T11:50:00+07:00`
- Completed: `2026-08-14T12:08:00+07:00`
- Baseline main SHA: `674aa692e92255a112dc1ea906614d54183af33a`
- Implementation checkpoint SHA: `51d0fe4872fe0a6610a19e4aae9344efd64314fd`
- Priority: owner-reported BricsCAD V25 NETLOAD failure with `.NET Framework` "Operation is not supported" when loading the release DLL from an extracted Desktop package.

## Reserved scope

Harden the V25 release-package/manual-NETLOAD path against Windows Mark-of-the-Web (`Zone.Identifier`) failures without weakening BricsCAD or PowerShell security. Add a package-local one-click recovery helper, package it deterministically, keep signed-release executable coverage complete, document the safe install/manual fallback, and add a focused static regression guard for this distribution contract.

## Implemented

- `e903f6342ecdf53d852f52f57ee6addb342b61cc` — added `scripts/unblock-v25-netload.ps1`; it validates safe manifest paths, every SHA-256 entry, complete package-file coverage, required V25 package identity files and the `QS3D` command before any recursive `Unblock-File` operation.
- `71b5feb8523e8f6f3271919f1c2142ff8bfee83b` — added `scripts/UNBLOCK-QS3D.cmd`; one `RemoteSigned` / noninteractive PowerShell process verifies the packaged helper hash and Authenticode status before unblocking/bootstrap, then invokes the integrity-first package helper.
- `0e9726a822ae5c2d0d8d236ca4d498dd52681457` — packages both recovery files before `SHA256SUMS.txt` generation and embeds explicit `Operation is not supported` / `0x80131515` recovery guidance in V25 `README.txt`.
- `ad0a1a48ec102b027cbc80e7954a8afe1fde7269` — extended the existing install/update UX preflight to guard the recovery launcher/helper/package contract.
- `fa52108320c7e0b9708a028401e3eccaa6169063` — documented the Mark-of-the-Web failure boundary and recovery path in repository `README.md`.
- `252cd2abffe45f3389438be32ff1e971af9f40e1` — added `unblock-v25-netload.ps1` to signed-package finalization so stable signed V25 packages require its Authenticode signature and record it in signed executable metadata.
- `2035f299ab002c21f43dc1bae3c857801facf04f` — added the recovery helper to both Authenticode sign and verify payload lists in the manual V25 release workflow; no workflow was dispatched by this lane.
- `51d0fe4872fe0a6610a19e4aae9344efd64314fd` — locked signed recovery-helper sign/verify/finalization coverage in `preflight-update-install-ux.py`.

## Validation evidence

Exact-source GitHub readback confirms:

- `unblock-v25-netload.ps1` performs package integrity/coverage/identity validation before recursively removing Mark-of-the-Web and does not change BricsCAD security/trusted-path or PowerShell execution-policy settings.
- `UNBLOCK-QS3D.cmd` uses `ExecutionPolicy RemoteSigned`, rejects a helper hash mismatch, inspects Authenticode state, and unblocks the helper only after hash verification.
- `package-v25.ps1` copies `UNBLOCK-QS3D.cmd` and `unblock-v25-netload.ps1` into the package before the package hash manifest is generated and documents whole-dependency-folder recovery rather than unblocking only the primary DLL.
- `.github/workflows/release-v25.yml` includes the recovery helper in both Authenticode signing and signature verification payload arrays.
- `finalize-v25-signed-package.ps1` includes the recovery helper in `$SignedPayloadNames` and therefore in `signedExecutablePayload` metadata.
- `preflight-update-install-ux.py` statically guards the integrity-first recovery path and signed-helper coverage.

Focused preflight execution is `NOT_RUN` in this connector-only source lane because no executable repository checkout was available to this session. GitHub Actions are also `NOT_RUN` and were deliberately not dispatched under the repository manual-only CI policy. No PASS is fabricated for either layer.

Licensed BricsCAD V25 runtime validation is `NOT_RUN` here. The source/distribution fix is complete on `main`, but the exact rebuilt package still requires a Windows/BricsCAD V25 customer-machine acceptance check: extract the newly built package, run `INSTALL-QS3D.cmd` (preferred), or intentionally run `UNBLOCK-QS3D.cmd` before manual `NETLOAD`, then confirm the previous .NET loader error is absent.

## Excluded scope

- `scripts/install-v25-autoload.ps1` behavior; its existing verified-install path already unblocks copied payloads and remains unchanged
- plugin startup/lifecycle code after an assembly has successfully loaded
- V25/V26 product-version and release-tag synchronization automation
- V26 package behavior
- updater release-channel behavior
- BricsCAD security/trusted-path relaxation
- GitHub Actions dispatch/release publication
- licensed BricsCAD runtime qualification

## Coordination

The earlier V25 preview package/run-140 claim uses a non-reserving `SOURCE_FIXED / AUTOMATION_HARDENED / PENDING_FRESH_CI` status and owns release-tag/version synchronization rather than this manual-NETLOAD MOTW recovery. Unrelated concurrent source/local-runtime lanes were preserved; this claim did not overwrite their paths.

## Completion result

The secure V25 manual-NETLOAD Mark-of-the-Web recovery path, signed-release coverage, package inclusion, documentation and regression guard are pushed to `main`. Remaining evidence is LOCAL_ONLY runtime acceptance of a package rebuilt from the resulting `main`; it is not an uncommitted source fix.
