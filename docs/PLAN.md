# QS3D BricsCAD V25 plan

## Implemented foundation in this batch
- V25/net48/x64 plugin project with external BricsCAD references.
- Docked left/right WPF palettes.
- Semantic category/family domain models for room/wall/opening requirements.
- Selection snapshot reader with curve length + closed polyline area.
- Quantity report grouping/totals.
- Modeless BQ window.
- Dependency-free real `.xlsx` exporter.
- Formula/rebar/unit/geometry foundations.
- Manual-only CI gates.
- Public-repo guard against proprietary BricsCAD/BLT files and private DWG/DOCX fixtures.

## Next integration milestones after first successful V25 build
1. Native BricsCAD ribbon tabs/buttons wired to QS3D commands.
2. CAD transactions for create/delete/update semantic elements.
3. Persistent `.qsdb` project model and source-handle traceability.
4. HT_Phòng automatic finish generation from room boundaries.
5. Tường KT 3D generation from centerline/profile/thickness/levels.
6. Door/opening boolean deductions and host-wall linking.
7. Layer/Xref manager wired to the current drawing database.
8. Locate/highlight/zoom from BQ rows back to source handles.
9. Concrete/formwork/opening/finish deterministic formulas.
10. Revision/diff and audit trail.
