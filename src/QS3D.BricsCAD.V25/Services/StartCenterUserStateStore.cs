using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class StartCenterRecentProject
    {
        public string Path { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime LastOpenedUtc { get; set; }

        public bool Exists
        {
            get
            {
                try { return File.Exists(Path); }
                catch { return false; }
            }
        }

        public string DisplayName
        {
            get
            {
                try
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(Path);
                    return string.IsNullOrWhiteSpace(name) ? Path : name;
                }
                catch { return Path; }
            }
        }

        public string StateLabel => Exists ? (IsPinned ? "Đã ghim • Sẵn sàng" : "Sẵn sàng") : (IsPinned ? "Đã ghim • Thiếu file" : "Thiếu file");
    }

    internal sealed class StartCenterUserStateSnapshot
    {
        public IList<string> FavoriteCommands { get; set; } = new List<string>();
        public IList<string> RecentCommands { get; set; } = new List<string>();
        public IList<StartCenterRecentProject> RecentProjects { get; set; } = new List<StartCenterRecentProject>();
    }

    internal static class StartCenterUserStateStore
    {
        private const int MaxFileBytes = 256 * 1024;
        private const int MaxRecentCommands = 16;
        private const int MaxRecentProjects = 32;
        private const int MaxFavoriteCommands = 48;
        private static readonly object Gate = new object();
        private static StartCenterUserStateSnapshot _current = LoadCore();

        public static StartCenterUserStateSnapshot GetSnapshot()
        {
            lock (Gate) return Clone(_current);
        }

        public static void ToggleFavorite(string command)
        {
            if (!StartCenterCommandCatalog.TryGet(command, out var item)) return;
            lock (Gate)
            {
                var next = Clone(_current);
                var existing = next.FavoriteCommands.FirstOrDefault(x => string.Equals(x, item.Command, StringComparison.OrdinalIgnoreCase));
                if (existing != null) next.FavoriteCommands.Remove(existing);
                else
                {
                    if (next.FavoriteCommands.Count >= MaxFavoriteCommands) next.FavoriteCommands.RemoveAt(next.FavoriteCommands.Count - 1);
                    next.FavoriteCommands.Insert(0, item.Command);
                }
                _current = Normalize(next);
                TrySaveCore(_current);
            }
        }

        public static void RecordCommand(string command)
        {
            if (!StartCenterCommandCatalog.TryGet(command, out var item)) return;
            lock (Gate)
            {
                var next = Clone(_current);
                RemoveCommand(next.RecentCommands, item.Command);
                next.RecentCommands.Insert(0, item.Command);
                Trim(next.RecentCommands, MaxRecentCommands);
                _current = Normalize(next);
                TrySaveCore(_current);
            }
        }

        public static bool RecordProject(string path)
        {
            if (!TryNormalizeDwgPath(path, out var normalized)) return false;
            lock (Gate)
            {
                var next = Clone(_current);
                var existing = next.RecentProjects.FirstOrDefault(x => SamePath(x.Path, normalized));
                var pinned = existing?.IsPinned ?? false;
                if (existing != null) next.RecentProjects.Remove(existing);
                next.RecentProjects.Add(new StartCenterRecentProject
                {
                    Path = normalized,
                    IsPinned = pinned,
                    LastOpenedUtc = DateTime.UtcNow
                });
                _current = Normalize(next);
                TrySaveCore(_current);
                return true;
            }
        }

        public static void ToggleProjectPinned(string path)
        {
            if (!TryNormalizeDwgPath(path, out var normalized)) return;
            lock (Gate)
            {
                var next = Clone(_current);
                var existing = next.RecentProjects.FirstOrDefault(x => SamePath(x.Path, normalized));
                if (existing == null) return;
                existing.IsPinned = !existing.IsPinned;
                _current = Normalize(next);
                TrySaveCore(_current);
            }
        }

        public static void RemoveProject(string path)
        {
            if (!TryNormalizeDwgPath(path, out var normalized)) return;
            lock (Gate)
            {
                var next = Clone(_current);
                next.RecentProjects = next.RecentProjects.Where(x => !SamePath(x.Path, normalized)).ToList();
                _current = Normalize(next);
                TrySaveCore(_current);
            }
        }

        public static void ClearProjects()
        {
            lock (Gate)
            {
                var next = Clone(_current);
                next.RecentProjects.Clear();
                _current = Normalize(next);
                TrySaveCore(_current);
            }
        }

        internal static bool TryNormalizeDwgPath(string path, out string normalized)
        {
            normalized = string.Empty;
            var text = (path ?? string.Empty).Trim();
            if (text.Length == 0 || text.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0) return false;
            try
            {
                if (!System.IO.Path.IsPathRooted(text)) return false;
                var full = System.IO.Path.GetFullPath(text);
                if (!string.Equals(System.IO.Path.GetExtension(full), ".dwg", StringComparison.OrdinalIgnoreCase)) return false;
                normalized = full;
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is System.Security.SecurityException)
            {
                return false;
            }
        }

        private static StartCenterUserStateSnapshot LoadCore()
        {
            var state = new StartCenterUserStateSnapshot();
            try
            {
                if (!TrySettingsPath(out var path)) return state;
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length < 0 || stream.Length > MaxFileBytes) return state;
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
                    {
                        string? raw;
                        while ((raw = reader.ReadLine()) != null)
                        {
                            var line = raw.Trim();
                            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                            if (line.StartsWith("F=", StringComparison.Ordinal))
                            {
                                if (!TryDecode(line.Substring(2), out var command)) continue;
                                if (StartCenterCommandCatalog.TryGet(command, out var favorite)) state.FavoriteCommands.Add(favorite.Command);
                                continue;
                            }

                            if (line.StartsWith("C=", StringComparison.Ordinal))
                            {
                                if (!TryDecode(line.Substring(2), out var command)) continue;
                                if (StartCenterCommandCatalog.TryGet(command, out var recent)) state.RecentCommands.Add(recent.Command);
                                continue;
                            }

                            if (!line.StartsWith("P=", StringComparison.Ordinal)) continue;
                            var parts = line.Substring(2).Split(new[] { '|' }, 3);
                            if (parts.Length != 3) continue;
                            if (!long.TryParse(parts[1], out var ticks) || ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) continue;
                            if (!TryDecode(parts[2], out var decoded)) continue;
                            if (!TryNormalizeDwgPath(decoded, out var normalized)) continue;
                            state.RecentProjects.Add(new StartCenterRecentProject
                            {
                                IsPinned = string.Equals(parts[0], "1", StringComparison.Ordinal),
                                LastOpenedUtc = new DateTime(ticks, DateTimeKind.Utc),
                                Path = normalized
                            });
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
            catch (System.Security.SecurityException) { }

            return Normalize(state);
        }

        private static StartCenterUserStateSnapshot Normalize(StartCenterUserStateSnapshot state)
        {
            var favoriteSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            state.FavoriteCommands = state.FavoriteCommands
                .Where(x => StartCenterCommandCatalog.TryGet(x, out _) && favoriteSeen.Add(x))
                .Take(MaxFavoriteCommands)
                .ToList();

            var recentCommandSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            state.RecentCommands = state.RecentCommands
                .Where(x => StartCenterCommandCatalog.TryGet(x, out _) && recentCommandSeen.Add(x))
                .Take(MaxRecentCommands)
                .ToList();

            var projects = new Dictionary<string, StartCenterRecentProject>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in state.RecentProjects)
            {
                if (raw == null || !TryNormalizeDwgPath(raw.Path, out var normalized)) continue;
                if (!projects.TryGetValue(normalized, out var existing) || raw.LastOpenedUtc > existing.LastOpenedUtc)
                {
                    projects[normalized] = new StartCenterRecentProject
                    {
                        Path = normalized,
                        IsPinned = raw.IsPinned || (existing?.IsPinned ?? false),
                        LastOpenedUtc = raw.LastOpenedUtc.Kind == DateTimeKind.Utc ? raw.LastOpenedUtc : raw.LastOpenedUtc.ToUniversalTime()
                    };
                }
                else if (raw.IsPinned) existing.IsPinned = true;
            }

            state.RecentProjects = projects.Values
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.LastOpenedUtc)
                .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                .Take(MaxRecentProjects)
                .ToList();
            return state;
        }

        private static void TrySaveCore(StartCenterUserStateSnapshot state)
        {
            string? temp = null;
            try
            {
                if (!TrySettingsPath(out var path)) return;
                var directory = System.IO.Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temp, Serialize(state), new UTF8Encoding(false));
                if (!File.Exists(path))
                {
                    File.Move(temp, path);
                    temp = null;
                    return;
                }

                var backup = path + ".replace.bak";
                try
                {
                    File.Replace(temp, path, backup, true);
                    temp = null;
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(temp, path, true);
                    File.Delete(temp);
                    temp = null;
                }
                finally
                {
                    TryDelete(backup);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (NotSupportedException) { }
            catch (ArgumentException) { }
            catch (System.Security.SecurityException) { }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temp)) TryDelete(temp!);
            }
        }

        private static string Serialize(StartCenterUserStateSnapshot state)
        {
            var normalized = Normalize(Clone(state));
            var builder = new StringBuilder();
            builder.AppendLine("# QS3D Start Center user state v1");
            foreach (var favorite in normalized.FavoriteCommands) builder.Append("F=").AppendLine(Encode(favorite));
            foreach (var command in normalized.RecentCommands) builder.Append("C=").AppendLine(Encode(command));
            foreach (var project in normalized.RecentProjects)
            {
                builder.Append("P=")
                    .Append(project.IsPinned ? "1" : "0")
                    .Append('|')
                    .Append(project.LastOpenedUtc.Ticks)
                    .Append('|')
                    .AppendLine(Encode(project.Path));
            }
            return builder.ToString();
        }

        private static bool TrySettingsPath(out string path)
        {
            path = string.Empty;
            try
            {
                var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(root)) return false;
                path = System.IO.Path.Combine(root, "QS3D", "BricsCAD-V25", "start-center-v1.txt");
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException)
            {
                path = string.Empty;
                return false;
            }
        }

        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

        private static bool TryDecode(string value, out string decoded)
        {
            decoded = string.Empty;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static void RemoveCommand(IList<string> commands, string command)
        {
            for (var i = commands.Count - 1; i >= 0; i--)
                if (string.Equals(commands[i], command, StringComparison.OrdinalIgnoreCase)) commands.RemoveAt(i);
        }

        private static void Trim<T>(IList<T> items, int max)
        {
            while (items.Count > max) items.RemoveAt(items.Count - 1);
        }

        private static bool SamePath(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static StartCenterUserStateSnapshot Clone(StartCenterUserStateSnapshot source) => new StartCenterUserStateSnapshot
        {
            FavoriteCommands = source.FavoriteCommands.ToList(),
            RecentCommands = source.RecentCommands.ToList(),
            RecentProjects = source.RecentProjects.Select(x => new StartCenterRecentProject
            {
                Path = x.Path,
                IsPinned = x.IsPinned,
                LastOpenedUtc = x.LastOpenedUtc
            }).ToList()
        };

        private static void TryDelete(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (System.Security.SecurityException) { }
        }
    }
}
