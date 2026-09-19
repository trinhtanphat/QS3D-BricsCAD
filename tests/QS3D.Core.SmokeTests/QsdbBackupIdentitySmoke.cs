using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbBackupIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsPadded();
            RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsMissing();
            RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsBlank();
        }

        private static void RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsPadded() =>
            RejectsCrossProjectBackupForAmbiguousPrimaryIdentity(
                root => root.SetAttributeValue("projectId", " PROJECT-A "),
                "padded");

        private static void RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsMissing() =>
            RejectsCrossProjectBackupForAmbiguousPrimaryIdentity(
                root => root.Attribute("projectId")?.Remove(),
                "missing");

        private static void RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsBlank() =>
            RejectsCrossProjectBackupForAmbiguousPrimaryIdentity(
                root => root.SetAttributeValue("projectId", "   "),
                "blank");

        private static void RejectsCrossProjectBackupForAmbiguousPrimaryIdentity(Action<XElement> corruptIdentity, string label)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-backup-identity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            var backupPath = path + ".bak";

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(new ProjectState("PROJECT-A", "Primary project"), path);
                store.SaveNew(new ProjectState("PROJECT-B", "Different backup project"), backupPath);

                var primary = XDocument.Load(path, LoadOptions.PreserveWhitespace);
                var root = primary.Root ?? throw new InvalidOperationException("Smoke fixture primary has no root.");
                corruptIdentity(root);
                primary.Save(path, SaveOptions.DisableFormatting);

                Throws<InvalidDataException>(
                    () => store.LoadWithBackupFallback(path),
                    "parseable primary with " + label + " identity must not authorize a different-project backup");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(directory)) Directory.Delete(directory, true);
                }
                catch
                {
                    // Test cleanup must not hide the persistence contract assertion.
                }
            }
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("QsdbBackupIdentitySmoke expected " + typeof(TException).Name + ": " + label + ".");
        }
    }
}
