using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using QS3D.Core.Domain;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public sealed class FamilyUsageTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || !(values[0] is ProjectFamily family)) return "—";
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || !ProjectContextCoordinator.TryGetReadOnly(document, out var project)) return "—";

            try
            {
                var ownedFamily = project.FindFamily(family.Id);
                if (ownedFamily == null || !ReferenceEquals(ownedFamily, family)) return "—";
                var count = project.Elements.Count(element =>
                    element != null && string.Equals(element.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase));
                return count.ToString("N0", culture ?? CultureInfo.CurrentCulture) + " cấu kiện";
            }
            catch (InvalidOperationException)
            {
                return "—";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException("Family usage badge is read-only.");
    }
}
