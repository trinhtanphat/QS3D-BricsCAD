from pathlib import Path
import sys

source = Path("src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs").read_text(encoding="utf-8")
start = source.find("private static string ExecuteDirectLayoutCommand")
end = source.find("private static bool LayoutExists", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL: ExecuteDirectLayoutCommand source block not found")
block = source[start:end]

def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL: " + message)

lock_index = block.find("using (document.LockDocument())")
first_layout = block.find("LayoutManager.Current")
affinity = block.find('EnsureSameActiveDocument(document, "cad_layout")')
result_capture = block.find("var currentLayout = LayoutManager.Current.CurrentLayout")
record = block.find("RecordMutation(document, \"cad-layout\"")

require(lock_index >= 0, "direct layout mutation must retain DocumentLock ownership")
require(affinity > lock_index and affinity < first_layout,
        "exact active-document affinity must be revalidated inside DocumentLock before LayoutManager.Current")
require(result_capture > affinity and result_capture < record,
        "currentLayout result must be captured while exact document affinity is still owned")
require(block.find('EnsureSameActiveDocument(document, "cad_layout_result")', result_capture) > result_capture,
        "result publication must revalidate exact active document after native layout mutation")

print("PASS: MCP direct layout mutations retain exact active-document affinity through result capture")
