using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryScheduleRemainsReadOnly();
        }

        private static void OrdinaryScheduleRemainsReadOnly()
        {
            var project = new ProjectState("P-CURTAIN-READONLY", "Curtain readonly smoke");
            project.Elements.Add(new ProjectElement("CW-1", ElementCategory.GlassWall));

            var rows = CurtainWallScheduleBuilder.Build(project);
            if (rows.Count != 1 || rows[0].WallCount != 1 || rows[0].ElementIds.Count != 1 ||
                !string.Equals(rows[0].ElementIds[0], "CW-1", StringComparison.Ordinal))
                throw new InvalidOperationException("Curtain wall schedule row/count semantics changed while hardening the result boundary.");

            if (!(rows is ICollection<CurtainWallScheduleRow> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Curtain wall schedule result must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(new CurtainWallScheduleRow());
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Curtain wall schedule result accepted structural mutation through ICollection<T>.");
        }
    }
}
