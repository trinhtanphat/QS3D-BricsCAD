using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Bricscad.ApplicationServices;
using QS3D.Core.Agent;
using QS3D.Core.Domain;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// QS3D business-domain MCP runtime. This boundary owns project/family/floor semantics
    /// and deliberately does not participate in native cad_* capability availability.
    /// </summary>
    internal static class McpQs3dDomainRuntime
    {
        private static readonly object Sync = new object();
        private static bool _available = true;
        private static string _lastErrorCode = string.Empty;
        private static string _lastErrorMessage = string.Empty;

        internal static bool IsTool(string? tool)
        {
            return string.Equals(tool, "qs3d_status", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_domain_status", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_project_bind", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_project_reload", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal);
        }

        internal static bool RequiresMutation(string? tool)
        {
            return string.Equals(tool, "qs3d_project_bind", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_project_reload", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal);
        }

        internal static void ResetForServerStart()
        {
            lock (Sync)
            {
                _available = true;
                _lastErrorCode = string.Empty;
                _lastErrorMessage = string.Empty;
            }
        }

        internal static string BuildStatusJson(bool deprecatedAlias)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var contextAvailable = false;
            var contextReason = string.Empty;
            if (document == null)
            {
                contextReason = "No active BricsCAD document; QS3D business context cannot be resolved.";
            }
            else
            {
                try
                {
                    ProjectState project;
                    contextAvailable = ExistingProjectMutationContext.TryGet(document, out project);
                    if (!contextAvailable)
                        contextReason = "No persisted QS3D project context. Call qs3d_project_bind with createIfMissing=false to bind an existing sidecar or createIfMissing=true to explicitly create and persist one before qs3d_* mutations.";
                }
                catch (Exception ex)
                {
                    var failure = McpToolCapabilityContract.ClassifyFailure("qs3d_domain_status", ex);
                    RecordFailure(failure);
                    contextReason = failure.Message;
                }
            }

            bool available;
            string errorCode;
            string errorMessage;
            lock (Sync)
            {
                available = _available;
                errorCode = _lastErrorCode;
                errorMessage = _lastErrorMessage;
            }

            var build = McpRuntimeBuildProvenance.Current;
            var errorJson = string.IsNullOrWhiteSpace(errorCode)
                ? "null"
                : "{\"code\":\"" + Escape(errorCode) + "\",\"message\":\"" + Escape(errorMessage) + "\"}";
            return "{\"lane\":\"qs3d_domain\",\"available\":" + JsonBool(available)
                + ",\"context\":{\"available\":" + JsonBool(contextAvailable) + ",\"reason\":\"" + Escape(contextReason) + "\"}"
                + ",\"buildSha\":\"" + Escape(build.BuildSha) + "\""
                + ",\"buildId\":\"" + Escape(build.BuildId) + "\""
                + ",\"buildUtc\":\"" + Escape(build.BuildUtc) + "\""
                + ",\"lastError\":" + errorJson + ",\"deprecatedAlias\":" + JsonBool(deprecatedAlias) + "}";
        }

        internal static string Call(string tool, string arguments)
        {
            if (!RequiresMutation(tool))
                throw new InvalidOperationException("Unknown QS3D domain mutation tool: " + tool);
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            try
            {
                var result = McpDiagnosticHub.InvokeInCadContext(() =>
                {
                    McpCadAgentRuntime.EnsureCurrentMutationRunning();
                    if (string.Equals(tool, "qs3d_project_bind", StringComparison.Ordinal)) return BindProject(body);
                    if (string.Equals(tool, "qs3d_project_reload", StringComparison.Ordinal)) return ReloadProject();
                    if (string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal)) return RunQs3dCommand(body);
                    if (string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal)) return PlaceSingleFooting(body);
                    throw new InvalidOperationException("Unknown QS3D domain mutation tool: " + tool);
                });
                RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                RecordFailure(McpToolCapabilityContract.ClassifyFailure(tool, ex));
                throw;
            }
        }

        private static string BindProject(string body)
        {
            var document = RequireDocument();
            var createIfMissing = McpTopLevelJson.ExtractBoolean(body, "createIfMissing");
            ProjectState project;
            if (ExistingProjectMutationContext.TryGet(document, out project))
            {
                ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "QS3D MCP project bind");
                McpCadAgentRuntime.AuditDomainMutation("qs3d_project_bind", "created=false; restored=true; projectId=" + project.ProjectId);
                return ProjectContextJson(project, false, true);
            }

            if (!createIfMissing)
                throw new InvalidOperationException(
                    "No existing QS3D project is attached to the active document. Set createIfMissing=true only when you intend to create and persist a new project for this DWG.");

            var drawingPath = document.Database.Filename ?? string.Empty;
            if (!Path.IsPathRooted(drawingPath))
                throw new InvalidOperationException(
                    "qs3d_project_bind cannot create a restart-persistent project for an unsaved drawing. Save the DWG to a local path first.");

            project = ProjectContextCoordinator.GetOrCreate(document);
            var sidecarPath = ProjectContextCoordinator.Save(document);
            if (!File.Exists(sidecarPath) && !File.Exists(sidecarPath + ".bak"))
            {
                ProjectContextCoordinator.Forget(document);
                throw new InvalidOperationException("QS3D project creation returned without a persisted sidecar; binding was rolled back.");
            }
            McpCadAgentRuntime.AuditDomainMutation("qs3d_project_bind", "created=true; restored=false; projectId=" + project.ProjectId);
            return ProjectContextJson(project, true, false);
        }

        private static string ReloadProject()
        {
            var document = RequireDocument();
            var project = ProjectContextCoordinator.Reload(document);
            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "QS3D MCP project reload");
            McpCadAgentRuntime.AuditDomainMutation("qs3d_project_reload", "restored=true; projectId=" + project.ProjectId);
            return ProjectContextJson(project, false, true);
        }

        private static string ProjectContextJson(ProjectState project, bool created, bool restored)
        {
            return "{\"bound\":true,\"created\":" + JsonBool(created)
                + ",\"restored\":" + JsonBool(restored)
                + ",\"projectId\":\"" + Escape(project.ProjectId) + "\"}"
                ;
        }

        private static string RunQs3dCommand(string body)
        {
            var command = McpTopLevelJson.ExtractString(body, "command").Trim();
            if (command.Length == 0 || command.Length > 80 || !Regex.IsMatch(command, McpCadAgentRuntime.Qs3dCommandPattern, RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Only one QS3D command name matching " + McpCadAgentRuntime.Qs3dCommandPattern + " is allowed.");
            McpCadAgentRuntime.EnsureCurrentMutationRunning();
            var document = RequireDocument();
            McpCadMutationCoordinator.QueueNativeCommand(
                document,
                command,
                () => SendQs3dCommand(document, command),
                detail => McpCadAgentRuntime.AuditDomainMutation("qs3d_run_command", detail));
            McpCadAgentRuntime.AuditDomainMutation("qs3d_run_command", "command=" + command.ToUpperInvariant());
            return "{\"accepted\":true,\"command\":\"" + Escape(command.ToUpperInvariant()) + "\"}";
        }

        private static void SendQs3dCommand(Document document, string command)
        {
            document.SendStringToExecute(command + "\n", true, false, true);
        }

        private static string PlaceSingleFooting(string body)
        {
            var x = NumberRequired(body, "x");
            var y = NumberRequired(body, "y");
            McpCadAgentRuntime.EnsureCurrentMutationRunning();
            var document = RequireDocument();
            var handle = SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d));
            McpCadAgentRuntime.AuditDomainMutation("qs3d_place_single_footing", "handle=" + handle);
            return "{\"created\":true,\"handle\":\"" + Escape(handle) + "\",\"type\":\"SingleFooting\",\"elevationPolicy\":\"active-floor\"}";
        }

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document is available.");
            return document;
        }

        private static double NumberRequired(string body, string property)
        {
            double value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractDouble(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) throw new InvalidOperationException(property + " must be a finite number.");
            return value;
        }

        private static void RecordSuccess()
        {
            lock (Sync)
            {
                _available = true;
                _lastErrorCode = string.Empty;
                _lastErrorMessage = string.Empty;
            }
        }

        private static void RecordFailure(McpToolFailure failure)
        {
            if (failure == null) return;
            lock (Sync)
            {
                _lastErrorCode = failure.Code;
                _lastErrorMessage = failure.Message;
                if (failure.Code == McpToolCapabilityContract.Qs3dSourceBugCode
                    || failure.Code == McpToolCapabilityContract.Qs3dDomainUnavailableCode)
                    _available = false;
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
