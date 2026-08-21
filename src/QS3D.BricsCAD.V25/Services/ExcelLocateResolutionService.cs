using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Services;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Services
{
    internal enum ExcelLocateFailureCode
    {
        ModernSchemaRequired,
        FingerprintMismatch,
        UnknownElementId,
        ProvenanceMismatch,
        NoLiveHandles,
        PartialResolution,
    }

    internal sealed class ExcelLocateResolutionException : InvalidOperationException
    {
        public ExcelLocateResolutionException(ExcelLocateFailureCode code, string message) : base(message)
        {
            Code = code;
        }

        public ExcelLocateFailureCode Code { get; }
    }

    internal sealed class ExcelLocateResolution
    {
        public ExcelLocateResolution(IEnumerable<string> handles, IEnumerable<ObjectId> objectIds)
        {
            Handles = (handles ?? throw new ArgumentNullException(nameof(handles))).ToList().AsReadOnly();
            ObjectIds = (objectIds ?? throw new ArgumentNullException(nameof(objectIds))).ToList().AsReadOnly();
        }

        public IReadOnlyList<string> Handles { get; }
        public IReadOnlyList<ObjectId> ObjectIds { get; }
    }

    /// <summary>
    /// Shared fail-closed resolver for modern Excel provenance. All semantic identity,
    /// fingerprint, Handle parity and live-CAD checks complete before a caller may replace PICKFIRST.
    /// </summary>
    internal static class ExcelLocateResolutionService
    {
        public static ExcelLocateResolution ResolveModern(
            Document document,
            ProjectState project,
            XlsxHandleLookupResult lookup)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));
            if (!lookup.IsModernSchema || !lookup.IsEd2Detail ||
                !string.Equals(lookup.WorksheetName, "CHI_TIET", StringComparison.OrdinalIgnoreCase))
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.ModernSchemaRequired,
                    "Modern Excel Locate requires a QS3D ED2 CHI_TIET row.");
            if (lookup.ElementIds.Count != 1)
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.UnknownElementId,
                    "ED2 CHI_TIET Locate requires exactly one QS3D Element ID.");
            return ResolveRow(
                document,
                project,
                lookup.ElementIds,
                lookup.Handles,
                lookup.DrawingFingerprint,
                true,
                "ED2 CHI_TIET");
        }

        public static ExcelLocateResolution ResolveCustomerTrace(
            Document document,
            ProjectState project,
            QsCustomerWorkbookTrace trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            var requireSingle = string.Equals(trace.WorksheetName, QsCustomerWorkbookExporter.DetailSheet, StringComparison.OrdinalIgnoreCase);
            return ResolveRow(
                document,
                project,
                trace.ElementIds,
                trace.Handles,
                trace.DrawingFingerprint,
                requireSingle,
                "Customer Excel " + trace.WorksheetName);
        }

        internal static ExcelLocateResolution ResolveModernRow(
            Document document,
            ProjectState project,
            IEnumerable<string> elementIds,
            IEnumerable<string> handles,
            string drawingFingerprint)
        {
            return ResolveRow(document, project, elementIds, handles, drawingFingerprint, true, "ED2 CHI_TIET");
        }

        private static ExcelLocateResolution ResolveRow(
            Document document,
            ProjectState project,
            IEnumerable<string> elementIds,
            IEnumerable<string> handles,
            string drawingFingerprint,
            bool requireSingleElement,
            string label)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("Excel Locate requires the reviewed DWG to remain active.");
            var ids = (elementIds ?? throw new ArgumentNullException(nameof(elementIds)))
                .Select(value => (value ?? string.Empty).Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0 || (requireSingleElement && ids.Count != 1))
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.UnknownElementId,
                    requireSingleElement
                        ? label + " Locate requires exactly one QS3D Element ID."
                        : label + " Locate requires at least one QS3D Element ID.");
            if (string.IsNullOrWhiteSpace(drawingFingerprint) ||
                !string.Equals(drawingFingerprint, project.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.FingerprintMismatch,
                    "Excel drawing fingerprint does not match the active DWG.");

            foreach (var elementId in ids)
                if (project.FindElement(elementId) == null)
                    throw new ExcelLocateResolutionException(
                        ExcelLocateFailureCode.UnknownElementId,
                        "Excel references an unknown QS3D Element ID: " + elementId + ".");

            var projectHandles = CanonicalHandles(SourceHandleResolver.Resolve(project, ids), "QS3D project");
            var excelHandles = CanonicalHandles(handles, "Excel");
            if (projectHandles.Count == 0 || excelHandles.Count == 0 ||
                !excelHandles.SequenceEqual(projectHandles, StringComparer.OrdinalIgnoreCase))
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.ProvenanceMismatch,
                    "Excel Element ID to CAD Handle provenance does not match the active QS3D project.");

            var resolved = CadHandleService.Resolve(document, projectHandles);
            if (resolved.Count == 0)
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.NoLiveHandles,
                    "Excel Locate could not resolve any CAD Handle. Selection was not changed; repair stale or missing provenance first.");
            if (resolved.Count != projectHandles.Count)
                throw new ExcelLocateResolutionException(
                    ExcelLocateFailureCode.PartialResolution,
                    "Excel Locate could not resolve every CAD Handle. Selection was not changed; repair stale or missing provenance first.");
            return new ExcelLocateResolution(projectHandles, resolved);
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> values, string label)
        {
            return (values ?? throw new ArgumentNullException(nameof(values)))
                .Select(value => CadHandleService.NormalizeHexHandle(value)
                    ?? throw new InvalidOperationException(label + " contains an invalid CAD Handle."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }
    }
}
