using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Reporting
{
    // C02 generation-fence remediation in progress.
    public static class ProjectQuantityReportBuilder
    {
        internal const int MaxSelectionElementIds = 10000;

        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project) => Build(project, null, false);

        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return Build(project, elementIds, false);
        }

        public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project) => Build(project, null, true);

        public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return Build(project, elementIds, true);
        }

        private static IReadOnlyList<QuantityReportRow> Build(ProjectState project, IEnumerable<string>? elementIds, bool detail)
        {
            throw new NotImplementedException();
        }
    }
}
