# V25 update and version commands

QS3D V25 exposes the following user-facing BricsCAD commands after the plugin is loaded:

- `QS3DUPDATE` — open the secure GitHub Release Update Center.
- `QSUPDATE` — short alias of `QS3DUPDATE`.
- `QS3DVER` — print the running product version, assembly version, exact loaded DLL path and current update status in the BricsCAD command line.
- `QSVER` — short alias of `QS3DVER`.
- `QS3DUPDATEONCLOSE` — toggle installation of an already verified eligible update when BricsCAD is closed normally.
- `QS3DUPDATESTATUS` — show the Update-khi-đóng preference plus the current updater state.

The Update Center also shows the running product version prominently in its title/header, the newest applicable GitHub release, and the exact `QS3D.BricsCAD.V25.dll` path loaded by BricsCAD. The loaded path is intentionally visible so stale binaries or copy-over installations can be diagnosed without guessing.

The one-click updater remains fail-closed: release/channel selection, signed update manifest, package hash, executable signatures/publisher identity, bounded download/archive checks, atomic install/rollback and graceful BricsCAD close/restart behavior remain handled by the existing updater chain.

Source/static presence is not a substitute for the licensed BricsCAD V25 clean-machine runtime/signing qualification required by the release runbook.
