using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// First-class MCP surface for the bounded diagnostic stream and host-wide theme owner.
    /// Diagnostics can only read the canonical QS3D MCP audit files; callers cannot supply paths.
    /// Theme mutations are invoked through McpCadAgentRuntime's mutation guard before reaching here.
    /// </summary>
    internal static class McpDirectDiagnosticsThemeRuntime
    {
        private const int MaxEvents = 100;
        private const int DefaultTailEvents = 25;
        private const int DefaultSnapshotEvents = 50;
        private const int MaxScannedEventsPerFile = 50000;
        private const int MaxEventCharacters = 8192;
        private const int MaxWaitMilliseconds = 15000;
        private const int MinPollMilliseconds = 100;
        private const int MaxPollMilliseconds = 1000;
        private static readonly Regex SequenceRegex = new Regex(
            @"""sequence""\s*:\s*(?<value>[0-9]+)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static IEnumerable<string> ToolDescriptors()
        {
            return new[]
            {
                Tool(
                    "diagnostics_log_tail",
                    "Read the latest bounded unified MCP/QS3D/BricsCAD diagnostic events from the canonical local diagnostic stream.",
                    "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}",
                    true, false, true),
                Tool(
                    "diagnostics_since",
                    "Read bounded unified diagnostic events with sequence greater than afterSequence.",
                    "\"afterSequence\":{\"type\":\"integer\",\"minimum\":0},\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}",
                    true, false, true,
                    "afterSequence"),
                Tool(
                    "diagnostics_snapshot",
                    "Capture current MCP, BricsCAD, QS3D project-audit and theme state into the unified stream and return the newest bounded events.",
                    "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}",
                    true, false, true),
                Tool(
                    "diagnostics_wait",
                    "Bounded long-poll for unified diagnostic events after a sequence cursor. No unbounded server event stream is opened.",
                    "\"afterSequence\":{\"type\":\"integer\",\"minimum\":0},\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100},\"timeoutMs\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":15000},\"pollIntervalMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":1000}",
                    true, false, true,
                    "afterSequence"),
                Tool(
                    "theme_get",
                    "Read configured System/Dark/Light mode, effective host mode and BricsCAD COLORTHEME.",
                    string.Empty,
                    true, false, true),
                Tool(
                    "theme_set",
                    "Set host-wide QS3D and BricsCAD theme to system, dark or light. Requires confirmMutation=true and respects the MCP emergency-stop guard.",
                    "\"mode\":{\"type\":\"string\",\"enum\":[\"system\",\"dark\",\"light\"]},\"confirmMutation\":{\"type\":\"boolean\"}",
                    false, true, true,
                    "mode", "confirmMutation")
            };
        }

        internal static string Call(string toolName, string arguments, Action? ensureMutationRunning, Action<string> audit)
        {
            var tool = toolName ?? string.Empty;
            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            switch (tool)
            {
                case "diagnostics_log_tail":
                    return ReadEvents(0, Integer(args, "limit", DefaultTailEvents, 1, MaxEvents), true);
                case "diagnostics_since":
                    return ReadEvents(RequiredNonNegativeInteger(args, "afterSequence"), Integer(args, "limit", DefaultTailEvents, 1, MaxEvents), false);
                case "diagnostics_snapshot":
                    return Snapshot(Integer(args, "limit", DefaultSnapshotEvents, 1, MaxEvents));
                case "diagnostics_wait":
                    return Wait(args);
                case "theme_get":
                    return ThemeStateJson();
                case "theme_set":
                    return SetTheme(args, ensureMutationRunning, audit);
                default:
                    throw new InvalidOperationException("Unknown direct diagnostics/theme MCP tool: " + tool + ".");
            }
        }

        private static string Snapshot(int limit)
        {
            McpDiagnosticHub.InvokeInCadContext(() =>
            {
                McpDiagnosticHub.CaptureSnapshot("mcp-direct");
                return "{}";
            });
            return ReadEvents(0, limit, true);
        }

        private static string Wait(string body)
        {
            var afterSequence = RequiredNonNegativeInteger(body, "afterSequence");
            var limit = Integer(body, "limit", DefaultTailEvents, 1, MaxEvents);
            var timeout = Integer(body, "timeoutMs", 5000, 0, MaxWaitMilliseconds);
            var poll = Integer(body, "pollIntervalMs", 250, MinPollMilliseconds, MaxPollMilliseconds);
            var timer = Stopwatch.StartNew();
            var result = ReadEventBatch(afterSequence, limit, false);
            if (result.Events.Count > 0)
                return EventBatchJson(result, afterSequence, false, timer.ElapsedMilliseconds, false);

            var stamp = DiagnosticStamp();
            while (timer.ElapsedMilliseconds < timeout)
            {
                var remaining = Math.Max(1, timeout - (int)Math.Min(timeout, timer.ElapsedMilliseconds));
                Thread.Sleep(Math.Min(poll, remaining));
                var nextStamp = DiagnosticStamp();
                if (nextStamp == stamp) continue;
                stamp = nextStamp;
                result = ReadEventBatch(afterSequence, limit, false);
                if (result.Events.Count > 0)
                    return EventBatchJson(result, afterSequence, false, timer.ElapsedMilliseconds, false);
            }
            return EventBatchJson(result, afterSequence, false, timer.ElapsedMilliseconds, true);
        }

        private static string ReadEvents(long afterSequence, int limit, bool tail)
        {
            var result = ReadEventBatch(afterSequence, limit, tail);
            return EventBatchJson(result, afterSequence, tail, 0, false);
        }

        private static EventBatch ReadEventBatch(long afterSequence, int limit, bool tail)
        {
            var all = new List<DiagnosticEventLine>();
            var truncatedScan = false;
            foreach (var path in DiagnosticPaths())
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var scanned = 0;
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (++scanned > MaxScannedEventsPerFile)
                            {
                                truncatedScan = true;
                                break;
                            }
                            if (line.Length == 0 || line.Length > MaxEventCharacters) continue;
                            long sequence;
                            if (!TrySequence(line, out sequence)) continue;
                            if (!tail && sequence <= afterSequence) continue;
                            all.Add(new DiagnosticEventLine(sequence, line));
                        }
                    }
                }
                catch (IOException)
                {
                    // Concurrent rotation/appends are expected; return the safely readable subset.
                }
                catch (UnauthorizedAccessException)
                {
                    // The canonical diagnostics file can temporarily be unavailable during host shutdown.
                }
            }

            all.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            var start = tail ? Math.Max(0, all.Count - limit) : 0;
            var count = Math.Min(limit, Math.Max(0, all.Count - start));
            var selected = new List<DiagnosticEventLine>(count);
            for (var i = 0; i < count; i++) selected.Add(all[start + i]);
            return new EventBatch(selected, truncatedScan || (!tail && all.Count > limit));
        }

        private static IEnumerable<string> DiagnosticPaths()
        {
            var path = McpCadAgentRuntime.AuditFilePath;
            yield return path + ".1";
            yield return path;
        }

        private static long DiagnosticStamp()
        {
            unchecked
            {
                long stamp = 17;
                foreach (var path in DiagnosticPaths())
                {
                    try
                    {
                        if (!File.Exists(path)) continue;
                        var info = new FileInfo(path);
                        stamp = stamp * 31 + info.Length;
                        stamp = stamp * 31 + info.LastWriteTimeUtc.Ticks;
                    }
                    catch { }
                }
                return stamp;
            }
        }

        private static bool TrySequence(string line, out long sequence)
        {
            sequence = 0;
            var match = SequenceRegex.Match(line ?? string.Empty);
            return match.Success
                   && long.TryParse(match.Groups["value"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out sequence)
                   && sequence >= 0;
        }

        private static string EventBatchJson(EventBatch batch, long afterSequence, bool tail, long elapsedMilliseconds, bool timedOut)
        {
            var builder = new StringBuilder(1024).Append("{\"events\":[");
            for (var i = 0; i < batch.Events.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(batch.Events[i].Json);
            }
            var latest = batch.Events.Count == 0 ? afterSequence : batch.Events[batch.Events.Count - 1].Sequence;
            builder.Append("],\"count\":").Append(batch.Events.Count.ToString(CultureInfo.InvariantCulture))
                .Append(",\"latestSequence\":").Append(latest.ToString(CultureInfo.InvariantCulture))
                .Append(",\"afterSequence\":").Append(afterSequence.ToString(CultureInfo.InvariantCulture))
                .Append(",\"tail\":").Append(tail ? "true" : "false")
                .Append(",\"truncated\":").Append(batch.Truncated ? "true" : "false");
            if (elapsedMilliseconds > 0 || timedOut)
                builder.Append(",\"elapsedMs\":").Append(elapsedMilliseconds.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"timedOut\":").Append(timedOut ? "true" : "false");
            return builder.Append('}').ToString();
        }

        private static string ThemeStateJson()
        {
            return McpDiagnosticHub.InvokeInCadContext(ThemeStateJsonInCadContext);
        }

        private static string ThemeStateJsonInCadContext()
        {
            var mode = Qs3dThemeCoordinator.CurrentMode;
            var effectiveDark = Qs3dThemeCoordinator.EffectiveDark;
            var colorTheme = "unknown";
            try { colorTheme = Convert.ToString(Application.GetSystemVariable("COLORTHEME"), CultureInfo.InvariantCulture) ?? "unknown"; }
            catch { }
            return "{\"mode\":\"" + ModeText(mode)
                   + "\",\"effective\":\"" + (effectiveDark ? "dark" : "light")
                   + "\",\"bricscadColorTheme\":\"" + Escape(colorTheme) + "\"}";
        }

        private static string SetTheme(string body, Action? ensureMutationRunning, Action<string> audit)
        {
            if (ensureMutationRunning == null)
                throw new InvalidOperationException("Theme mutation execution context is unavailable.");
            var modeText = McpTopLevelJson.ExtractString(body, "mode").Trim().ToLowerInvariant();
            Qs3dThemeMode mode;
            if (modeText == "system") mode = Qs3dThemeMode.System;
            else if (modeText == "dark") mode = Qs3dThemeMode.Dark;
            else if (modeText == "light") mode = Qs3dThemeMode.Light;
            else throw new InvalidOperationException("mode must be system, dark or light.");

            ensureMutationRunning();
            Qs3dThemeCoordinator.SetMode(mode, "mcp-theme-set");
            ensureMutationRunning();
            if (audit != null) audit("mode=" + modeText + "; host-wide=true");
            return ThemeMutationAckJson(mode);
        }

        private static string ThemeMutationAckJson(Qs3dThemeMode requestedMode)
        {
            var appliedMode = Qs3dThemeCoordinator.CurrentMode;
            var effectiveDark = Qs3dThemeCoordinator.EffectiveDark;
            return "{\"applied\":true,\"requested\":\"" + ModeText(requestedMode)
                   + "\",\"mode\":\"" + ModeText(appliedMode)
                   + "\",\"effective\":\"" + (effectiveDark ? "dark" : "light")
                   + "\",\"verification\":\"theme_get\"}";
        }

        private static int Integer(string body, string property, int fallback, int minimum, int maximum)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            if (value < minimum || value > maximum)
                throw new InvalidOperationException(property + " must be between "
                    + minimum.ToString(CultureInfo.InvariantCulture) + " and "
                    + maximum.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static int RequiredNonNegativeInteger(string body, string property)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found || value < 0)
                throw new InvalidOperationException(property + " must be a non-negative integer.");
            return value;
        }

        private static string Tool(
            string name,
            string description,
            string properties,
            bool readOnly,
            bool destructive,
            bool idempotent,
            params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + Escape(name)
                   + "\",\"description\":\"" + Escape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + (properties ?? string.Empty)
                   + "},\"additionalProperties\":false" + requiredJson + "}"
                   + ",\"annotations\":{\"readOnlyHint\":" + Bool(readOnly)
                   + ",\"destructiveHint\":" + Bool(destructive)
                   + ",\"idempotentHint\":" + Bool(idempotent)
                   + ",\"openWorldHint\":false}}";
        }

        private static string ModeText(Qs3dThemeMode mode)
        {
            return mode == Qs3dThemeMode.Dark ? "dark" : mode == Qs3dThemeMode.Light ? "light" : "system";
        }

        private static string Bool(bool value) { return value ? "true" : "false"; }
        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }

        private sealed class DiagnosticEventLine
        {
            public DiagnosticEventLine(long sequence, string json) { Sequence = sequence; Json = json; }
            public long Sequence { get; private set; }
            public string Json { get; private set; }
        }

        private sealed class EventBatch
        {
            public EventBatch(List<DiagnosticEventLine> events, bool truncated) { Events = events; Truncated = truncated; }
            public List<DiagnosticEventLine> Events { get; private set; }
            public bool Truncated { get; private set; }
        }
    }
}
