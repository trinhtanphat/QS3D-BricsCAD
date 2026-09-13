from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs").read_text(encoding="utf-8")

required = [
    "item.Done.Wait();",
    "CadWorkCancelledBeforeStart",
    "Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued)",
]
missing = [token for token in required if token not in source]
if missing:
    raise SystemExit("embedded CAD ownership guard missing: " + ", ".join(missing))

unsafe = "completion is uncertain. Do not retry automatically; inspect CAD state before deciding whether another mutation is safe."
if unsafe in source:
    raise SystemExit("embedded CAD dispatcher still abandons ownership after callback start")

late_dispose = "Interlocked.Exchange(ref item.Abandoned, 1)"
if late_dispose in source:
    raise SystemExit("embedded CAD dispatcher still uses abandoned completion ownership")

print("PASS: embedded CAD callback retains ownership through terminal completion after start")
