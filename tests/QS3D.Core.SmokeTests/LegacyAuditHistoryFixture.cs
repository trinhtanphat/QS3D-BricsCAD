using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyAuditHistoryFixture
    {
        internal static void AppendRaw(ProjectState project, AuditEvent? item)
        {
            var ownerList = project.AuditEvents;
            FieldInfo? storageField = null;
            var count = 0;
            for (var type = ownerList.GetType(); type != null; type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (!typeof(IList<AuditEvent>).IsAssignableFrom(field.FieldType)) continue;
                    storageField = field;
                    count++;
                }
            }

            if (count != 1 || storageField?.GetValue(ownerList) is not IList<AuditEvent> storage)
                throw new InvalidOperationException("Could not resolve unique AuditEvents backing storage for legacy fixture injection.");
            storage.Add(item!);
        }
    }
}
