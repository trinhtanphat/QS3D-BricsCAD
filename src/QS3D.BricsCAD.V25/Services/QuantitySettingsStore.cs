using System;
using System.IO;
using System.Runtime.Serialization.Json;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.Services
{
    public sealed class QuantitySettingsStore
    {
        private const string SettingsFileName = "quantity_settings.json";
        private readonly string _settingsPath;

        public QuantitySettingsStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QS3D",
                SettingsFileName))
        {
        }

        internal QuantitySettingsStore(string settingsPath)
        {
            if (string.IsNullOrWhiteSpace(settingsPath)) throw new ArgumentException("Settings path is required.", nameof(settingsPath));
            _settingsPath = Path.GetFullPath(settingsPath);
        }

        public string SettingsPath => _settingsPath;

        public QuantityCalculationSettings Load()
        {
            if (!File.Exists(_settingsPath)) return QuantityCalculationSettings.CreateDefault();
            return ReadAndValidate(_settingsPath);
        }

        public QuantityCalculationSettings Import(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Template path is required.", nameof(path));
            return ReadAndValidate(Path.GetFullPath(path));
        }

        public void Save(QuantityCalculationSettings settings)
        {
            WriteAtomic(_settingsPath, Prepare(settings));
        }

        public void Export(string path, QuantityCalculationSettings settings)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Template path is required.", nameof(path));
            WriteAtomic(Path.GetFullPath(path), Prepare(settings));
        }

        private static QuantityCalculationSettings Prepare(QuantityCalculationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var copy = settings.Clone();
            if (copy.SchemaVersion <= 0) copy.SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion;
            copy.NormalizeAndValidate();
            return copy;
        }

        private static QuantityCalculationSettings ReadAndValidate(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Quantity settings template was not found.", path);
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(QuantityCalculationSettings));
                    var value = serializer.ReadObject(stream) as QuantityCalculationSettings;
                    if (value == null) throw new InvalidDataException("Quantity settings template is empty or has an unsupported root object.");
                    value.NormalizeAndValidate();
                    return value;
                }
            }
            catch (Exception ex) when (!(ex is FileNotFoundException))
            {
                throw new InvalidDataException("Cannot read quantity settings template '" + path + "': " + ex.Message, ex);
            }
        }

        private static void WriteAtomic(string path, QuantityCalculationSettings settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Quantity settings path must have a parent directory.");
            Directory.CreateDirectory(directory);

            var temp = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var serializer = new DataContractJsonSerializer(typeof(QuantityCalculationSettings));
                    serializer.WriteObject(stream, settings);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    var backup = path + ".bak";
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Replace(temp, path, backup, true);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
