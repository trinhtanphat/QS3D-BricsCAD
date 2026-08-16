# LOCAL_ONLY closeout — 2026-08-16

## Outcome

**Not all LOCAL_ONLY work is complete.** This pass closes or materially advances the executable V25 rows available on the current machine, corrects the canonical inbox, and records every remaining source/resource boundary without manufacturing PASS evidence.

The product remains a BricsCAD-hosted plugin. Installed AutoCAD and BLT3D environments are useful references but are not substitutes for matching BricsCAD V25/V26 host qualification.

## Exact candidate and environment

- Latest broad runner candidate: `8032e70c4f0c537c854c11819b079c4f50f48bba`, rebased on `origin/main@39051f102899499fda124946eb9df73f7cc013f9`
- Latest synchronized LOCAL-018 candidate: `80ed9caef3441d904614a9297aa9374df685ebe5`, rebased on `origin/main@6d7deeae50fc6b7c33c077df904bea8f86b1e18a`. Runtime results remain attached to their exact tested SHA and are not relabelled to a later documentation commit.
- Final documentation sync observed `origin/main@0b6c4bfefb11d82b1e0efacc20e0535d2a4f8edb`; the later source movement is not qualified by this local evidence and does not relabel either tested candidate.
- Host: BricsCAD V25.2.10 x64 on Windows 11 Pro x64; CLR 4.0
- Latest adapter/Core ProductVersion: `0.1.0-preview.10081+80ed9caef3441d904614a9297aa9374df685ebe5`
- Latest adapter SHA-256: `F61EF798718A600087A8A5EB5468FB3374D857796DE1CC5DD7A1C58AA3D5BB1E`
- Latest Core SHA-256: `06D8165F47B1472D62ECFD0EDADA3DAF2F88B9A7C6242E0331D0677BD13AA406`
- V25 build: `Release|x64`, zero warnings and zero errors
- Package evidence remains tied to `02b386de...`: 17 checksum-covered files, 412 commands, `OnCommand` DemandLoad, unsigned preview ZIP SHA-256 `40FDDA96A3C7D1109EA90DFAEAB63CDD686F08795F8FBF0643805AD72484E432`
- Exact `8032e70c...` runtime baseline: NETLOAD/Ribbon/Workspace/palette PASS; sidecar revision and four-document save/reopen/multi-DWG lifecycle with all command/unit phases PASS; zero BricsCAD process residue. Isolated DemandLoad/registry restoration remains the earlier exact `02b386de...` result and is not silently relabelled.
- Display available in this session: 1366x768, Intel UHD 620, 100-percent DPI only
- Missing capabilities: BricsCAD V26, code-signing certificate/private key, alternate DPI profiles, an authorized representative large project, the known-good BRC reference/workbook and a connected Chrome Profile 7 session

## Canonical matrix

| Item | Status | Evidence / remaining boundary |
|---|---|---|
| LOCAL-001 | IN_PROGRESS | Exact `8032e70c...` V25 build, NETLOAD, sidecar revision and expanded project lifecycle pass on BricsCAD 25.2.10; exact `80ed9cae...` also builds and NETLOADs. Package/DemandLoad remains exact `02b386de...`; modeless export, Interchange/Host Link, rebar/Save/UI prompt matrices remain pending. PR `#2118` merged, but exact offline-WPF rerun passes Theme then fails in its palette runner before assertions; `#2085` is reopened as a source defect. |
| LOCAL-002 | IN_PROGRESS | Exact `8032e70c...` passes the complete Curtain P01-P12 matrix on one binary. Family-editor and broad H.1 remain pending. |
| LOCAL-003 | IN_PROGRESS | Exact `8032e70c...` passes Level mm/m, representative lifecycle and curved/round structural mm/m runners. Complete-family dual-unit lifecycle, broader multi-DWG and authorized representative/private evidence remain pending under the canonical row. |
| LOCAL-004 | PASS | Exact `8032e70c...` confirms the complete Source Reconcile production matrix plus four native Undo variants with save/cold reopen, rollback and multi-DWG cleanup. |
| LOCAL-005 | OPEN | Polygon Slab/Foundation native reinforcement region/hole/bulge/rollback/lifecycle matrix has no licensed current evidence. |
| LOCAL-006 | OPEN | Native tag/Table/custom-schedule, Layout/Viewport and detached modeless reporting lifecycle remain unqualified. |
| LOCAL-007 | OPEN | Wall-junction analysis, Wall Snap and physical L/T/X/Multi ownership/rebuild matrix remain unqualified. |
| LOCAL-008 | OPEN | Direct Draw quick/advanced cancel, prompt-drift, transient/repeated-mode, UCS and document-switch matrix remain unqualified. |
| LOCAL-009 | BLOCKED | Unsigned preview packaging and DemandLoad pass only. No production certificate, V26 host or authorized clean customer-like trust target exists in this session. |
| LOCAL-010 | BLOCKED | Exact-current `977311c2...` passes 100-percent-DPI Start Center rendering and real-button dispatch (`Drawing1`→`Drawing2`), resolving the #1807 interaction defect. Alternate DPI, V26 and representative large-model timing remain unavailable. |
| LOCAL-011 | IN_PROGRESS | Curtain atomicity/post-commit/multi-DWG slice passes. Cross-family Grid/Rebar replacement, stale modeless windows and palette teardown/rebind remain pending. |
| LOCAL-012 | OPEN | Project Browser tree selection/Locate, drag/drop, multi-DWG, corrupted-state and large-tree performance remain unqualified. |
| LOCAL-013 | BLOCKED | Synthetic/public probe fails closed safely, but the qualified `7B5D54...` BRC input/workbook is absent and private drawings are not authorized substitutes. |
| LOCAL-014 | IN_PROGRESS | Plan-to-3D P01/P02 pass at exact `8032e70c...`; advanced prompt drift/cancel, injected compensation, Undo/Redo and save/reopen remain pending. |
| LOCAL-015 | IN_PROGRESS | Modeless UI, six buttons, Unicode fill-only, technical-context toggle and empty/>512 refusal pass safely. Actual browser URL/SafeSearch plus DWG-switch/close lifecycle remain pending because Chrome is stopped and the extension-enabled Profile 7 is not connected. |
| LOCAL-016 | OPEN | Integrated MEP takeoff/broad clash/Locate/exact clash/highlight/zoom/modeless/profile matrix from `#1619`, `#1636`, `#1641`, `#1649`, `#1666` still needs licensed graphics/runtime proof. |
| LOCAL-017 | OPEN | Integrated Móng Bè, quantity context menu, Project Setup and six ĐỊNH LƯỢNG routes still need interactive click/rollback/save-reopen proof. |
| LOCAL-018 | BLOCKED | PR `#1804` peer replay is native-confirmed at exact `80ed9cae...`: changed/reordered peers, corrupt ownership refusal, Redo, save/cold reopen, Health and multi-DWG pass. Native Undo restores CAD but leaves semantic applied/generated handles on the erased replacement, yielding two Health errors; sanitized result `UNDO_SEMANTIC_NATIVE_INCOHERENT` keeps `#1744` source-blocked. |

## Current-candidate Curtain detail

On exact source `8032e70c4f0c537c854c11819b079c4f50f48bba`, one V25 binary passed P01-P12. Sanitized outputs include `1/10/15` host/frame/panel objects, opening clipping, 21 straight-path pieces, 168 bulged-path pieces, 39 rebuilt panels, 87 valid objects after the seven injected atomic boundaries, post-commit isolation, Workspace/Health/Release selection, coherent Undo/Redo/save-cold-reopen-rebuild and two-DWG modeless isolation.

The earlier P11 attempts encountered responsive BricsCAD processes without a product marker and remain diagnostic `NO_RESULT`. Recovery backed up the V25 profile registry, removed only 41 exact test-owned `QS3D-LOCAL-CLOSEOUT-*` profiles, preserved `Default` and initialized `QS3D-5ABD-*` profiles, and confirmed the active CUI plus backup parse as valid XML while BricsCAD had already quarantined the truncated file as `.invalid`. Using an initialized test profile, exact-current `977311c2...` then passed P11 Undo/Redo/save-cold-reopen-rebuild at `1/10/15` counts with zero Health issues and byte-for-byte restoration, followed by P12 two-DWG/modeless isolation with both fixture copies unchanged and zero process/private-state residue.

Exact `8032e70c...` rebuilt with zero warnings/errors and passed the generic NETLOAD/runtime gate plus the expanded four-document lifecycle. The earlier exact `977311c2...` binary also exposed the 100-percent-DPI `Tạo dự án mới` surface as a real `Button` with `InvokePattern`; invoking it changed `Drawing1` to `Drawing2`. All test-owned hosts closed and no raw screenshot or recent-project data is committed.

## Offline WPF rerun

After PR `#2118` merged, exact candidate `80ed9caef3441d904614a9297aa9374df685ebe5` reran the aggregate smoke against adapter SHA-256 `F61EF798718A600087A8A5EB5468FB3374D857796DE1CC5DD7A1C58AA3D5BB1E` and palette-runner SHA-256 `5F8D87B9FF25414EC586DA48BA280C0A31EDD571801BFEE9C3891E1D49B8A40B`. PowerShell `7.6.4` and Windows PowerShell `5.1.26100.9168` both passed `Theme.xaml` for 11 styled control types, then failed before any Workspace/RightPanel assertion: `New-XamlNamespaceManager` yielded `System.Object[]`, which cannot bind to the required `System.Xml.XmlNamespaceManager` parameter.

This is a source-safe runner defect rather than qualified palette evidence. Issue `#2085` was reopened with the exact sanitized result at `#issuecomment-5309043684`; the local lane made no source/script repair and awaits an integrated source SHA before rerunning.

## LOCAL-018 native result

PR `#1804` is present in exact source `80ed9caef3441d904614a9297aa9374df685ebe5`. A disposable synthetic Slab with two exact `slabOpen` elements proved the changed-footprint refusal, replay of both peers after host replacement, the same result after reversing peer order, and fail-closed corrupt host XData ownership without replacement. The final host retained one solid, two applied openings, `15.4 m3` synthetic volume and zero Health errors. Redo, save/cold reopen and second-DWG isolation were coherent.

The required native Undo row failed: CAD returned to the prior host solid and prior footprints, while in-memory `GeneratedSolidHandle` and both applied-opening handles still named the erased replacement. Runtime Health reported two errors. The sanitized final marker is `UNDO_SEMANTIC_NATIVE_INCOHERENT`; therefore `production_local018_qualified=false` and Issue `#1744` remains the source handoff. Both test processes exited and all disposable drawings, sidecars, locks and private scripts were removed.

## Safety and handoff

- No private/customer DWG, raw Handle, ProjectId, browser history, certificate, proprietary BLT3D binary/API or unsanitized screenshot is committed.
- No source-safe product defect was fixed in this local-only lane. Source issues are handed to the existing remote/source owners.
- No GitHub Actions were dispatched and no release was published.
- `main` remains read-only. This closeout belongs to the dedicated task branch and must stop at a PR unless the owner separately authorizes integration.
