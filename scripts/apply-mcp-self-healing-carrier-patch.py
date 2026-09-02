#!/usr/bin/env python3
from pathlib import Path

PATH = Path("src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs")
text = PATH.read_text(encoding="utf-8")

old_catches = '''            catch (McpToolContractException ex) { return ToolError(ex.Code, McpToolCapabilityContract.LaneName(ex.Lane), ex.Message); }
            catch (Exception ex)
            {
                var failure = McpToolCapabilityContract.ClassifyFailure(tool, ex);
                return ToolError(failure.Code, McpToolCapabilityContract.LaneName(failure.Lane), failure.Message);
            }'''
new_catches = '''            catch (McpToolContractException ex)
            {
                var lane = McpToolCapabilityContract.LaneName(ex.Lane);
                var repairJson = McpSelfHealingRepairRuntime.RecordFailure(
                    tool, ex.Code, lane, ex.Message, ex, true);
                return ToolError(ex.Code, lane, ex.Message, repairJson);
            }
            catch (Exception ex)
            {
                var failure = McpToolCapabilityContract.ClassifyFailure(tool, ex);
                var lane = McpToolCapabilityContract.LaneName(failure.Lane);
                var repairJson = McpSelfHealingRepairRuntime.RecordFailure(
                    tool, failure.Code, lane, failure.Message, ex, false);
                return ToolError(failure.Code, lane, failure.Message, repairJson);
            }'''

old_error = '''        private static string ToolError(string code, string lane, string message)
        {
            var safeCode = string.IsNullOrWhiteSpace(code) ? McpToolCapabilityContract.ToolFailedCode : code;
            var safeLane = string.IsNullOrWhiteSpace(lane) ? "unknown" : lane;
            var safeMessage = string.IsNullOrWhiteSpace(message) ? "MCP tool failed." : message;
            return "{\\\"content\\\":[{\\\"type\\\":\\\"text\\\",\\\"text\\\":\\\"" + JsonEscape(safeCode + ": " + safeMessage)
                   + "\\\"}],\\\"structuredContent\\\":{\\\"error\\\":{\\\"code\\\":\\\"" + JsonEscape(safeCode)
                   + "\\\",\\\"lane\\\":\\\"" + JsonEscape(safeLane) + "\\\",\\\"message\\\":\\\"" + JsonEscape(safeMessage)
                   + "\\\"}},\\\"isError\\\":true}";
        }'''
new_error = '''        private static string ToolError(string code, string lane, string message, string repairJson = null)
        {
            var safeCode = string.IsNullOrWhiteSpace(code) ? McpToolCapabilityContract.ToolFailedCode : code;
            var safeLane = string.IsNullOrWhiteSpace(lane) ? "unknown" : lane;
            var safeMessage = string.IsNullOrWhiteSpace(message) ? "MCP tool failed." : message;
            var repair = string.IsNullOrWhiteSpace(repairJson) ? string.Empty : ",\\\"repair\\\":" + repairJson;
            return "{\\\"content\\\":[{\\\"type\\\":\\\"text\\\",\\\"text\\\":\\\"" + JsonEscape(safeCode + ": " + safeMessage)
                   + "\\\"}],\\\"structuredContent\\\":{\\\"error\\\":{\\\"code\\\":\\\"" + JsonEscape(safeCode)
                   + "\\\",\\\"lane\\\":\\\"" + JsonEscape(safeLane) + "\\\",\\\"message\\\":\\\"" + JsonEscape(safeMessage)
                   + "\\\"" + repair + "}},\\\"isError\\\":true}";
        }'''

if text.count(old_catches) != 1:
    raise SystemExit("expected exactly one original CallTool catch block")
if text.count(old_error) != 1:
    raise SystemExit("expected exactly one original ToolError block")

text = text.replace(old_catches, new_catches)
text = text.replace(old_error, new_error)
PATH.write_text(text, encoding="utf-8")
print("PASS: applied two localized MCP self-healing transport edits")
