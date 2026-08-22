using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverAutoRoomBoundaryCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalBoundaryHandlesResolveInWriterOrder();
            MalformedBoundaryHandlesFailClosed();
            EmptyBoundarySnapshotStillFallsBackToGeneratedOwnership();
            DirectSourcePrecedenceStillBypassesBoundaryFallback();
        }

        private static void CanonicalBoundaryHandlesResolveInWriterOrder()
        {
            var project = CreateProject(out var element);
            element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "AA;BB";

            var resolved = SourceHandleResolver.Resolve(project, new[] { element.Id });

            Equal(2, resolved.Count, "canonical boundary count");
            Equal("AA", resolved[0], "canonical boundary first handle");
            Equal("BB", resolved[1], "canonical boundary second handle");
        }

        private static void MalformedBoundaryHandlesFailClosed()
        {
            var malformed = new[]
            {
                "aa;BB",
                "BB;AA",
                "AA;;BB",
                "AA;aa",
                " AA;BB",
                "AA;BB "
            };

            foreach (var raw in malformed)
            {
                var project = CreateProject(out var element);
                element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = raw;
                ThrowsCanonicality(() => SourceHandleResolver.Resolve(project, new[] { element.Id }), raw);
            }
        }

        private static void EmptyBoundarySnapshotStillFallsBackToGeneratedOwnership()
        {
            var project = CreateProject(out var element);
            element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = string.Empty;
            element.Properties["GeneratedSolidHandle"] = "GEN1";

            var resolved = SourceHandleResolver.Resolve(project, new[] { element.Id });

            Equal(1, resolved.Count, "empty boundary generated fallback count");
            Equal("GEN1", resolved[0], "empty boundary generated fallback handle");
        }

        private static void DirectSourcePrecedenceStillBypassesBoundaryFallback()
        {
            var project = CreateProject(out var element);
            element.SourceHandles.Add("DIRECT1");
            element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "noncanonical";

            var resolved = SourceHandleResolver.Resolve(project, new[] { element.Id });

            Equal(1, resolved.Count, "direct precedence count");
            Equal("DIRECT1", resolved[0], "direct precedence handle");
        }

        private static ProjectState CreateProject(out ProjectElement element)
        {
            var project = new ProjectState("AUTOROOM-BOUNDARY-CANON", "Auto Room boundary canonicality");
            element = new ProjectElement("E1", ElementCategory.Room);
            project.Elements.Add(element);
            return project;
        }

        private static void ThrowsCanonicality(Action action, string raw)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("non-canonical " + AutoRoomLifecycle.BoundarySourceHandlesKey, StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected BoundarySourceHandles validation error for " + raw + ".", ex);
            }
            throw new InvalidOperationException("Expected non-canonical BoundarySourceHandles rejection for " + raw + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
