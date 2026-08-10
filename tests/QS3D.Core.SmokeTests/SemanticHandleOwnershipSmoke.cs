using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            UnrelatedAmbiguityDoesNotBlockCleanSelection();
            SelectedAmbiguityIsRejected();
            GeneratedMultiHandleResolvesOwner();
            FoundationMeshGeneratedHandleResolvesOwner();
            FutureGeneratedOwnerSlotResolvesOwner();
            ReferenceHandleIsNotGeneratedOwner();
            OwnerCollectionDedupesAndIncludesOpeningCut();
            AmbiguousGeneratedOwnerIsRejected();
        }

        private static void UnrelatedAmbiguityDoesNotBlockCleanSelection()
        {
            var project = Project();
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "AA" });
            if (resolved.Count != 1 || resolved[0].Id != "A")
                throw new Exception("Unrelated ownership ambiguity blocked or changed a clean semantic selection.");
        }

        private static void SelectedAmbiguityIsRejected()
        {
            var project = Project();
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "BB" }));
        }

        private static void GeneratedMultiHandleResolvesOwner()
        {
            var project = Project();
            var curtain = new ProjectElement("CW", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            curtain.Properties["GeneratedCurtainFrameHandles"] = "C1; C2 ; C2";
            project.Elements.Add(curtain);

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "c2" });
            if (resolved.Count != 1 || resolved[0].Id != "CW")
                throw new Exception("Generated multi-handle selection did not resolve its semantic owner.");
        }

        private static void FoundationMeshGeneratedHandleResolvesOwner()
        {
            var project = Project();
            var foundation = new ProjectElement("FND", ElementCategory.Foundation, string.Empty, string.Empty, string.Empty);
            foundation.Properties["GeneratedFoundationMeshHandles"] = "F1; F2 ; F2";
            project.Elements.Add(foundation);

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "f2" });
            if (resolved.Count != 1 || resolved[0].Id != "FND")
                throw new Exception("Generated foundation-mesh selection did not resolve its semantic owner.");
        }

        private static void FutureGeneratedOwnerSlotResolvesOwner()
        {
            var project = Project();
            var future = new ProjectElement("FUTURE", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            future.Properties["GeneratedFuturePanelHandles"] = "N1;N2";
            project.Elements.Add(future);

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "n2" });
            if (resolved.Count != 1 || resolved[0].Id != "FUTURE")
                throw new Exception("Future Generated*Handles owner slot was not resolved dynamically.");
        }

        private static void ReferenceHandleIsNotGeneratedOwner()
        {
            var project = Project();
            var reference = new ProjectElement("REF", ElementCategory.Door, string.Empty, string.Empty, string.Empty);
            reference.Properties["HostHandle"] = "HOST-1";
            reference.Properties["BoundarySourceHandles"] = "BOUNDARY-1";
            project.Elements.Add(reference);

            if (GeneratedHandleOwnershipPolicy.TryFindOwner(project, "HOST-1", out _, out _))
                throw new Exception("Reference/provenance HostHandle was incorrectly treated as generated ownership.");
            if (SemanticHandleOwnershipResolver.Resolve(project, new[] { "HOST-1" }).Count != 0)
                throw new Exception("Reference/provenance handle incorrectly resolved as generated owner.");
        }

        private static void OwnerCollectionDedupesAndIncludesOpeningCut()
        {
            var project = Project();
            var element = new ProjectElement("OWN", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "S1";
            element.Properties["GeneratedFutureHandles"] = "S1;S2;s2";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "CUT1";
            project.Elements.Add(element);

            var handles = GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project);
            if (handles.Count(x => string.Equals(x, "S1", StringComparison.OrdinalIgnoreCase)) != 1 ||
                handles.Count(x => string.Equals(x, "S2", StringComparison.OrdinalIgnoreCase)) != 1 ||
                handles.Count(x => string.Equals(x, "CUT1", StringComparison.OrdinalIgnoreCase)) != 1)
                throw new Exception("Generated owner collection did not dedupe or include opening-cut ownership.");
        }

        private static void AmbiguousGeneratedOwnerIsRejected()
        {
            var project = Project();
            var left = new ProjectElement("LEFT", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            left.Properties["GeneratedSlabMeshHandles"] = "DUP";
            var right = new ProjectElement("RIGHT", ElementCategory.Foundation, string.Empty, string.Empty, string.Empty);
            right.Properties["GeneratedFoundationMeshHandles"] = "dup";
            project.Elements.Add(left);
            project.Elements.Add(right);
            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "DUP", out _, out _));
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("selection-safety", "Selection Safety");
            var a = new ProjectElement("A", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            a.SourceHandles.Add("AA");
            var b = new ProjectElement("B", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            b.SourceHandles.Add("BB");
            var c = new ProjectElement("C", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            c.Properties["GeneratedSolidHandle"] = "BB";
            project.Elements.Add(a);
            project.Elements.Add(b);
            project.Elements.Add(c);
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
