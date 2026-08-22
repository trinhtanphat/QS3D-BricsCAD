using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    internal static class RuntimeSourceIdentityGuard
    {
        private const string SourceLinkRoot =
            "https://raw.githubusercontent.com/trinhtanphat/QS3D-BricsCAD/";

        public static void RequireExactSourceLink(Assembly assembly, string sourceSha, string label)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Assembly label is required.", nameof(label));

            var normalizedSha = (sourceSha ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedSha.Length != 40)
                throw new InvalidOperationException(label + " source SHA is invalid.");
            foreach (var character in normalizedSha)
            {
                if (!Uri.IsHexDigit(character))
                    throw new InvalidOperationException(label + " source SHA is invalid.");
            }

            var assemblyPath = assembly.Location;
            if (string.IsNullOrWhiteSpace(assemblyPath))
                throw new InvalidOperationException(label + " assembly location is unavailable.");
            var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
            if (string.IsNullOrWhiteSpace(pdbPath) || !File.Exists(pdbPath))
                throw new InvalidOperationException(label + " PDB SourceLink evidence is unavailable.");

            var sourceLinkPrefix = SourceLinkRoot + normalizedSha + "/";
            var pdbText = Encoding.UTF8.GetString(File.ReadAllBytes(pdbPath));
            if (pdbText.IndexOf(sourceLinkPrefix, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(label + " PDB SourceLink does not match exact source SHA.");
        }
    }
}
