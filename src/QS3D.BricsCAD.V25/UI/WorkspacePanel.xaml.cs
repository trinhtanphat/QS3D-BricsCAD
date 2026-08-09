using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Model;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel : UserControl
    {
        public WorkspacePanel() => InitializeComponent();
        public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots) { InspectionList.ItemsSource = snapshots; SelectionCount.Text = snapshots.Count.ToString(); }
        private void OnAddClick(object sender, RoutedEventArgs e) => MessageBox.Show("Family editor foundation is ready; CAD creation transaction is gated behind the first V25 integration build.", "QS3D");
        private void OnDeleteClick(object sender, RoutedEventArgs e) => MessageBox.Show("Delete will use a BricsCAD transaction + undo record after the first runtime gate.", "QS3D");
        private void OnView3DClick(object sender, RoutedEventArgs e) => MessageBox.Show("3D remains in the native BricsCAD viewport; QS3D manages semantic elements and quantities.", "QS3D");
        private void OnQuantityClick(object sender, RoutedEventArgs e) => MessageBox.Show("Run QS3DBQ to open the quantity summary for the current selection.", "QS3D");
    }
}
