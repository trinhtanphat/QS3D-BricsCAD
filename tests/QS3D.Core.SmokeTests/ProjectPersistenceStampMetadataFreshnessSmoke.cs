using System;
using System.Reflection;
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

            SetPersistenceMetadata(project, "Custom.Persistence.Flag", "enabled");
            if (project.ChangeVersion != initialVersion)
                throw new InvalidOperationException("Persistence metadata fixture must not advance ChangeVersion.");
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("Adding persisted metadata must make the persistence stamp pending even without a ChangeVersion increment.");

            stamp.MarkSaved(project);
            if (stamp.RequiresSave(project))
                throw new InvalidOperationException("MarkSaved must refresh the saved metadata snapshot.");

            SetPersistenceMetadata(project, "custom.persistence.flag", "enabled");
            if (project.ChangeVersion != initialVersion)
                throw new InvalidOperationException("Case-insensitive persistence metadata fixture must not advance ChangeVersion.");
            if (stamp.RequiresSave(project))
                throw new InvalidOperationException("Metadata freshness must follow the project's case-insensitive key semantics.");

            SetPersistenceMetadata(project, "Custom.Persistence.Flag", "Enabled");
            if (project.ChangeVersion != initialVersion)
                throw new InvalidOperationException("Value-change persistence metadata fixture must not advance ChangeVersion.");
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("Metadata value changes must use exact value semantics.");

            stamp.MarkSaved(project);
            if (!RemovePersistenceMetadata(project, "CUSTOM.PERSISTENCE.FLAG"))
                throw new InvalidOperationException("Smoke setup requires case-insensitive metadata removal.");
            if (project.ChangeVersion != initialVersion)
                throw new InvalidOperationException("Persistence metadata removal fixture must not advance ChangeVersion.");
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("Removing persisted metadata must make the persistence stamp pending.");
        }

        private static void SetPersistenceMetadata(ProjectState project, string key, string value)
        {
            var method = project.Metadata.GetType().GetMethod(
                "SetPersistenceValue",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Project metadata persistence setter is unavailable.");
            method.Invoke(project.Metadata, new object[] { key, value });
        }

        private static bool RemovePersistenceMetadata(ProjectState project, string key)
        {
            var method = project.Metadata.GetType().GetMethod(
                "RemoveOwned",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Project metadata persistence remover is unavailable.");
            return (bool)(method.Invoke(project.Metadata, new object[] { key }) ?? false);
        }
    }
}
