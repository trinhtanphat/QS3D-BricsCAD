# BLT3D public-source research notes

Research date: 2026-08-09.  
Product-form clarification: 2026-08-10.

Public-source review found product references for BLT3D/BricsCAD workflows, but no public repository containing the BLT3D product source code in the searches performed.

## Product-form clarification

QS3D's chosen product target is a **BricsCAD V25 plugin**. Public BLT/BLT3D references must not be used to infer that QS3D should be a standalone application or EXE.

Within QS3D documentation, `BLT-like`, `BLT-style`, `BLT3D-familiar` and similar language means clean-room **workflow/UX only**: navigation, panels, commands, takeoff flow and user ergonomics. It does not assert how BLT itself is packaged and it does not change QS3D's hosted-plugin architecture.

See `docs/PRODUCT-BOUNDARY.md`.

Project policy:
- Treat BLT3D as proprietary unless its author/license explicitly provides source.
- Build QS3D independently as a clean-room BricsCAD V25 plugin implementation.
- Use the supplied screenshots and requirements only as workflow/UX references.
- Do not depend on a BLT installation folder, BLT binaries, license files, or proprietary assets.
- A user-owned installation may later be inspected only for compatibility/migration behavior where legally permitted, never copied into this public repository.
