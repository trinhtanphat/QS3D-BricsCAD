using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectLoadResult
    {
        public ProjectLoadResult(ProjectState project, string sourcePath, bool recoveredFromBackup, string primaryFailureMessage)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            SourcePath = sourcePath ?? string.Empty;
            RecoveredFromBackup = recoveredFromBackup;
            PrimaryFailureMessage = primaryFailureMessage ?? string.Empty;
        }

        public ProjectState Project { get; }
        public string SourcePath { get; }
        public bool RecoveredFromBackup { get; }
        public string PrimaryFailureMessage { get; }
    }
}
