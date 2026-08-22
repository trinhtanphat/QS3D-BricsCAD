using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QS3D.Core.Reporting
{
    [DataContract]
    public sealed class QuantityCalculationMatrixDiagnosticPairSnapshot
    {
        [DataMember(Name = "sourceCode", Order = 1)]
        private int _sourceCode;

        [DataMember(Name = "targetCode", Order = 2)]
        private int _targetCode;

        private QuantityCalculationMatrixDiagnosticPairSnapshot()
        {
        }

        internal QuantityCalculationMatrixDiagnosticPairSnapshot(int sourceCode, int targetCode)
        {
            _sourceCode = sourceCode;
            _targetCode = targetCode;
        }

        public int SourceCode => _sourceCode;
        public int TargetCode => _targetCode;
    }

    [DataContract]
    public sealed class QuantityCalculationMatrixDiagnosticSnapshot
    {
        [DataMember(Name = "schemaVersion", Order = 1)]
        private int _schemaVersion;

        [DataMember(Name = "observedCategoryCodes", Order = 2)]
        private List<int> _observedCategoryCodes = new List<int>();

        [DataMember(Name = "intersectionOnlyCategoryCodes", Order = 3)]
        private List<int> _intersectionOnlyCategoryCodes = new List<int>();

        [DataMember(Name = "unreferencedCategoryRuleCodes", Order = 4)]
        private List<int> _unreferencedCategoryRuleCodes = new List<int>();

        [DataMember(Name = "existingDirectedRuleCount", Order = 5)]
        private int _existingDirectedRuleCount;

        [DataMember(Name = "expectedDirectedRuleCount", Order = 6)]
        private long _expectedDirectedRuleCount;

        [DataMember(Name = "isCompleteDirectedMatrix", Order = 7)]
        private bool _isCompleteDirectedMatrix;

        [DataMember(Name = "missingDirectedPairs", Order = 8)]
        private List<QuantityCalculationMatrixDiagnosticPairSnapshot> _missingDirectedPairs =
            new List<QuantityCalculationMatrixDiagnosticPairSnapshot>();

        private QuantityCalculationMatrixDiagnosticSnapshot()
        {
        }

        private QuantityCalculationMatrixDiagnosticSnapshot(
            int schemaVersion,
            QuantityCalculationMatrixDiagnosticResult diagnostics)
        {
            _schemaVersion = schemaVersion;
            _observedCategoryCodes = diagnostics.ObservedCategoryCodes.ToList();
            _intersectionOnlyCategoryCodes = diagnostics.IntersectionOnlyCategoryCodes.ToList();
            _unreferencedCategoryRuleCodes = diagnostics.UnreferencedCategoryRuleCodes.ToList();
            _existingDirectedRuleCount = diagnostics.ExistingDirectedRuleCount;
            _expectedDirectedRuleCount = diagnostics.ExpectedDirectedRuleCount;
            _isCompleteDirectedMatrix = diagnostics.IsCompleteDirectedMatrix;
            _missingDirectedPairs = diagnostics.MissingDirectedPairs
                .Select(x => new QuantityCalculationMatrixDiagnosticPairSnapshot(x.SourceCode, x.TargetCode))
                .ToList();
        }

        public int SchemaVersion => _schemaVersion;
        public IReadOnlyList<int> ObservedCategoryCodes => new ReadOnlyCollection<int>(_observedCategoryCodes);
        public IReadOnlyList<int> IntersectionOnlyCategoryCodes => new ReadOnlyCollection<int>(_intersectionOnlyCategoryCodes);
        public IReadOnlyList<int> UnreferencedCategoryRuleCodes => new ReadOnlyCollection<int>(_unreferencedCategoryRuleCodes);
        public int ExistingDirectedRuleCount => _existingDirectedRuleCount;
        public long ExpectedDirectedRuleCount => _expectedDirectedRuleCount;
        public bool IsCompleteDirectedMatrix => _isCompleteDirectedMatrix;
        public IReadOnlyList<QuantityCalculationMatrixDiagnosticPairSnapshot> MissingDirectedPairs =>
            new ReadOnlyCollection<QuantityCalculationMatrixDiagnosticPairSnapshot>(_missingDirectedPairs);

        public static QuantityCalculationMatrixDiagnosticSnapshot Create(QuantityCalculationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var snapshot = settings.Clone();
            snapshot.NormalizeAndValidate();
            var diagnostics = QuantityCalculationMatrixDiagnostics.Analyze(snapshot);
            return new QuantityCalculationMatrixDiagnosticSnapshot(snapshot.SchemaVersion, diagnostics);
        }
    }

    public static class QuantityCalculationMatrixDiagnosticSnapshotExporter
    {
        public static void Save(string path, QuantityCalculationMatrixDiagnosticSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Diagnostic snapshot path is required.", nameof(path));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Diagnostic snapshot path must have a parent directory.");
            Directory.CreateDirectory(directory);

            var temp = Path.Combine(
                directory,
                "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Write(stream, snapshot);
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                    File.Replace(temp, fullPath, null, true);
                else
                    File.Move(temp, fullPath);
            }
            finally
            {
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch
                {
                    // Diagnostic temp cleanup is best-effort and must not mask the publish failure.
                }
            }
        }

        public static void Write(Stream stream, QuantityCalculationMatrixDiagnosticSnapshot snapshot)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!stream.CanWrite) throw new ArgumentException("Diagnostic snapshot stream must be writable.", nameof(stream));

            var serializer = new DataContractJsonSerializer(typeof(QuantityCalculationMatrixDiagnosticSnapshot));
            serializer.WriteObject(stream, snapshot);
            stream.Flush();
        }
    }
}