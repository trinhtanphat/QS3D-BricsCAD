using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampMetadataFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("STAMP-METADATA", "Metadata Freshness");
            var stamp = new ProjectPersistenceStamp(project);
            var initialVersion = project.ChangeVersion;

            project.Metadata["Custom.Persistence.Flag"] = "enabled";
            if (project.ChangeVersion != initialVersion)
                throw new InvalidOperationException("Smoke setup requires direct metadata mutation to leave ChangeVersion unchanged.");
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("Adding persisted metadata must make the persistence stamp pending even without a ChangeVersion increment.");

            stamp.MarkSaved(project);
            if (stamp.RequiresSave(project))
                throw new InvalidOperationException("MarkSaved must refresh the saved metadata snapshot.");

            project.Metadata["custom.persistence.flag"] = "enabled";
            if (stamp.RequiresSave(project))
                throw new InvalidOperationException("Metadata freshness must follow the project's case-insensitive key semantics.");

            project.Metadata["Custom.Persistence.Flag"] = "Enabled";
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("Metadata value changes must use exact value semantics.");

            stamp.MarkSaved(project);
            if (!project.Metadata.Remove("CUSTOM.PERSISTENCE.FLAG"))
                throw new InvalidOperationException("Smoke setup requires case-insensitive metadata removal.");
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("Removing persisted metadata must make the persistence stamp pending.");
        }
    }
}
