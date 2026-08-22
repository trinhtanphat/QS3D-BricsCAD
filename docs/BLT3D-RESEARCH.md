# BLT3D public-source research notes

Research date: 2026-08-09.  
Product-form clarification: 2026-08-10.  
Current implementation-truth audit: 2026-08-14.

Public-source review found product references for BLT3D/BricsCAD workflows, but no public repository containing the BLT3D product source code in the searches performed.

## Product-form clarification

QS3D's chosen product target in this repository is a **BricsCAD-hosted plugin**. Public BLT/BLT3D references must not be used to infer that this repository should become a standalone application or EXE.

Within QS3D documentation, `BLT-like`, `BLT-style`, `BLT3D-familiar` and similar language means clean-room **workflow/UX only**: navigation, panels, commands, takeoff flow and user ergonomics. It does not assert how BLT itself is packaged and it does not change this repository's hosted-plugin architecture.

See `docs/PRODUCT-BOUNDARY.md` for the current V25/V26 and sibling-product boundary.

## Research archive vs implementation status

The dated BLT3D/Gemini research archive is retained as provenance and idea-generation material. It is **not** a live list of missing QS3D code.

Before implementing a research-derived idea, read:

- `docs/research/BLT3D-GEMINI-RESEARCH-INDEX.md` — archive provenance/deduplication boundary;
- `docs/research/BLT3D-QS3D-IMPLEMENTATION-STATUS-2026-08-14.md` — current-source overlay showing already-landed foundations and remaining `PARTIAL_OR_OPEN`, `LOCAL_ONLY`, `ENGINEERING_REQUIRED`, or out-of-scope work;
- `docs/BLT3D-RESEARCH-TO-QS3D-AGENT-WORKSTREAMS-2026-08-12.md` — dated advisory decomposition only;
- current source/tests and current `ACTIVE`/`BLOCKED` claims — final authority before opening a new lane.

Project policy:
- Treat BLT3D as proprietary unless its author/license explicitly provides source.
- Build QS3D independently as a clean-room BricsCAD plugin implementation.
- Use supplied screenshots and requirements only as workflow/UX references.
- Do not depend on a BLT installation folder, BLT binaries, license files, or proprietary assets.
- A user-owned installation may later be inspected only for compatibility/migration behavior where legally permitted, never copied into this public repository.
- Do not reimplement a dated research lane until current source proves the capability is actually missing.
- Do not convert `LOCAL_ONLY`, engineering-policy, or separate-product research into speculative plugin code merely to make the research archive look “complete”.
