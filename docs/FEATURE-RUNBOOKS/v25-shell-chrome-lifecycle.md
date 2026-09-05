# V25 shell-chrome lifecycle qualification

Issue: #5811  
Lane-Key: `issue-5811`

## Hosted/source acceptance

1. Run `python scripts/preflight-v25-shell-chrome-lifecycle.py`.
2. Run the repository aggregate feature guards.
3. Build `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` with the repository locked/trusted V25 references.
4. Treat hosted success as source/compile evidence only; it is not BricsCAD runtime evidence.

## LOCAL_ONLY BricsCAD V25 matrix

Bind every observation to the exact candidate SHA and package identity.

1. Start a clean BricsCAD V25 host and NETLOAD the exact candidate package using the repository qualification procedure.
2. Exercise the QS3D HOME / PROJECT / BIM transitions, plus a host workspace/Ribbon transition that causes BricsCAD to reconstruct its Ribbon visual tree when available in the licensed host.
3. Repeat the transition/rebuild cycle at least 25 times. Verify QS3D-owned shell chrome remains correctly hidden, QS3D surfaces route correctly, and normal CAD tabs are not covered by stale QS3D surfaces.
4. Capture managed-memory/retention evidence with the repository-approved local tooling. After old Ribbon generations become otherwise unreachable and GC is allowed by the harness, verify QS3D does not retain prior visual trees solely through `Blt3dShellChromeCoordinator`.
5. Exercise plugin/host teardown so `Blt3dShellChromeCoordinator.Reset()` runs. Verify any still-live chrome registered by QS3D is restored best-effort and teardown emits no callback/UI exception.
6. Reopen/reload once more and repeat a short HOME / PROJECT / BIM cycle to catch stale static-generation residue.

Do not report `LOCAL_PASS` without licensed-host evidence for the exact SHA. Hosted CI may only report `REMOTE_SAFE` source/static/locked-reference compile evidence.
