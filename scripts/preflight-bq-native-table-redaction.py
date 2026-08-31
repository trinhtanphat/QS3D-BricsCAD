#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
path = root / "src/QS3D.BricsCAD.V25/BqNativeTableCommands.cs"
text = path.read_text(encoding="utf-8") if path.is_file() else ""
errors = []

for token in (
    'catch (Exception) { Report(document, "QS3DBQTABLE lỗi: thao tác không hoàn tất; kiểm tra project/CAD state và thử lại."); }',
    'catch (Exception) { Report(document, "QS3DBQTABLEREFRESH lỗi: refresh không hoàn tất; kiểm tra project/CAD state và thử lại."); }',
    'catch (Exception) { Report(document, "QS3DBQTABLEREMOVE lỗi: remove không hoàn tất; kiểm tra project/CAD state và thử lại."); }',
    'catch (Exception) { Report(document, "QS3DBQTABLEHEALTH lỗi: health check không hoàn tất; kiểm tra project/CAD state và thử lại."); }',
    'document.Editor.Regen();',
    'PaletteCoordinator.RefreshProject();',
    'native Table đã commit nhưng viewport/palette không refresh đầy đủ.',
):
    if token not in text:
        errors.append("missing BQ Table redaction/lifecycle token: " + token)

for forbidden in ('ex.Message', 'exception.Message', 'GetBaseException()', 'StackTrace'):
    if forbidden in text:
        errors.append("BQ Table user surface exposes raw exception detail: " + forbidden)

regen = text.find('document.Editor.Regen();')
refresh = text.find('PaletteCoordinator.RefreshProject();', regen + 1)
status = text.find('PaletteCoordinator.SetStatus(message);', refresh + 1)
write = text.find('document.Editor.WriteMessage("\\nQS3D " + message);', status + 1)
if min(regen, refresh, status, write) < 0 or not (regen < refresh < status < write):
    errors.append("FinalizeUi must preserve Regen -> palette refresh -> status -> editor ordering")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)
print("PASS: BQ native Table command failures and post-commit UI warnings are stable/redacted while UI ordering remains pinned.")
