using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class UserUiLayout
    {
        public int WorkspacePaletteWidth { get; set; } = 640;
        public int WorkspacePaletteHeight { get; set; } = 720;
        public int RightPaletteWidth { get; set; } = 300;
        public int RightPaletteHeight { get; set; } = 720;
        public int QuantityPaletteWidth { get; set; } = 330;
        public int QuantityPaletteHeight { get; set; } = 720;
        public double ModelColumnWidth { get; set; } = 160d;
        public double FamilyColumnWidth { get; set; } = 245d;
        public double FamilyTopHeight { get; set; } = 250d;
        public double RoomTopHeight { get; set; } = 218d;
    }

    internal static class UserUiLayoutStore
    {
        internal const int WorkspacePaletteMinWidth = 460;
        internal const int WorkspacePaletteMinHeight = 420;
        internal const int RightPaletteMinWidth = 255;
        internal const int RightPaletteMinHeight = 480;
        internal const int QuantityPaletteMinWidth = 280;
        internal const int QuantityPaletteMinHeight = 360;

        private const int MaxFileBytes = 16 * 1024;
#if BRICSCAD_V26
        private const string HostMajorDirectory = "BricsCAD-V26";
#else
        private const string HostMajorDirectory = "BricsCAD-V25";
#endif
        private static readonly object Gate = new object();
        private static UserUiLayout _current = LoadCore();

        public static UserUiLayout Get()
        {
            lock (Gate) return Clone(_current);
        }

        public static void Update(Action<UserUiLayout> update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));
            lock (Gate)
            {
                var next = Clone(_current);
                update(next);
                Normalize(next);
                if (Equivalent(_current, next)) return;
                _current = next;
                TrySaveCore(next);
            }
        }

        private static UserUiLayout LoadCore()
        {
            var layout = new UserUiLayout();
            try
            {
                var path = SettingsPath();
                if (!File.Exists(path)) return layout;
                var info = new FileInfo(path);
                if (info.Length < 0 || info.Length > MaxFileBytes) return layout;
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var line = (rawLine ?? string.Empty).Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    var equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    var key = line.Substring(0, equals).Trim();
                    var value = line.Substring(equals + 1).Trim();
                    if (key.Length > 0) values[key] = value;
                }

                layout.WorkspacePaletteWidth = Int(values, "WorkspacePaletteWidth", layout.WorkspacePaletteWidth);
                layout.WorkspacePaletteHeight = Int(values, "WorkspacePaletteHeight", layout.WorkspacePaletteHeight);
                layout.RightPaletteWidth = Int(values, "RightPaletteWidth", layout.RightPaletteWidth);
                layout.RightPaletteHeight = Int(values, "RightPaletteHeight", layout.RightPaletteHeight);
                layout.QuantityPaletteWidth = Int(values, "QuantityPaletteWidth", layout.QuantityPaletteWidth);
                layout.QuantityPaletteHeight = Int(values, "QuantityPaletteHeight", layout.QuantityPaletteHeight);
                layout.ModelColumnWidth = Double(values, "ModelColumnWidth", layout.ModelColumnWidth);
                layout.FamilyColumnWidth = Double(values, "FamilyColumnWidth", layout.FamilyColumnWidth);
                layout.FamilyTopHeight = Double(values, "FamilyTopHeight", layout.FamilyTopHeight);
                layout.RoomTopHeight = Double(values, "RoomTopHeight", layout.RoomTopHeight);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (NotSupportedException) { }
            catch (ArgumentException) { }
            Normalize(layout);
            return layout;
        }

        private static void TrySaveCore(UserUiLayout layout)
        {
            string? temp = null;
            try
            {
                var path = SettingsPath();
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temp, Serialize(layout), new UTF8Encoding(false));
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
            finally
            {
                if (!string.IsNullOrWhiteSpace(temp)) TryDelete(temp!);
            }
        }

        private static string Serialize(UserUiLayout layout)
        {
            var invariant = CultureInfo.InvariantCulture;
            var builder = new StringBuilder();
            builder.AppendLine("# QS3D per-user UI layout v1");
            builder.Append("WorkspacePaletteWidth=").AppendLine(layout.WorkspacePaletteWidth.ToString(invariant));
            builder.Append("WorkspacePaletteHeight=").AppendLine(layout.WorkspacePaletteHeight.ToString(invariant));
            builder.Append("RightPaletteWidth=").AppendLine(layout.RightPaletteWidth.ToString(invariant));
            builder.Append("RightPaletteHeight=").AppendLine(layout.RightPaletteHeight.ToString(invariant));
            builder.Append("QuantityPaletteWidth=").AppendLine(layout.QuantityPaletteWidth.ToString(invariant));
            builder.Append("QuantityPaletteHeight=").AppendLine(layout.QuantityPaletteHeight.ToString(invariant));
            builder.Append("ModelColumnWidth=").AppendLine(layout.ModelColumnWidth.ToString("R", invariant));
            builder.Append("FamilyColumnWidth=").AppendLine(layout.FamilyColumnWidth.ToString("R", invariant));
            builder.Append("FamilyTopHeight=").AppendLine(layout.FamilyTopHeight.ToString("R", invariant));
            builder.Append("RoomTopHeight=").AppendLine(layout.RoomTopHeight.ToString("R", invariant));
            return builder.ToString();
        }

        private static string SettingsPath()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("LocalApplicationData is unavailable.");
            return Path.Combine(root, "QS3D", HostMajorDirectory, "ui-layout-v1.txt");
        }

        private static void Normalize(UserUiLayout layout)
        {
            layout.WorkspacePaletteWidth = Clamp(layout.WorkspacePaletteWidth, WorkspacePaletteMinWidth, 1600);
            layout.WorkspacePaletteHeight = Clamp(layout.WorkspacePaletteHeight, WorkspacePaletteMinHeight, 2000);
            layout.RightPaletteWidth = Clamp(layout.RightPaletteWidth, RightPaletteMinWidth, 1200);
            layout.RightPaletteHeight = Clamp(layout.RightPaletteHeight, RightPaletteMinHeight, 2000);
            layout.QuantityPaletteWidth = Clamp(layout.QuantityPaletteWidth, QuantityPaletteMinWidth, 1200);
            layout.QuantityPaletteHeight = Clamp(layout.QuantityPaletteHeight, QuantityPaletteMinHeight, 2000);
            layout.ModelColumnWidth = Clamp(layout.ModelColumnWidth, 135d, 500d, 160d);
            layout.FamilyColumnWidth = Clamp(layout.FamilyColumnWidth, 220d, 700d, 245d);
            layout.FamilyTopHeight = Clamp(layout.FamilyTopHeight, 160d, 900d, 250d);
            layout.RoomTopHeight = Clamp(layout.RoomTopHeight, 135d, 900d, 218d);
        }

        private static bool Equivalent(UserUiLayout left, UserUiLayout right)
        {
            return left.WorkspacePaletteWidth == right.WorkspacePaletteWidth &&
                   left.WorkspacePaletteHeight == right.WorkspacePaletteHeight &&
                   left.RightPaletteWidth == right.RightPaletteWidth &&
                   left.RightPaletteHeight == right.RightPaletteHeight &&
                   left.QuantityPaletteWidth == right.QuantityPaletteWidth &&
                   left.QuantityPaletteHeight == right.QuantityPaletteHeight &&
                   left.ModelColumnWidth == right.ModelColumnWidth &&
                   left.FamilyColumnWidth == right.FamilyColumnWidth &&
                   left.FamilyTopHeight == right.FamilyTopHeight &&
                   left.RoomTopHeight == right.RoomTopHeight;
        }

        private static int Int(IDictionary<string, string> values, string key, int fallback) =>
            values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

        private static double Double(IDictionary<string, string> values, string key, double fallback) =>
            values.TryGetValue(key, out var raw) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;

        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

        private static double Clamp(double value, double min, double max, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        private static UserUiLayout Clone(UserUiLayout source) => new UserUiLayout
        {
            WorkspacePaletteWidth = source.WorkspacePaletteWidth,
            WorkspacePaletteHeight = source.WorkspacePaletteHeight,
            RightPaletteWidth = source.RightPaletteWidth,
            RightPaletteHeight = source.RightPaletteHeight,
            QuantityPaletteWidth = source.QuantityPaletteWidth,
            QuantityPaletteHeight = source.QuantityPaletteHeight,
            ModelColumnWidth = source.ModelColumnWidth,
            FamilyColumnWidth = source.FamilyColumnWidth,
            FamilyTopHeight = source.FamilyTopHeight,
            RoomTopHeight = source.RoomTopHeight
        };

        private static void TryDelete(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
