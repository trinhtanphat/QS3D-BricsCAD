# Agent work claim — LOCAL_ONLY qualification closeout

- Owner/session: `codex` (local capability lane)
- Registered: 2026-08-16 (UTC+7)
- Initial baseline: `main@5abd0113f1598bf6ca948e09c8c55bd7c8d13d6a`
- Latest broad runner baseline: task SHA `8032e70c4f0c537c854c11819b079c4f50f48bba` on `origin/main@39051f102899499fda124946eb9df73f7cc013f9`
- Latest LOCAL-018 baseline: task SHA `80ed9caef3441d904614a9297aa9374df685ebe5` on `origin/main@6d7deeae50fc6b7c33c077df904bea8f86b1e18a`; evidence remains attached to the exact tested SHA rather than the later docs commit.
- Final documentation sync observed `origin/main@0b6c4bfefb11d82b1e0efacc20e0535d2a4f8edb`; no runtime result is relabelled to that later source.
- Branch: `agent/codex/local-only-closeout-20260816`
- Coordination Issue: `#72`
- Status: `IN_PROGRESS`

## Scope

- Reconcile every canonical item in `docs/LOCAL-AGENT-INBOX.md` against real local capability and current Issue/PR state.
- Build the BricsCAD V25 adapter against the installed licensed V25 references, bind every runtime result to an exact source SHA and DLL hash, and exercise the existing guarded LOCAL_ONLY runners on repository-generated disposable drawings.
- Validate NETLOAD, DemandLoad, registry restoration, package integrity, native P0 workflows, selected P1/P2 workflows with existing approved runners, and zero-process/private-state cleanup.
- Commit only sanitized status/evidence documentation. Raw runtime artifacts stay under ignored local/temp roots.

## Current evidence boundary

- Exact task SHA `8032e70c4f0c537c854c11819b079c4f50f48bba` builds V25 `Release|x64` with .NET SDK `8.0.423`, zero warnings/errors and plugin SHA-256 `2F84137F50872E8C2066BD9D9F830BAAD8C6CA29172616CC7051D035731C5D64`; licensed V25.2.10 NETLOAD/runtime, sidecar revision and expanded four-document lifecycle pass with zero host residue. Latest synchronized SHA `80ed9caef3441d904614a9297aa9374df685ebe5` also builds with zero warnings/errors; ProductVersion is `0.1.0-preview.10081+80ed9caef3441d904614a9297aa9374df685ebe5` and adapter/Core SHA-256 is `F61EF798718A600087A8A5EB5468FB3374D857796DE1CC5DD7A1C58AA3D5BB1E` / `06D8165F47B1472D62ECFD0EDADA3DAF2F88B9A7C6242E0331D0677BD13AA406`.
- The current Curtain reading is complete P01–P12 PASS on one exact `8032e70c...` binary. P11 proves coherent Undo/Redo/save-cold-reopen-rebuild and drawing restoration; P12 proves two-DWG/modeless refusal/reactivation/close isolation. Family-editor and broad H.1 remain pending.
- The same `8032e70c...` candidate passed Level Z Millimeter/Meter, curved structural Millimeter/Meter, representative Level lifecycle, Source Reconcile production plus four Undo variants, Plan-to-3D P01/P02 and project lifecycle/sidecar revision runners. Remaining broad/interactive rows retain their canonical pending status.
- The responsive-headless blocker was recovered without touching product source: the V25 profile subtree was backed up, 41 exact test-owned closeout profiles were removed, initialized profiles were reused, and the truncated CUI was already quarantined separately. Historical timeouts remain `NO_RESULT`; current exact runtime markers supersede them only for the rerun rows.
- Earlier exact candidate `5abd0113f1598bf6ca948e09c8c55bd7c8d13d6a` passed the complete Curtain P01–P12 matrix, Level Z mm/m plus lifecycle and curved-structural rows, Source Reconcile plus Undo lifecycle, Plan-to-3D P01/P02, the sidecar-revision probe, NETLOAD, DemandLoad and cleanup. The later `02b386de` source delta does not touch those domain/runtime probe paths, but exact-current reruns are recorded separately rather than silently relabelled.
- Source fix PR `#2124` closed `#2092`, and the licensed lifecycle rerun passes. The 100-percent-DPI Start Center primary card also passes on exact `977311c2...`: UI Automation resolves a real `Button` with `InvokePattern`, and invocation changes `Drawing1` to `Drawing2`. PR `#2118` merged, but the exact `80ed9cae...` offline-WPF rerun passes Theme and then fails before palette assertions because `New-XamlNamespaceManager` yields `System.Object[]` instead of `System.Xml.XmlNamespaceManager` under both PowerShell `7.6.4` and Windows PowerShell `5.1.26100.9168`; Issue `#2085` is reopened for the source-safe fix, and this local lane did not edit the runner.
- PR `#1804` is integrated, so LOCAL-018 is no longer blocked by missing peer-replay source. Exact `80ed9cae...` native execution passes changed/reordered peer replay, corrupt ownership refusal, Redo, save/cold reopen, final Health and multi-DWG. Native Undo restores the old CAD state but leaves semantic generated/applied handles on the erased replacement, producing two Health errors; sanitized result is `UNDO_SEMANTIC_NATIVE_INCOHERENT`, `production_local018_qualified=false`, handed back to Issue `#1744`.
- V26, production signing/trust, non-100-percent DPI, representative large/private projects, connected Chrome Profile 7 URL verification and the known-good BRC round-trip fixture/workbook are unavailable in this local session and cannot be fabricated.

## Exclusions and safety boundary

- No direct write, merge, force-push or equivalent mutation of `main`.
- No source-safe product bug fix from this local-only lane; source defects are reduced to sanitized Issue handoffs for another agent.
- No use or commit of private/customer DWGs, raw Handles, ProjectIds, browsing history, proprietary BLT3D internals, BricsCAD SDK binaries, certificates, signing keys or unsanitized screenshots.
- No GitHub Actions dispatch and no release publication.
- No claim that every LOCAL_ONLY gate is complete while an OPEN, IN_PROGRESS or BLOCKED row remains.
