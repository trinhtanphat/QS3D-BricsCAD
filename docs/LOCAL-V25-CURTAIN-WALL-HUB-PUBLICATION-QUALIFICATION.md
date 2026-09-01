# LOCAL V25 Curtain Wall Hub Publication Qualification

Status: `LOCAL_ONLY / SOURCE_READY / NO_RESULT` until executed in licensed BricsCAD V25 on the exact candidate.

Use the exact pushed/package SHA and an exclusive BricsCAD host. Hosted compile/source guards are not `LOCAL_PASS`.

Qualification matrix:

- CW01 normal open: `QS3DCURTAIN` opens exactly one Curtain Wall Hub and publishes the exact active DWG owner.
- CW02 same-DWG repeat: invoke again after publication; existing Hub activates and no second window is created.
- CW03 reentrant publication: inject/reproduce a second invocation while `ShowModelessWindow` has not returned; pending ownership must reject the second candidate.
- CW04 cross-DWG pending: switch/invoke from another DWG while the first candidate is pending; no second candidate may publish.
- CW05 cross-DWG published replacement: invoke from another DWG after publication; old Hub must reach terminal Closed before replacement publishes.
- CW06 close veto/failure: make old Hub remain loaded or throw on close; replacement must fail closed and preserve the existing owner.
- CW07 close during publication: close the candidate before promotion; pending ownership must release and no stale published owner may remain.
- CW08 host-show failure: inject a host exception from modeless show; candidate ownership must release, candidate close is best-effort, and no exception escapes.
- CW09 reporting failure: inject Palette status failure and Editor `WriteMessage` failure on the command-error path; both must be non-escaping.
- CW10 redaction: injected host/native exception details, paths and stack text must not appear in Palette or command-line output.
- CW11 wrapper drift: with the same native database but a different managed Document wrapper, published ownership must not be treated as the exact reusable owner.
- CW12 cold lifecycle: close/reopen drawings and repeat open/replace/close operations; no stale pending/published owner or duplicate Hub may survive.

For each cell record exact package/source SHA, BricsCAD V25 version, sanitized transcript/window evidence, active drawing identity and cleanup outcome. Runtime PASS requires all applicable cells on the exact artifact.
