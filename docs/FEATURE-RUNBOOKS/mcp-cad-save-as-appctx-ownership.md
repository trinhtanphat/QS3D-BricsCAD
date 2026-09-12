# MCP `cad_save_as` application-context ownership

## Failure class

`cad_save_as` is a side-effecting native mutation. It must not run through the bounded diagnostic CAD-context dispatcher because that dispatcher may return a timeout after work has already started.

A timeout after `Database.SaveAs(...)` starts can otherwise create a ghost mutation: the MCP request reports failure and releases writer ownership while BricsCAD can still change the active drawing path or finish writing the target DWG.

## Required ownership contract

- Queue SaveAs through a mutation-owned application-context work item.
- Timeout may cancel only while work is still queued and has not crossed the start boundary.
- Once running, retain request/writer ownership until that exact callback reaches terminal completion.
- Do not retry or replay an uncertain started SaveAs.
- Capture the active managed `Document` and non-zero native `Database.UnmanagedObject` generation before SaveAs.
- Revalidate active document, native database generation, and exact requested path after SaveAs and before chained native QSAVE verification.
- Keep `CMDACTIVE`, overwrite/path validation, document lock, writer coordination, audit, and QSAVE/DBMOD verification intact.
