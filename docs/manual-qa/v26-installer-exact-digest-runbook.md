# V26 installer exact-digest release qualification

Status: REMOTE_SAFE qualification for C05 issue #5919.

## Safety invariant

A V26 cloud release or installer-cache priming run must not restore, acquire, qualify, cache, or publish against an installer generation unless the effective `BRICSCAD_V26_PINNED_MSI_SHA256` is one raw, exact 64-hex SHA-256 value. Authenticode signer and product-version checks remain defense in depth; they do not replace byte-generation identity.

The cache restore key must contain that exact digest. There must be no `mirror` fallback and no broad `restore-keys` prefix that can admit a cache created for another installer digest.

## Remote-safe cases

1. Dispatch with `confirm_release=RELEASE`, with neither `installer_sha256` nor repository `BRICSCAD_V26_MSI_SHA256` set. Expected: fail in exact-digest admission before cache restore, acquisition, packaging, or publication.
2. Dispatch cache priming with no effective digest. Expected: same fail-closed behavior before cache restore/acquisition.
3. Supply a 63-character digest, 65-character digest, or any non-hex character. Expected: admission fails before cache restore.
4. Supply a syntactically valid digest with leading or trailing whitespace. Expected: admission fails; the workflow must not trim/normalize the value and then use a different raw identity in cache/acquisition.
5. Supply a valid 64-hex digest for the wrong MSI generation. Expected: exact cache lookup cannot fall back to another digest; acquisition must reject the acquired bytes on SHA mismatch; no release is published.
6. Supply the correct exact digest. Expected: exact cache restore/acquisition path proceeds, existing Authenticode/product checks still run, and any cache save remains keyed by `steps.acquire.outputs.sha256`.
7. Inspect the workflow source. Expected: no `|| 'mirror'` / `|| "mirror"` installer cache-key fallback and no broad `restore-keys` prefix under the V26 installer restore step.

## Windows / shell qualification

The validation step runs under PowerShell on the Windows release runner. Treat the digest as `[string]$env:BRICSCAD_V26_PINNED_MSI_SHA256` without `.Trim()` or case/path transformation; regex-match the entire raw value with `^[0-9A-Fa-f]{64}$`. Do not embed filesystem paths into the digest expression. Existing acquisition path quoting remains unchanged.

## Regression gate

`scripts/preflight-v26-installer-exact-digest.py` is auto-discovered by Shared CI. Its mutation probes must reject: a relaxed digest regex, whitespace normalization, an unbound `mirror` cache fallback, and a broad cross-digest restore prefix.
