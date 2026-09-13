from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs")
text = SOURCE.read_text(encoding="utf-8")

anchor = '[CommandMethod("QS3DMCPAGENTCENTER", CommandFlags.Modal)]'
start = text.find(anchor)
if start < 0:
    raise SystemExit("FAIL: QS3DMCPAGENTCENTER command boundary not found")
end = text.find("internal sealed class McpAgentControlCenterWindow", start)
if end < 0:
    raise SystemExit("FAIL: Agent Center command boundary terminator not found")
command = text[start:end]

if "ex.Message" in command:
    raise SystemExit("FAIL: Agent Center command boundary publishes raw exception text")
if "McpPublicTextSanitizer" not in command:
    raise SystemExit("FAIL: Agent Center command boundary must sanitize public failure text")
if "document.Editor.WriteMessage" not in command:
    raise SystemExit("FAIL: Agent Center command boundary must retain bounded editor feedback")
if "MdiActiveDocument" not in command:
    raise SystemExit("FAIL: Agent Center command boundary must bind failure publication to captured document")

print("PASS: Agent Center command boundary sanitizes exception publication and keeps document-bound feedback")
