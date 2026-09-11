using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarModeNullSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-null-mode", "Null-safe rebar mode health");
            SeedCorruptNullElement(project);
            try
            {
                new GeneratedRebarModeHealthService().Inspect(project);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("GeneratedRebarModeNullSafetySmoke: malformed null semantic entries must fail visibly.");
        }

        private static void SeedCorruptNullElement(ProjectState project)
        {
            var itemsField = project.Elements.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed corrupt generated-rebar-mode safety project state.");
            var items = itemsField.GetValue(project.Elements) as List<ProjectElement>
                ?? throw new InvalidOperationException("Unexpected ProjectState.Elements backing collection.");
            items.Add(null!);
        }
    }
}
