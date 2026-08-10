using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using QS3D.Core.Revisions;

namespace QS3D.BricsCAD.V25.UI
{
    public sealed class RevisionDisplayRow
    {
        public string ElementId { get; set; } = string.Empty;
        public string Change { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Before { get; set; } = string.Empty;
        public string After { get; set; } = string.Empty;
    }

    public partial class RevisionWindow : Window
    {
        private readonly Action<string>? _locate;

        public RevisionWindow(RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<RevisionDelta> deltas, Action<string>? locate = null)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            if (deltas == null) throw new ArgumentNullException(nameof(deltas));
            _locate = locate;
            InitializeComponent();
            Header.Text = before.Id + " → " + after.Id;
            var rows = Flatten(deltas);
            Grid.ItemsSource = rows;
            Totals.Text = deltas.Count + " element thay đổi • " + rows.Count + " field delta";
        }

        private static IReadOnlyList<RevisionDisplayRow> Flatten(IEnumerable<RevisionDelta> deltas)
        {
            var rows = new List<RevisionDisplayRow>();
            foreach (var delta in deltas)
            {
                if (delta.Fields.Count == 0)
                    rows.Add(new RevisionDisplayRow { ElementId = delta.ElementId, Change = delta.Change });
                else
                    foreach (var field in delta.Fields)
                        rows.Add(new RevisionDisplayRow { ElementId = delta.ElementId, Change = delta.Change, Field = field.Field, Before = field.Before, After = field.After });
            }
            return rows;
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();
        private void Locate()
        {
            if (_locate != null && Grid.SelectedItem is RevisionDisplayRow row) _locate(row.ElementId);
        }
    }
}
