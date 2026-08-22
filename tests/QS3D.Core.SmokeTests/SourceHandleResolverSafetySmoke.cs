using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverSafetySmoke
    {
        internal static void Run()
        {
            DeepDependencyChainDoesNotUseProcessStack();
            DependencyCycleTerminatesDeterministically();
            DuplicateElementIdsFailClosed();
            DirectAndDependencyHandleOrderIsStable();
        }

        private static void DeepDependencyChainDoesNotUseProcessStack()
        {
            const int depth = 8192;
            var project = new ProjectState("source-deep", "Source Deep");
            for (var index = 0; index < depth; index++)
            {
                var element = NewElement("E" + index);
                if (index + 1 < depth) element.DependsOn.Add("E" + (index + 1));
                else element.SourceHandles.Add("DEEP-END");
                project.Elements.Add(element);
            }

            var handles = SourceHandleResolver.Resolve(project, new[] { "E0" });
            if (handles.Count != 1 || !string.Equals(handles[0], "DEEP-END", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Deep dependency traversal did not resolve the terminal source handle.");
        }

        private static void DependencyCycleTerminatesDeterministically()
        {
            var project = new ProjectState("source-cycle", "Source Cycle");
            var a = NewElement("A");
            var b = NewElement("B");
            a.SourceHandles.Add("HA");
            a.DependsOn.Add("B");
            b.SourceHandles.Add("HB");
            b.DependsOn.Add("A");
            project.Elements.Add(a);
            project.Elements.Add(b);

            var handles = SourceHandleResolver.Resolve(project, new[] { "A" });
            if (handles.Count != 2 || handles[0] != "HA" || handles[1] != "HB")
                throw new Exception("Cyclic dependency traversal changed deterministic source-handle order.");
        }

        private static void DuplicateElementIdsFailClosed()
        {
            var project = new ProjectState("source-duplicate", "Source Duplicate");
            project.Elements.Add(NewElement("A"));
            project.Elements.Add(NewElement("a"));
            var threw = false;
            try { SourceHandleResolver.Resolve(project, new[] { "A" }); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Duplicate semantic element ids must fail source-handle resolution closed.");
        }

        private static void DirectAndDependencyHandleOrderIsStable()
        {
            var project = new ProjectState("source-order", "Source Order");
            var root = NewElement("ROOT");
            root.SourceHandles.Add("ROOT-H");
            root.DependsOn.Add("FIRST");
            root.DependsOn.Add("SECOND");
            var first = NewElement("FIRST");
            first.SourceHandles.Add("FIRST-H");
            var second = NewElement("SECOND");
            second.SourceHandles.Add("SECOND-H");
            project.Elements.Add(root);
            project.Elements.Add(first);
            project.Elements.Add(second);

            var handles = SourceHandleResolver.Resolve(project, new[] { "ROOT" });
            if (handles.Count != 3 || handles[0] != "ROOT-H" || handles[1] != "FIRST-H" || handles[2] != "SECOND-H")
                throw new Exception("Iterative source-handle traversal must preserve dependency encounter order.");
        }

        private static ProjectElement NewElement(string id) =>
            new ProjectElement(id, ElementCategory.Room, string.Empty, string.Empty, string.Empty);
    }

    internal static class SourceHandleResolverSafetyRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SourceHandleResolverSafetySmoke.Run();
    }
}
