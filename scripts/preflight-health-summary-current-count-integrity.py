from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "HealthSummary.cs"

text = SOURCE.read_text(encoding="utf-8")

current = "var issue = enumerator.Current;"
rebound = "RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);"
retain = "result.Add(issue);"

current_pos = text.find(current)
if current_pos < 0:
    raise SystemExit("HealthSummary must capture enumerator.Current explicitly before retention.")

rebound_pos = text.find(rebound, current_pos + len(current))
if rebound_pos < 0:
    raise SystemExit("HealthSummary must revalidate known Count immediately after enumerator.Current.")

retain_pos = text.find(retain, rebound_pos + len(rebound))
if retain_pos < 0:
    raise SystemExit("HealthSummary must retain the issue only after the post-Current Count rebound.")

next_loop_boundary = text.find("}", retain_pos)
if next_loop_boundary >= 0 and not (current_pos < rebound_pos < retain_pos < next_loop_boundary):
    raise SystemExit("HealthSummary Current/Count/retention ordering drifted.")

print("PASS health summary Current Count integrity source guard")
