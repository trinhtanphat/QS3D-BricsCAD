using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedRebarReplacementGuard
    {
        public static void ValidateCompleteSet(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (ownership == null) throw new ArgumentNullException(nameof(ownership));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;

            var expected = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var original = token.Trim();
                if (original.Length == 0) continue;
                ownership.EnsureOwned(original, element, propertyKey);
                var canonical = CadHandleService.NormalizeHexHandle(original);
                if (canonical == null)
                    throw new InvalidOperationException(
                        "Generated rebar metadata contains an invalid handle. Refusing destructive replacement before any rebar is erased: " + original + ".");
                if (seen.Add(canonical)) expected.Add(canonical);
            }

            if (expected.Count == 0)
                throw new InvalidOperationException(
                    "Generated rebar metadata contains no valid handles. Refusing destructive replacement before any rebar is erased.");

            var ids = CadHandleService.Resolve(document, expected);
            if (ids.Count != expected.Count)
                throw new InvalidOperationException(
                    "Generated rebar live-handle set is incomplete for " + propertyKey + ". Refusing destructive replacement before any rebar is erased.");

            for (var index = 0; index < expected.Count; index++)
            {
                var entity = transaction.GetObject(ids[index], OpenMode.ForRead, false) as Entity;
                if (!(entity is Solid3d solid) || solid.IsErased)
                    throw new InvalidOperationException(
                        "Generated rebar is missing, erased, or not a Solid3d. Refusing destructive replacement before any rebar is erased: " + expected[index] + ".");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(
                    solid,
                    project,
                    element,
                    propertyKey,
                    "validate generated rebar replacement " + expected[index]);
            }
        }
    }
}
