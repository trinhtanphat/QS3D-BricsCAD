using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnershipSafetySmoke
    {
        internal static void Run()
        {
            ValidOwnershipStillResolvesDeterministically();
            NullElementFailsClosed();
            DuplicateElementIdsFailClosed();
        }

        private static void ValidOwnershipStillResolvesDeterministically()
        {
            var project = new ProjectState("P", "Project");
            var first = new ProjectElement("E1", ElementCategory.Beam);
            first.Properties["GeneratedSolidHandle"] = "AA11";
            var second = new ProjectElement("E2", ElementCategory.Beam);
            second.Properties["GeneratedRebarHandles"] = "CC33;BB22";
            project.Elements.Add(first);
            project.Elements.Add(second);

            Equal("AA11|BB22|CC33", string.Join("|", GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)));
            True(GeneratedHandleOwnershipPolicy.TryFindOwner(project, "bb22", out var owner, out var propertyKey));
            True(ReferenceEquals(second, owner));
            Equal("GeneratedRebarHandles", propertyKey);
        }

        private static void NullElementFailsClosed()
        {
            var project = new ProjectState("P", "Project");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA11";
            project.Elements.Add(element);
            SeedCorruptNullElement(project);

            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project));
            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "UNOWNED", out _, out _));
        }

        private static void DuplicateElementIdsFailClosed()
        {
            var project = new ProjectState("P", "Project");
            var first = new ProjectElement("E1", ElementCategory.Beam);
            first.Properties["GeneratedSolidHandle"] = "AA11";
            var duplicate = new ProjectElement("E1", ElementCategory.Beam);
            duplicate.Properties["GeneratedRebarHandles"] = "BB22";
            project.Elements.Add(first);
            project.Elements.Add(duplicate);

            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project));
            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "UNOWNED", out _, out _));
        }

        private static void SeedCorruptNullElement(ProjectState project)
        {
            var itemsField = project.Elements.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed corrupt generated-handle ownership project state.");
            var items = itemsField.GetValue(project.Elements) as List<ProjectElement>
                ?? throw new InvalidOperationException("Unexpected ProjectState.Elements backing collection.");
            items.Add(null!);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class GeneratedHandleOwnershipSafetySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GeneratedHandleOwnershipSafetySmoke.Run();
    }
}
