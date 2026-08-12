# Work claim — rebar XLSX XML sanitization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-xlsx-xml-sanitization-20260812-0130`
- Registered: `2026-08-12T01:30:00+07:00`
- Baseline main SHA: `45991a9b38e3968f047bcd83b38f7ba6625ed186`
- Integrated main SHA: `ec9fb8b064d586a7f284385dad0ed7787d2aea73`
- PR: `#605`
- Priority: evidence-driven remote-safe XLSX integrity hardening during owner-requested `continue all`

## Completed scope

BBS/Rebar XLSX inline-string cells now use the repository's XML 1.0 sanitizer so invalid control characters and malformed surrogate sequences cannot produce invalid worksheet XML while valid supplementary Unicode remains intact.

## Changes

- Replaced direct `SecurityElement.Escape` use in `XlsxRebarScheduleExporter.AppendText()` with shared `XlsxXmlText.Escape()`.
- Removed the now-unused `System.Security` import.
- Preserved existing BBS worksheet row-limit, numeric serialization, package validation, and reporting behavior.
- Added dedicated module-initializer smoke coverage that writes a real workbook, reads `xl/worksheets/sheet1.xml`, parses it with `XmlDocument`, and verifies replacement/escaping/supplementary-Unicode round-trip behavior.

## Validation actually performed

- Reviewed exact PR #605 patch: production behavior is one escaping substitution plus the unused import removal; the only other file is focused smoke coverage.
- Re-read moving `main` immediately before PR publication; `XlsxRebarScheduleExporter.cs` remained at original blob `6f1ad91276782d673642a48090f6797867592cab`, so no concurrent source overlap was present.
- Confirmed no workflow runs were associated with exact PR head `71c1d945f17ccfb99bc6694b36e08caa1729397b`.
- Squash-merged PR #605 with exact head SHA `71c1d945f17ccfb99bc6694b36e08caa1729397b` into `main` as `ec9fb8b064d586a7f284385dad0ed7787d2aea73`.
- Re-read the merged exporter and dedicated smoke from remote `main` after integration.
- No GitHub Actions were dispatched.
- No local .NET compile/build, licensed BricsCAD V25/Windows runtime, native entity/UI/geometry execution, or `LOCAL_PASS` is claimed from this environment.

## Integration

PR #605 was squash-merged into `main` as `ec9fb8b064d586a7f284385dad0ed7787d2aea73` without force-push.
