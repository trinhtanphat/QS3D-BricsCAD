using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class CorruptProjectStateSeed
    {
        internal static void AddNullElement(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            if (project.Elements is List<ProjectElement> list)
            {
                list.Add(null!);
                return;
            }

            var itemsField = project.Elements.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to access the ProjectState.Elements backing list for a corrupt-state fixture.");
            if (itemsField.GetValue(project.Elements) is not List<ProjectElement> items)
                throw new InvalidOperationException("Unexpected ProjectState.Elements backing collection for a corrupt-state fixture.");
            items.Add(null!);
        }
    }
}
