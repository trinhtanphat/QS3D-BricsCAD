using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedOwnershipIdentityToken
    {
        private const string ProjectPrefix = "p1:";
        private const string ElementPrefix = "e1:";

        public static string Project(string projectId)
        {
            return Build(ProjectPrefix, projectId, "Project id");
        }

        public static string Element(string elementId)
        {
            return Build(ElementPrefix, elementId, "Element id");
        }

        public static bool MatchesProject(string storedIdentity, string projectId)
        {
            return Matches(storedIdentity, projectId, ProjectPrefix, "Project id");
        }

        public static bool MatchesElement(string storedIdentity, string elementId)
        {
            return Matches(storedIdentity, elementId, ElementPrefix, "Element id");
        }

        private static bool Matches(string storedIdentity, string rawIdentity, string prefix, string label)
        {
            var normalized = Normalize(rawIdentity, label);
            return string.Equals(storedIdentity, BuildNormalized(prefix, normalized), StringComparison.Ordinal) ||
                string.Equals(storedIdentity, normalized, StringComparison.OrdinalIgnoreCase);
        }

        private static string Build(string prefix, string rawIdentity, string label)
        {
            return BuildNormalized(prefix, Normalize(rawIdentity, label));
        }

        private static string Normalize(string rawIdentity, string label)
        {
            var normalized = (rawIdentity ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException(label + " is required.");
            return normalized;
        }

        private static string BuildNormalized(string prefix, string normalized)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                var result = new StringBuilder(prefix.Length + hash.Length * 2);
                result.Append(prefix);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
