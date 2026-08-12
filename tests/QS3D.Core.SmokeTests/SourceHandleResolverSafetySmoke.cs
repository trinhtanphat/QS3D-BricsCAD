using System;
using System.Collections.Generic;
using System.Linq;
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
            BlankSourceHandleFailsClosed();
            NonCanonicalSourceHandleFailsClosed();
            ExactDuplicateSourceHandlesFailClosed();
            CaseAliasDuplicateSourceHandlesFailClosed();
            NumericAliasDuplicateSourceHandlesFailClosed();
            UniqueSourceHandlesRemainResolvable();
            NumericAliasesAcrossElementsResolveOnce();
            BoundaryAndDirectNumericAliasesResolveOnce();
            DirectAndDependencyHandleOrderIsStable();
            SourceReferenceWinsOverGeneratedFallback();
            BoundaryReferenceWinsOverGeneratedFallback();
            CanonicalGeneratedOwnersResolveWhenSourceIsMissing();
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

        private static void BlankSourceHandleFailsClosed()
        {
            var project = new ProjectState("source-handle-blank", "Source Handle Blank");
            var element = NewElement("E");
            element.SourceHandles.Add(" ");
            project.Elements.Add(element);

            AssertInvalidDirectSourceHandleFailsClosed(project, element.Id, "empty SourceHandles entry at index 0");
        }

        private static void NonCanonicalSourceHandleFailsClosed()
        {
            var project = new ProjectState("source-handle-noncanonical", "Source Handle Noncanonical");
            var element = NewElement("E");
            element.SourceHandles.Add(" ABCD");
            project.Elements.Add(element);

            AssertInvalidDirectSourceHandleFailsClosed(project, element.Id, "non-canonical SourceHandles entry at index 0");
        }

        private static void AssertInvalidDirectSourceHandleFailsClosed(ProjectState project, string elementId, string expectedMessage)
        {
            try
            {
                SourceHandleResolver.Resolve(project, new[] { elementId });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                    throw new Exception("Malformed SourceHandles failure did not preserve the expected diagnostic: " + expectedMessage);
                return;
            }

            throw new Exception("Malformed direct SourceHandles input must fail source-handle resolution closed.");
        }

        private static void ExactDuplicateSourceHandlesFailClosed()
        {
            var project = new ProjectState("source-handle-duplicate", "Source Handle Duplicate");
            var element = NewElement("E");
            element.SourceHandles.Add("ABCD");
            element.SourceHandles.Add("ABCD");
            project.Elements.Add(element);

            AssertDuplicateSourceHandlesFailClosed(project, element.Id);
        }

        private static void CaseAliasDuplicateSourceHandlesFailClosed()
        {
            var project = new ProjectState("source-handle-case-alias", "Source Handle Case Alias");
            var element = NewElement("E");
            element.SourceHandles.Add("ABCD");
            element.SourceHandles.Add("abcd");
            project.Elements.Add(element);

            AssertDuplicateSourceHandlesFailClosed(project, element.Id);
        }

        private static void NumericAliasDuplicateSourceHandlesFailClosed()
        {
            var project = new ProjectState("source-handle-numeric-alias", "Source Handle Numeric Alias");
            var element = NewElement("E");
            element.SourceHandles.Add("A");
            element.SourceHandles.Add("000A");
            project.Elements.Add(element);

            AssertDuplicateSourceHandlesFailClosed(project, element.Id);
        }

        private static void UniqueSourceHandlesRemainResolvable()
        {
            var project = new ProjectState("source-handle-unique", "Source Handle Unique");
            var element = NewElement("E");
            element.SourceHandles.Add("ABCD");
            element.SourceHandles.Add("EF01");
            project.Elements.Add(element);

            var handles = SourceHandleResolver.Resolve(project, new[] { element.Id });
            if (handles.Count != 2 || handles[0] != "ABCD" || handles[1] != "EF01")
                throw new Exception("Unique SourceHandles must preserve direct-handle resolution order.");
        }

        private static void NumericAliasesAcrossElementsResolveOnce()
        {
            var project = new ProjectState("source-handle-numeric-cross-element", "Source Handle Numeric Cross Element");
            var root = NewElement("ROOT");
            root.SourceHandles.Add("A");
            root.DependsOn.Add("CHILD");
            var child = NewElement("CHILD");
            child.SourceHandles.Add("0A");
            project.Elements.Add(root);
            project.Elements.Add(child);

            var handles = SourceHandleResolver.Resolve(project, new[] { root.Id });
            if (handles.Count != 1 || handles[0] != "A")
                throw new Exception("Numeric SourceHandles aliases across elements must resolve one CAD object and preserve the first raw spelling.");
        }

        private static void BoundaryAndDirectNumericAliasesResolveOnce()
        {
            var project = new ProjectState("source-handle-boundary-numeric", "Source Handle Boundary Numeric Alias");
            var root = NewElement("ROOT");
            root.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "A";
            root.DependsOn.Add("CHILD");
            var child = NewElement("CHILD");
            child.SourceHandles.Add("000A");
            project.Elements.Add(root);
            project.Elements.Add(child);

            var handles = SourceHandleResolver.Resolve(project, new[] { root.Id });
            if (handles.Count != 1 || handles[0] != "A")
                throw new Exception("Boundary/direct numeric handle aliases must resolve one CAD object and preserve the first raw spelling.");
        }

        private static void AssertDuplicateSourceHandlesFailClosed(ProjectState project, string elementId)
        {
            try
            {
                SourceHandleResolver.Resolve(project, new[] { elementId });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("duplicate SourceHandles entries at indices 0 and 1", StringComparison.Ordinal) < 0)
                    throw new Exception("Duplicate SourceHandles failure did not preserve first/current index diagnostics.");
                return;
            }

            throw new Exception("Duplicate SourceHandles within one semantic element must fail source-handle resolution closed.");
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

        private static void SourceReferenceWinsOverGeneratedFallback()
        {
            var project = new ProjectState("source-priority", "Source Priority");
            var element = NewElement("E");
            element.SourceHandles.Add("SOURCE-H");
            element.Properties["GeneratedTieRebarHandles"] = "TIE-A;TIE-B";
            project.Elements.Add(element);

            var handles = SourceHandleResolver.Resolve(project, new[] { element.Id });
            if (handles.Count != 1 || handles[0] != "SOURCE-H")
                throw new Exception("Source handles must remain authoritative over generated-owner locate fallback.");
        }

        private static void BoundaryReferenceWinsOverGeneratedFallback()
        {
            var project = new ProjectState("boundary-priority", "Boundary Priority");
            var element = NewElement("E");
            element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "BOUND-A;BOUND-B";
            element.Properties["GeneratedCurtainFrameHandles"] = "FRAME-A";
            project.Elements.Add(element);

            var handles = SourceHandleResolver.Resolve(project, new[] { element.Id });
            if (handles.Count != 2 || handles[0] != "BOUND-A" || handles[1] != "BOUND-B")
                throw new Exception("Boundary source handles must remain authoritative over generated-owner locate fallback.");
        }

        private static void CanonicalGeneratedOwnersResolveWhenSourceIsMissing()
        {
            var slots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GeneratedSolidHandle"] = "A1",
                ["GeneratedRebarHandles"] = "A2;A3",
                ["GeneratedShapeRebarHandles"] = "A4",
                ["GeneratedTieRebarHandles"] = "A5",
                ["GeneratedBeamStirrupHandles"] = "A6",
                ["GeneratedSlabMeshHandles"] = "A7",
                ["GeneratedWallMeshHandles"] = "A8",
                ["GeneratedFoundationMeshHandles"] = "A9",
                ["GeneratedCurtainFrameHandles"] = "AA",
                ["PhysicalOpeningCutSolidHandle"] = "AB"
            };

            foreach (var pair in slots)
            {
                var project = new ProjectState("generated-" + pair.Key, "Generated " + pair.Key);
                var element = NewElement("E");
                element.Properties[pair.Key] = pair.Value;
                project.Elements.Add(element);

                var expected = pair.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                var handles = SourceHandleResolver.Resolve(project, new[] { element.Id });
                if (handles.Count != expected.Length || expected.Any(x => !handles.Contains(x, StringComparer.OrdinalIgnoreCase)))
                    throw new Exception("Canonical generated-owner locate fallback did not resolve slot " + pair.Key + ".");
            }
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
