# Room Finish Schedule modeless publication — LOCAL_ONLY V25 qualification

This runbook qualifies the source contract carried by issue #4847 after it is merged to an exact source-ready SHA. Hosted CI proves only source/static/compile behavior; it is never licensed BricsCAD runtime evidence.

## Preconditions

- Fetch/build the exact intended source-ready SHA and record plugin binary/ProductVersion identity.
- Use an authorized disposable V25 drawing pair A/B with valid QS3D projects and representative HT_Phòng rows.
- Obtain the canonical #72 licensed allocation and literal `OFFLINE_FREEZE`/host authorization required by the live local qualification contract.
- Start from zero owned QS3D modeless Room Finish Schedule windows and preserve user drawings/settings.

## Matrix

1. **A / first publication** — activate drawing A, run `QS3DFINISHSCHEDULE`; require exactly one loaded Room Finish Schedule surface and correct drawing-A title/data.
2. **A / repeated invocation** — invoke the command repeatedly without closing the surface; require the same loaded surface to be activated, with no second Room Finish Schedule window and no duplicated retained document lifetime.
3. **A / refresh + export affinity** — refresh/filter and export XLSX while A is active; require current A project data, existing finite/compensated totals behavior, and the existing A-derived export filename semantics.
4. **Cross-DWG replacement** — activate drawing B and invoke the command; require A's surface to reach terminal Closed before exactly one B-bound surface becomes published. No simultaneous authoritative A/B Room Finish Schedule surfaces are allowed.
5. **Close arbitration** — exercise any available host/user condition that vetoes or interrupts close; if the old surface remains loaded, the command must fail closed and must not publish a replacement.
6. **Stale/unloaded recovery** — close the published surface normally, then invoke again; require one fresh surface with no stale publication blocking it.
7. **Document lifetime** — close the source drawing while its schedule is open; require `DocumentBoundWindowLifetime` teardown to close/unload the surface and allow a later command from another live drawing to publish normally.
8. **Save/reopen control** — save/close/reopen the disposable drawing and repeat first/repeated invocation plus refresh. Require no stale schedule owner from the prior document wrapper.
9. **Cleanup** — close all QS3D windows/drawings used by the matrix and verify no owned process/private-state residue beyond the repository's normal V25 qualification allowances.

## Evidence and verdict

Record exact SHA, ProductVersion/plugin hash, BricsCAD V25 version, drawing identities in sanitized form, and one result per matrix row. Publish only `PASS`, `FAIL`, or `NO_RESULT` under the canonical #72 flow. A hosted build, source preflight, or successful preview publication must not be promoted to `LOCAL_PASS`.
