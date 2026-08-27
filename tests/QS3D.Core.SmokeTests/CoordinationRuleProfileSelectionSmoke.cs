using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleProfileSelectionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactBindingPreservesRequestedRevision();
            MissingRevisionFailsClosed();
            DuplicateRevisionFailsClosed();
            InvalidBindingFailsClosed();
            CatalogResolutionRejectsWildcardActualCategory();
        }

        private static void ExactBindingPreservesRequestedRevision()
        {
            var v1 = new CoordinationRuleProfile(
                "PROJECT-MEP",
                1,
                new[]
                {
                    new CoordinationRule("PIPE-BEAM", 1, "Pipe", "Beam", CoordinationRuleKind.Clearance, "Warning", 0.05d)
                });
            var v2 = new CoordinationRuleProfile(
                "PROJECT-MEP",
                2,
                new[]
                {
                    new CoordinationRule("PIPE-BEAM", 2, "Pipe", "Beam", CoordinationRuleKind.Clearance, "Error", 0.10d)
                });
            var catalog = new CoordinationRuleProfileCatalog(new[] { v1, v2 });

            var bindingV1 = catalog.Bind("project-mep", 1);
            var bindingV2 = catalog.Bind("PROJECT-MEP", 2);
            var resolvedV1 = catalog.Resolve(bindingV1, "Pipe", "Beam") ??
                             throw new InvalidOperationException("Expected v1 profile resolution.");
            var resolvedV2 = catalog.Resolve(bindingV2, "Pipe", "Beam") ??
                             throw new InvalidOperationException("Expected v2 profile resolution.");

            Equal(1, resolvedV1.ProfileVersion, "v1 binding drifted to another profile revision");
            Equal(1, resolvedV1.RuleVersion, "v1 binding did not retain v1 rule semantics");
            Equal(0.05d, resolvedV1.Clearance, "v1 binding did not retain v1 clearance");
            Equal(2, resolvedV2.ProfileVersion, "explicit v2 binding did not select v2");
            Equal(2, resolvedV2.RuleVersion, "explicit v2 binding did not use v2 rule semantics");
            Equal(0.10d, resolvedV2.Clearance, "explicit v2 binding did not use v2 clearance");
            Equal("PROJECT-MEP", resolvedV1.ProfileId, "profile provenance changed during exact binding");
        }

        private static void MissingRevisionFailsClosed()
        {
            var catalog = new CoordinationRuleProfileCatalog(
                new[]
                {
                    new CoordinationRuleProfile(
                        "PROJECT-MEP",
                        1,
                        new[]
                        {
                            new CoordinationRule("PIPE-BEAM", 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0d)
                        })
                });

            Throws<InvalidOperationException>(() => catalog.Bind("PROJECT-MEP", 2));
            Throws<InvalidOperationException>(() =>
                catalog.Resolve(new CoordinationRuleProfileBinding("MISSING", 1), "Pipe", "Beam"));
        }

        private static void DuplicateRevisionFailsClosed()
        {
            var first = new CoordinationRuleProfile(
                "PROJECT-MEP",
                4,
                new[]
                {
                    new CoordinationRule("PIPE-BEAM", 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0d)
                });
            var duplicate = new CoordinationRuleProfile(
                "project-mep",
                4,
                new[]
                {
                    new CoordinationRule("DUCT-WALL", 1, "Duct", "Wall", CoordinationRuleKind.HardClash, "Error", 0d)
                });

            Throws<ArgumentException>(() => new CoordinationRuleProfileCatalog(new[] { first, duplicate }));
        }

        private static void InvalidBindingFailsClosed()
        {
            Throws<ArgumentException>(() => new CoordinationRuleProfileBinding(" ", 1));
            Throws<ArgumentException>(() => new CoordinationRuleProfileBinding(" PROJECT-MEP", 1));
            Throws<ArgumentException>(() => new CoordinationRuleProfileBinding("PROJECT-MEP ", 1));
            Throws<ArgumentException>(() => new CoordinationRuleProfileBinding("PRO\tJECT-MEP", 1));
            Throws<ArgumentOutOfRangeException>(() => new CoordinationRuleProfileBinding("PROJECT-MEP", 0));
            var catalog = new CoordinationRuleProfileCatalog(new CoordinationRuleProfile[0]);
            Throws<ArgumentNullException>(() => catalog.Resolve(null!, "Pipe", "Beam"));
        }

        private static void CatalogResolutionRejectsWildcardActualCategory()
        {
            var profile = new CoordinationRuleProfile(
                "PROJECT-MEP",
                5,
                new[]
                {
                    new CoordinationRule("FALLBACK", 1, "*", "*", CoordinationRuleKind.Clearance, "Info", 0.05d)
                });
            var catalog = new CoordinationRuleProfileCatalog(new[] { profile });
            var binding = catalog.Bind("PROJECT-MEP", 5);

            Throws<ArgumentException>(() => catalog.Resolve(binding, "*", "Beam"));
            Throws<ArgumentException>(() => catalog.Resolve(binding, "Pipe", "*"));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException(
                "CoordinationRuleProfileSelectionSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationRuleProfileSelectionSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationRuleProfileSelectionSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationRuleProfileSelectionSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }
    }
}
