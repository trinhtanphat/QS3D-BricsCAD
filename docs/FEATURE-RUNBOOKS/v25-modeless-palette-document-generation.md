# V25 modeless palette document-generation affinity

## Scope
Start Center and Project Information are modeless UI surfaces. Every document-scoped refresh must bind the managed BricsCAD Document together with the exact native Database.UnmanagedObject generation observed at admission.

## Invariant
A refresh may publish project, floor, status, recent-project, diagnostic, or Project Information state only while both the managed document is still MdiActiveDocument and the native database identity is unchanged and non-zero. Same-wrapper native database replacement is treated as stale context.

## Lifecycle
Show and DocumentActivated capture the exact generation before entering panel refresh. Panels revalidate before document-scoped state mutation and immediately before UI publication. Activation and Show exception paths retain the same captured managed/native generation: Start Center suppresses Editor diagnostics once that generation is stale, and Project Information clears unavailable state only while that exact generation remains current. Stale generation therefore fails closed by clearing or suppressing document-scoped output; it must not resolve, diagnose, or publish another DWG state. Existing palette subscription/disposal ownership remains in the coordinators.

## Validation
Run scripts/preflight-v25-modeless-palette-document-generation.py plus the full discovered feature-source aggregate and admitted-reference V25 compile when available. Real MDI reentrancy, same-wrapper reload/replacement, PaletteSet disposal timing, Editor/PaletteSet host pumping, and visible modeless behavior remain LOCAL_ONLY licensed-runtime evidence.
