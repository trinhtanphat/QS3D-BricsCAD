# BricsCAD V25 Palette SetVisibility snapshot containment

## Scope

Lane C03 owns this hardening in `PaletteCoordinator.SetVisibility(...)`.
The transaction controls Workspace, Properties, Right and Quantity Insight native `PaletteSet` visibility.
It does not own project data, CAD geometry, MCP transport, installer or release state.

## Failure model

`PaletteSet.Visible` is a fallible native-host property. A stale/disposed palette may throw while the managed field still references it.
The prior implementation read four rollback values directly before entering the mutation `try`, so a getter failure could abort before a coherent rollback snapshot existed.

## Required transaction

1. Capture the exact four currently-published `PaletteSet` owners.
2. Read each captured `Visible` value through `TryReadPaletteVisibility`.
3. Require all four reads to be trustworthy before the first native setter.
4. If any read fails, fail closed with no visibility mutation.
5. Apply requested visibility only to the captured owner that is still the currently-published owner.
6. Revalidate all captured owners after the final setter because native setters may re-enter host UI code.
7. On apply/revalidation failure, restore prior visibility best-effort in reverse order only on the same still-published captured owners.
8. Preserve and rethrow the original transition failure even if one rollback setter also fails.

## Ownership and lifecycle review

The contained read helper is exact-instance and UI-state-only. It must not resolve `MdiActiveDocument`, bind project state, acquire a CAD transaction, dispatch commands or recreate palettes.
A setter callback may trigger palette recreation; `ReferenceEquals(expected, current)` therefore fences every apply and rollback operation against cross-generation restore.
Properties `StateChanged` remains owned by the published Properties palette and is not duplicated by this visibility transaction.
No-document and MDI transitions must either finish on the captured generation or fail closed; they must never mutate a replacement generation with stale rollback state.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-palette-setvisibility-snapshot-containment.py
python scripts/preflight-v25-palette-visibility-rollback.py
python scripts/preflight-palette-lifecycle-atomicity.py
python scripts/preflight-blt-bim-five-region-layout.py
python scripts/preflight-all.py
```

Build V25 only with the repository's admitted .NET Framework/BricsCAD reference workflow. A workstation missing the .NET Framework 4.8 targeting pack is `NO_RESULT`, not product failure and not PASS.

## Runtime classification

`REMOTE_SAFE`: deterministic source guards, aggregate preflight/smoke and admitted-reference V25 compilation.
`LOCAL_ONLY`: licensed BricsCAD V25 fault injection where native `Visible` getters/setters throw or re-enter during teardown/recreation.
Report `LOCAL_ONLY / NO_RESULT` until that exact candidate is exercised in a compatible licensed host.
