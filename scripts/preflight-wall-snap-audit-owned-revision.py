from pathlib import Path

text = Path("src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs").read_text(encoding="utf-8")

preview_start = text.find('public void Preview()')
apply_start = text.find('public void Apply()')
build_start = text.find('private static SnapPlan BuildPlan', apply_start)
if min(preview_start, apply_start, build_start) < 0:
    raise SystemExit("FAIL: Wall Snap method boundaries missing")

preview = text[preview_start:apply_start]
apply = text[apply_start:build_start]

preview_tokens = [
    'RequireTouchHeadroom(project, 2, "Wall Snap Preview")',
    'AuditTrail.ForProject(project).Record("wall.junction.snap.preview"',
    'var approvedVersion = NextChangeVersion(project.ChangeVersion);',
    'project.Metadata[PreviewChangeVersionKey] = approvedVersion.ToString',
    'project.Touch();',
]
positions = []
for token in preview_tokens:
    pos = preview.find(token)
    if pos < 0:
        raise SystemExit(f"FAIL: preview bookkeeping missing {token}")
    positions.append(pos)
if positions != sorted(positions):
    raise SystemExit("FAIL: preview final-version bookkeeping ordering changed")

if 'RequireTouchHeadroom(project, 1, "Wall Snap Apply")' not in apply:
    raise SystemExit("FAIL: Apply must reserve one revision advance")
record = 'AuditTrail.ForProject(project).Record("wall.junction.snap.apply"'
record_pos = apply.find(record)
if record_pos < 0:
    raise SystemExit("FAIL: Apply audit event missing")

nonempty_start = apply.find('var touchedHandles =')
if nonempty_start < 0 or nonempty_start >= record_pos:
    raise SystemExit("FAIL: non-empty Apply boundary missing")
if 'project.Touch();' in apply[nonempty_start:record_pos]:
    raise SystemExit("FAIL: non-empty Apply contains standalone Touch before audit-owned revision")

zero_start = apply.find('if (plan.Edits.Count == 0)')
zero_end = apply.find('var touchedHandles =', zero_start)
zero_body = apply[zero_start:zero_end]
if 'if (ClearPreview(project)) project.Touch();' not in zero_body:
    raise SystemExit("FAIL: zero-edit Apply must still version preview metadata removal")

print("PASS: Wall Snap Apply revision is audit-owned while Preview final-version bookkeeping remains intact")
