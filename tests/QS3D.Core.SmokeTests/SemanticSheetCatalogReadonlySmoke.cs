using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetCatalogReadonlySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var catalog = SemanticSheetPlanner.BuildCatalog(
                new[]
                {
                    new SemanticSheetDefinition(
                        "SHEET-200",
                        "A-200",
                        "Second",
                        420d,
                        297d,
                        Array.Empty<SemanticSheetPlacementDefinition>()),
                    new SemanticSheetDefinition(
                        "SHEET-100",
                        "A-100",
                        "First",
                        420d,
                        297d,
                        Array.Empty<SemanticSheetPlacementDefinition>())
                },
                Array.Empty<SemanticViewPlan>());

            Equal(2, catalog.Count);
            Equal("A-100", catalog[0].Number);
            Equal("A-200", catalog[1].Number);

            if (!(catalog is IList<SemanticSheetPlan> mutable))
                throw new InvalidOperationException("Semantic sheet catalog must expose the standard read-only IList contract.");

            var first = catalog[0];
            var second = catalog[1];
            Throws<NotSupportedException>(() => mutable[0] = second);
            Throws<NotSupportedException>(() => mutable.Add(first));
            Throws<NotSupportedException>(() => mutable.Remove(first));

            Equal(2, catalog.Count);
            Equal("SHEET-100", catalog[0].Id);
            Equal("SHEET-200", catalog[1].Id);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
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
    }
}
