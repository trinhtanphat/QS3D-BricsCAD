using System;
using System.IO;

namespace QS3D.Core.Persistence
{
    /// <summary>
    /// Fail-closed path checks shared by persistence write primitives.
    /// Persistence authority is expressed by the canonical path, so an existing
    /// symbolic-link/reparse-point component must never redirect a write or lock
    /// to a different filesystem object.
    /// </summary>
    internal static class PersistencePathSafety
    {
        public static void RequireNonRedirected(string fullPath, string role)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("A persistence path is required.", nameof(fullPath));

            var canonical = Path.GetFullPath(fullPath);
            var root = Path.GetPathRoot(canonical);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidDataException("QS3D could not resolve the persistence path root.");

            var separators = Path.AltDirectorySeparatorChar == Path.DirectorySeparatorChar
                ? new[] { Path.DirectorySeparatorChar }
                : new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var remainder = canonical.Substring(root.Length);
            var components = remainder.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            for (var index = 0; index < components.Length; index++)
            {
                current = Path.Combine(current, components[index]);
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(current);
                }
                catch (FileNotFoundException)
                {
                    // Once a component is missing, every deeper component is also
                    // non-existent at this observation. Callers recheck immediately
                    // before destructive filesystem operations.
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("QS3D refused a redirected or reparse-point " + role + " path.");
            }
        }
    }
}
