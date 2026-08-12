# Work claim — release #28 product-boundary preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:41:00+07:00`
- Expanded: `2026-08-12T08:43:00+07:00`
- Completed: `2026-08-12T08:46:00+07:00`
- Baseline main SHA: `232dfe41ee4e43b3ce215dabc89da46340c30b2b`
- Priority: QS3D Cloud V25 Preview Build & Release #28 failed `preflight-product-boundary.py` on superseded V25-only product markers while the canonical product boundary is V25 + V26 hosted plugins.

## Completed implementation

- `cd060b90bacca0ff54bfbd5e78cecf36b58fb21f` — `docs(product): align requirements with V25 V26 hosting`
- `6936bd427c02b4766f4b7af6bdecb58cc275afb8` — `docs(agent): align product form with V25 V26 hosts`
- `cdd4cd27e603648549a26e60013ae56eddbf70a6` — `fix(preflight): align product boundary with V25 V26 hosts`

`docs/REQUIREMENTS.md` and the locked product-form paragraph in `AGENTS.md` now describe the same V25 + V26 hosted-plugin model already locked by README / PRODUCT-BOUNDARY / ARCHITECTURE: matching licensed host, V25 `net48`, V26 `net8.0-windows`, explicit host-major identity and no standalone `QS3D.exe`.

The preflight no longer requires obsolete V25-only markers from current cross-host documents. It now additionally verifies both host csproj files preserve their correct target framework, `OutputType=Library` and host-major AssemblyName, and verifies both V25/V26 `PluginEntry.cs` files retain `IExtensionApplication`. V25-specific install/Direct Draw/historical markers remain intentionally scoped and were not rewritten.

## Validation performed

- Verified claim expansion commit `ff94e8ed23e43948aec1600a595bbf3159f9341b` was current `main` before implementation.
- Read back `AGENTS.md` and confirmed the locked product form is V25 + V26 hosted plugins with no standalone reinterpretation.
- Read back `docs/REQUIREMENTS.md` and confirmed matching licensed V25/V26 host, net48/net8 host adapters, cross-major fail-closed identity and no standalone EXE wording.
- Read back `scripts/preflight-product-boundary.py` and confirmed current documentation tokens plus both host-project Library/entry-point checks are present.
- Verified `cdd4cd27e603648549a26e60013ae56eddbf70a6` remains an ancestor of moving `main`; the immediate concurrent commit after it only added an unrelated tie-rebar claim.

## Validation boundary

- Run #28 is tied to `fbd5edf8c14c3c7547ac040172450e31add73cff` and cannot validate these newer commits.
- No GitHub Actions workflow was dispatched or rerun from this lane.
- No aggregate `preflight-all.py`, full Core smoke, licensed V25/V26 runtime, package, signing, installer or release PASS is claimed here.
- The separate run #28 V26 version/release mismatch and other feature-gate failures remain outside this completed scope and must be reconciled against current `main` before edits.
