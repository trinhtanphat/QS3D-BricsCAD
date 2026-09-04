# V26 generated-script template admission

## Scope

`scripts/new-v26-script-from-v25.ps1` is the shared build/release parity boundary that derives the V26 installer, uninstaller, updater, signed-package finalizer and update-manifest scripts from reviewed V25 templates.

This is REMOTE_SAFE source/build infrastructure. It does not establish licensed BricsCAD runtime acceptance and must never be reported as `LOCAL_PASS`.

## Admission contract

The selected V25 template pathname is validated as a repository-local ordinary non-reparse file, then opened once for read admission. The generator transforms only bytes captured from that admitted file handle.

While capture is active:

- the read handle does not share write/delete access;
- Win32 handle identity (`volume serial + file index`) is captured before and after the read;
- the resolved final handle path must equal the validated template path and remain stable;
- handle-reported length, stream length and captured byte length must agree;
- invalid UTF-8 fails closed using a strict decoder;
- SHA-256 evidence is computed from the same captured byte array that is decoded and transformed.

The template pathname is not reopened for either source text or source hashing after admission.

## Preserved V25→V26 parity

The host-major transform remains deliberately narrow: `V25/v25` becomes `V26/v26`. The generated V26 installer keeps its explicit additional `.runtimeconfig.json` payload requirement. Existing transformed-token validation remains authoritative for each supported template.

Output safety is unchanged: the destination must be a `.ps1` path distinct from the V25 source, its ancestor chain and existing leaf are checked for redirects, output is written to an ordinary staging file, and publication uses `File.Replace` or `File.Move` only after the final containment checks.

## Regression

Run:

```text
python scripts/preflight-v26-script-template-admission.py
```

The auto-discovered guard requires handle-bound source capture, pre/post identity/path/length fences, strict UTF-8 decoding, SHA-256 over captured bytes, transform ordering and the existing atomic output/parity invariants. It rejects reintroduction of `Get-Content -LiteralPath $sourceFull -Raw` or `Get-FileHash -LiteralPath $sourceFull` because those reopen the pathname after admission.

For protected integration, the exact candidate still requires current `preflight` and `core` success under repository CI policy. Actual V26 release publication/signing/runtime remains separately controlled by the V26 release workflow and its licensed/signing prerequisites.
