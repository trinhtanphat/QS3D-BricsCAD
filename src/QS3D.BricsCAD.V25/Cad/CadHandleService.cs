using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadHandleService
    {
        private const int MaxHandleInputCount = 10000;

        public static IReadOnlyList<ObjectId> Resolve(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (handles == null) throw new ArgumentNullException(nameof(handles));

            ValidateKnownCount(handles);

            var candidates = new List<ObjectId>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rawCount = 0;
            foreach (var text in handles)
            {
                rawCount++;
                if (rawCount > MaxHandleInputCount)
                {
                    ThrowTooManyHandles(nameof(handles));
                }

                var normalized = NormalizeHexHandle(text);
                if (normalized == null || !seen.Add(normalized) || !long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                try
                {
                    var id = document.Database.GetObjectId(false, new Handle(value), 0);
                    if (!id.IsNull && id.IsValid) candidates.Add(id);
                }
                catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex)) { }
            }

            var result = new List<ObjectId>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in candidates)
                {
                    try
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity != null && !entity.IsErased) result.Add(id);
                    }
                    catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex)) { }
                }
                transaction.Commit();
            }
            return result;
        }

        public static string? NormalizeHexHandle(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!string.Equals(text, text!.Trim(), StringComparison.Ordinal)) return null;

            var normalized = text;
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(2);
            if (normalized.Length == 0 || !long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value <= 0L) return null;
            return value.ToString("X", CultureInfo.InvariantCulture);
        }

        public static int Select(Document document, IEnumerable<string> handles) => SelectIfAny(document, handles);

        public static int SelectIfAny(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var ids = Resolve(document, handles);
            if (ids.Count == 0) return 0;
            document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());
            return ids.Count;
        }

        public static void ClearSelection(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
        }

        public static ISet<string> GetLiveHandles(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in Resolve(document, handles))
            {
                try { result.Add(id.Handle.ToString()); }
                catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex)) { }
            }
            return result;
        }

        public static ISet<string> GetLiveSolidHandles(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = Resolve(document, handles);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                        if (solid != null && !solid.IsErased) result.Add(id.Handle.ToString());
                    }
                    catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex)) { }
                }
                transaction.Commit();
            }
            return result;
        }

        private static void ValidateKnownCount(IEnumerable<string> handles)
        {
            int? knownCount = null;
            var conflictingKnownCounts = false;
            var invalidKnownCount = false;

            if (handles is ICollection<string> collection)
            {
                ObserveKnownCount(collection.Count, ref knownCount, ref conflictingKnownCounts, ref invalidKnownCount);
            }

            if (handles is IReadOnlyCollection<string> readOnlyCollection)
            {
                ObserveKnownCount(readOnlyCollection.Count, ref knownCount, ref conflictingKnownCounts, ref invalidKnownCount);
            }

            if (handles is ICollection nonGenericCollection)
            {
                ObserveKnownCount(nonGenericCollection.Count, ref knownCount, ref conflictingKnownCounts, ref invalidKnownCount);
            }

            if (knownCount.HasValue && knownCount.Value > MaxHandleInputCount)
            {
                ThrowTooManyHandles(nameof(handles));
            }

            if (invalidKnownCount)
            {
                throw new ArgumentException("Handle input exposes an invalid negative known Count value.", nameof(handles));
            }

            if (conflictingKnownCounts)
            {
                throw new ArgumentException("Handle input exposes conflicting known Count values.", nameof(handles));
            }
        }

        private static void ObserveKnownCount(
            int observed,
            ref int? knownCount,
            ref bool conflictingKnownCounts,
            ref bool invalidKnownCount)
        {
            if (observed < 0) invalidKnownCount = true;
            if (knownCount.HasValue && knownCount.Value != observed) conflictingKnownCounts = true;
            if (!knownCount.HasValue || observed > knownCount.Value) knownCount = observed;
        }

        private static void ThrowTooManyHandles(string parameterName)
        {
            throw new ArgumentException($"Handle input exceeds the maximum of {MaxHandleInputCount} entries.", parameterName);
        }

        private static bool IsRecoverableDiagnosticFailure(Exception exception)
        {
            return !(exception is OutOfMemoryException) &&
                   !(exception is StackOverflowException) &&
                   !(exception is AccessViolationException);
        }
    }
}
