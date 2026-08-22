using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewCatalogReadonlySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("VIEW-CATALOG-READONLY", "View catalog readonly");
            var catalog = SemanticViewPlanner.BuildCatalog(
                project,
                new[]
                {
                    new SemanticViewDefinition("VIEW-Z", "Zulu"),
                    new SemanticViewDefinition("VIEW-A", "Alpha")
                });

            Equal(2, catalog.Count);
            Equal("Alpha", catalog[0].Name);
            Equal("Zulu", catalog[1].Name);

            if (!(catalog is IList<SemanticViewPlan> mutable))
                throw new InvalidOperationException("Semantic view catalog must expose the standard read-only IList contract.");

            var first = catalog[0];
            var second = catalog[1];
            Throws<NotSupportedException>(() => mutable[0] = second);
            Throws<NotSupportedException>(() => mutable.Add(first));
            Throws<NotSupportedException>(() => mutable.Remove(first));

            Equal(2, catalog.Count);
            Equal("VIEW-A", catalog[0].Id);
            Equal("VIEW-Z", catalog[1].Id);
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
