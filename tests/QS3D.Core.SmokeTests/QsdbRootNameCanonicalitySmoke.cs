using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbRootNameCanonicalitySmoke
    {
        internal static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-root-name-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("root-name-project", "Root name canonicality"), path);
                var canonical = File.ReadAllText(path);

                Require(canonical.Contains("<qs3d ", StringComparison.Ordinal), "Saved QSDB did not contain the canonical qs3d root token.");
                store.Load(path);

                var nonCanonical = canonical.Replace("<qs3d ", "<QS3D ", StringComparison.Ordinal)
                    .Replace("</qs3d>", "</QS3D>", StringComparison.Ordinal);
                Require(!string.Equals(nonCanonical, canonical, StringComparison.Ordinal), "Root-name fixture did not mutate the canonical root token.");
                File.WriteAllText(path, nonCanonical);
                Throws<InvalidDataException>(() => store.Load(path));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
                SafeDelete(path + ".tmp");
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
