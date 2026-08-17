using System;
using System.Windows;
using System.Windows.Controls;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class PropertiesPanel : UserControl
    {
        private WorkspacePanel? _workspace;

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        public void Attach(WorkspacePanel workspace)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            if (ReferenceEquals(_workspace, workspace))
            {
                DataContext = workspace.DataContext;
                return;
            }

            Detach();
            _workspace = workspace;
            DataContext = workspace.DataContext;
            workspace.DataContextChanged += OnWorkspaceDataContextChanged;
        }

        public void Detach()
        {
            var workspace = _workspace;
            _workspace = null;
            if (workspace != null)
                workspace.DataContextChanged -= OnWorkspaceDataContextChanged;
            DataContext = null;
        }

        private void OnWorkspaceDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, _workspace)) return;
            DataContext = e.NewValue;
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PropertyRowViewModel row)
                row.ResetValue();
        }
    }
}
