using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbMetadataBackupFallbackSmoke
    {
        private const string BackupRecoveryReason = "Primary QSDB was invalid; loaded validated backup.";

        [ModuleInitializer]
        internal static void Initialize()
        {
            OversizedPrimaryMetadataFallsBackToValidBackup();
        }

        private static void OversizedPrimaryMetadataFallsBackToValidBackup()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-metadata-backup-fallback-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "project.qsdb");
            var backupPath = path + ".bak";
            Directory.CreateDirectory(root);

            try
            {
                var store = new QsdbProjectStore();
                var project = new ProjectState("P-METADATA-FALLBACK", "Metadata fallback");
                store.SaveNew(project, path);
                File.Copy(path, backupPath, true);

                var document = XDocument.Load(path, LoadOptions.None);
                var metadata = document.Root?.Element("metadata")
                    ?? throw new Exception("QSDB smoke fixture is missing metadata.");
                metadata.RemoveNodes();
                for (var index = 0; index <= 10000; index++)
                {
                    metadata.Add(new XElement(
                        "p",
                        new XAttribute("name", "Smoke.Metadata." + index),
                        new XAttribute("value", "v")));
                }
                document.Save(path, SaveOptions.DisableFormatting);

                var directRejected = false;
                try
                {
                    store.Load(path);
                }
                catch (InvalidDataException ex)
                {
                    directRejected = ex.Message.IndexOf("metadata", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                if (!directRejected)
                    throw new Exception("Direct QSDB load must fail closed with InvalidDataException when persisted project metadata exceeds the supported entry bound.");

                var recovered = store.LoadWithBackupFallback(path);
                if (!recovered.RecoveredFromBackup)
                    throw new Exception("QSDB load did not recover from the valid backup after oversized primary metadata was rejected.");
                if (!string.Equals(recovered.Project.ProjectId, project.ProjectId, StringComparison.Ordinal))
                    throw new Exception("QSDB backup recovery returned the wrong project identity.");
                if (!string.Equals(Path.GetFullPath(recovered.SourcePath), Path.GetFullPath(backupPath), StringComparison.OrdinalIgnoreCase))
                    throw new Exception("QSDB backup recovery did not report the backup as its source path.");
                if (!string.Equals(recovered.PrimaryFailureMessage, BackupRecoveryReason, StringComparison.Ordinal))
                    throw new Exception("QSDB backup recovery did not report the stable redacted primary failure reason.");
                if (recovered.PrimaryFailureMessage.IndexOf("metadata", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new Exception("QSDB backup recovery leaked primary metadata validation detail through the public recovery reason.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(root)) Directory.Delete(root, true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
