using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Commercial;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialQsWorkbookPreFilesystemValidationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OverlongCellFailsBeforeFilesystemSideEffects();
            MalformedUtf16FailsBeforeFilesystemSideEffects();
        }

        private static void OverlongCellFailsBeforeFilesystemSideEffects()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-commercial-prefilesystem-overlong-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "nested", "commercial.xlsx");
            try
            {
                Throws<InvalidDataException>(() => CommercialQsWorkbook.Export(path, Snapshot(new string('X', 32768))));
                True(!Directory.Exists(root), "Overlong commercial workbook cell must fail before creating the destination directory.");
            }
            finally
            {
                TryDelete(root);
            }
        }

        private static void MalformedUtf16FailsBeforeFilesystemSideEffects()
        {
            var snapshot = Snapshot("valid-description");
            var variation = snapshot.Variations!.Variations[0];
            var field = typeof(CommercialVariation).GetField("<Description>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Commercial variation description backing field changed; update hostile fixture intentionally.");
            field.SetValue(variation, "malformed-\uD800");

            var absentRoot = Path.Combine(Path.GetTempPath(), "qs3d-commercial-prefilesystem-unicode-" + Guid.NewGuid().ToString("N"));
            var absentPath = Path.Combine(absentRoot, "nested", "commercial.xlsx");
            try
            {
                Throws<EncoderFallbackException>(() => CommercialQsWorkbook.Export(absentPath, snapshot));
                True(!Directory.Exists(absentRoot), "Malformed commercial workbook UTF-16 must fail before creating the destination directory.");
            }
            finally
            {
                TryDelete(absentRoot);
            }

            var existingRoot = Path.Combine(Path.GetTempPath(), "qs3d-commercial-prefilesystem-existing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(existingRoot);
            var existingPath = Path.Combine(existingRoot, "commercial.xlsx");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44 };
            File.WriteAllBytes(existingPath, sentinel);
            var before = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            try
            {
                Throws<EncoderFallbackException>(() => CommercialQsWorkbook.Export(existingPath, snapshot));
                True(File.ReadAllBytes(existingPath).SequenceEqual(sentinel), "Malformed commercial workbook UTF-16 must preserve an existing destination.");
                var after = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                True(before.SequenceEqual(after, StringComparer.Ordinal), "Malformed commercial workbook UTF-16 must not leave an owned temporary package.");
            }
            finally
            {
                TryDelete(existingRoot);
            }
        }

        private static CommercialQsWorkbookSnapshot Snapshot(string description)
        {
            var variation = new CommercialVariation(
                "VO-PREFLIGHT",
                description,
                "VND",
                1m,
                1m,
                CommercialVariationStatus.Approved,
                new CommercialRevisionRef("variation", "VO-PREFLIGHT", "R1"));
            return new CommercialQsWorkbookSnapshot(
                new CommercialVariationRegister("VND", new[] { variation }),
                null, null, null, null, null, null);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void TryDelete(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }
    }
}
