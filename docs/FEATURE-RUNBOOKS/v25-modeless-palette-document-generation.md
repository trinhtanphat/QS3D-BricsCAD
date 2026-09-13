# V25 modeless palette document-generation affinity

## Scope
Start Center and Project Information are modeless UI surfaces. Every document-scoped refresh must bind the managed BricsCAD Document together with the exact native Database.UnmanagedObject generation observed at admission.

## Invariant
A refresh may publish project, floor, status, recent-project, or Project Information state only while both the managed document is still MdiActiveDocument and the native database identity is unchanged and non-zero. Same-wrapper native database replacement is treated as stale context.

## Lifecycle
Show and DocumentActivated capture the exact generation before entering panel refresh. Panels revalidate before document-scoped state mutation and immediately before UI publication. Stale generation fails closed by clearing or suppressing document-scoped output; it must not resolve or publish another DWG state. Existing palette subscription/disposal ownership remains in the coordinators.

## Validation
Run scripts/preflight-v25-modeless-palette-document-generation.py plus the full discovered feature-source aggregate and admitted-reference V25 compile when available. Real MDI reentrancy, same-wrapper reload/replacement, PaletteSet disposal timing, and visible modeless behavior remain LOCAL_ONLY licensed-runtime evidence.
