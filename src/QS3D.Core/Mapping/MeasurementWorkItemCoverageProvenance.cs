using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Mapping
{
    /// <summary>
    /// Immutable source identity captured for a measurement/work-item coverage snapshot.
    /// The values are copied from ProjectState so later project mutations cannot rewrite
    /// the provenance of an already-created matrix/export artifact.
    /// </summary>
    public sealed class MeasurementWorkItemCoverageProvenance
    {
        private MeasurementWorkItemCoverageProvenance(
            string projectId,
            string drawingFingerprint,
            long changeVersion,
            DateTime updatedUtc)
        {
            ProjectId = projectId;
            DrawingFingerprint = drawingFingerprint;
            ChangeVersion = changeVersion;
            UpdatedUtc = updatedUtc;
        }

        public string ProjectId { get; }
        public string DrawingFingerprint { get; }
        public long ChangeVersion { get; }
        public DateTime UpdatedUtc { get; }

        public static MeasurementWorkItemCoverageProvenance Capture(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (project.ChangeVersion < 0L)
                throw new ArgumentOutOfRangeException(nameof(project), "Project change version cannot be negative.");
            if (project.UpdatedUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Project updated timestamp must be UTC.", nameof(project));

            return new MeasurementWorkItemCoverageProvenance(
                project.ProjectId,
                project.DrawingFingerprint ?? string.Empty,
                project.ChangeVersion,
                project.UpdatedUtc);
        }
    }
}
