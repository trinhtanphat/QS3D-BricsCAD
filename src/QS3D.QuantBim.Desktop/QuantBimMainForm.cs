using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QS3D.Core.BenchmarkParity;

namespace QS3D.QuantBim.Desktop;

internal sealed class QuantBimMainForm : Form
{
    private readonly SoftwareViewport _viewport = new();
    private readonly QuantBimStandaloneWorkbench _workbench;
    private readonly QuantBimStandaloneViewportHost _host;
    private readonly DataGridView _elements = new() { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true, MultiSelect = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly DataGridView _properties = new() { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true };
    private readonly DataGridView _boq = new() { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true };
    private readonly ToolStripTextBox _entity = new() { ToolTipText = "IFC entity" };
    private readonly ToolStripTextBox _storey = new() { ToolTipText = "Storey" };
    private readonly ToolStripTextBox _type = new() { ToolTipText = "Type" };
    private readonly ToolStripTextBox _classification = new() { ToolTipText = "Classification" };
    private IfcStandaloneDocument? _document;

    public QuantBimMainForm()
    {
        Text = "QS3D QuantBIM Standalone IFC-QTO";
        Width = 1440;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        _workbench = new QuantBimStandaloneWorkbench(new IfcStepStandaloneSource());
        _host = new QuantBimStandaloneViewportHost(_workbench, new QuantBimStandaloneSceneBuilder(new SemanticProxyGeometryResolver()), _viewport);

        var toolbar = BuildToolbar();
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 980 };
        split.Panel1.Controls.Add(_viewport);
        split.Panel2.Controls.Add(BuildInspectorTabs());
        Controls.Add(split);
        Controls.Add(toolbar);

        _elements.SelectionChanged += (_, _) => SyncSelection();
    }

    private ToolStrip BuildToolbar()
    {
        var bar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        AddButton(bar, "Open IFC", OpenIfc);
        AddButton(bar, "Fit", (_, _) => _host.FitAll());
        AddButton(bar, "Iso", (_, _) => _host.SetStandardView(QuantBimViewportStandardView.Isometric));
        AddButton(bar, "Top", (_, _) => _host.SetStandardView(QuantBimViewportStandardView.Top));
        AddButton(bar, "Front", (_, _) => _host.SetStandardView(QuantBimViewportStandardView.Front));
        AddButton(bar, "Right", (_, _) => _host.SetStandardView(QuantBimViewportStandardView.Right));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(new ToolStripLabel("Entity")); bar.Items.Add(_entity);
        bar.Items.Add(new ToolStripLabel("Storey")); bar.Items.Add(_storey);
        bar.Items.Add(new ToolStripLabel("Type")); bar.Items.Add(_type);
        bar.Items.Add(new ToolStripLabel("Class")); bar.Items.Add(_classification);
        AddButton(bar, "Apply filter", (_, _) => ApplyFilter());
        AddButton(bar, "Clear", (_, _) => { _entity.Text = _storey.Text = _type.Text = _classification.Text = string.Empty; ApplyFilter(); });
        bar.Items.Add(new ToolStripSeparator());
        AddButton(bar, "QTO selection", (_, _) => RefreshBoq());
        AddButton(bar, "Export BOQ CSV", ExportCsv);
        return bar;
    }

    private Control BuildInspectorTabs()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabPage("Elements") { Controls = { _elements } });
        tabs.TabPages.Add(new TabPage("Properties") { Controls = { _properties } });
        tabs.TabPages.Add(new TabPage("BOQ") { Controls = { _boq } });
        return tabs;
    }

    private static void AddButton(ToolStrip bar, string text, EventHandler handler)
    {
        var button = new ToolStripButton(text);
        button.Click += handler;
        bar.Items.Add(button);
    }

    private void OpenIfc(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "IFC STEP (*.ifc)|*.ifc|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _host.Open(dialog.FileName);
            _document = _host.CurrentDocument;
            BindElements(_document!.Elements);
            Text = "QS3D QuantBIM — " + Path.GetFileName(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "IFC open failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyFilter()
    {
        if (_document == null) return;
        var filter = new IfcWorkbenchFilter(_entity.Text, _storey.Text, _type.Text, _classification.Text);
        var filtered = _workbench.Filter(_document, filter);
        _host.ApplyFilter(filter);
        BindElements(filtered);
        _properties.DataSource = null;
        _boq.DataSource = null;
    }

    private void BindElements(IEnumerable<IfcStandaloneElement> rows)
    {
        _elements.DataSource = rows.Select(x => new ElementRow(x.Guid, x.Entity, x.Name, x.Storey, x.Type, x.Classification)).ToList();
    }

    private void SyncSelection()
    {
        if (_document == null || _elements.SelectedRows.Count == 0) return;
        var guids = _elements.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as ElementRow)
            .Where(r => r != null)
            .Select(r => r!.Guid)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (guids.Count == 0) return;
        try
        {
            _host.Select("Desktop", guids);
            var inspection = _host.Inspect(guids[0]);
            _properties.DataSource = inspection.Properties.Select(x => new PropertyRow(x.Name, x.Value)).ToList();
        }
        catch (InvalidOperationException)
        {
            // Selection can transiently lag a filter rebinding; the next SelectionChanged settles it.
        }
    }

    private void RefreshBoq()
    {
        if (_document == null) return;
        var rows = _host.BuildSelectionBoq();
        _boq.DataSource = rows.Select(x => new BoqRow(x.Classification, x.Unit, x.Quantity, x.SourceCount)).ToList();
    }

    private void ExportCsv(object? sender, EventArgs e)
    {
        if (_document == null) return;
        var csv = _host.ExportSelectionBoqCsv();
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "quantbim-boq.csv", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, csv, new System.Text.UTF8Encoding(false));
    }

    private sealed record ElementRow(string Guid, string Entity, string Name, string Storey, string Type, string Classification);
    private sealed record PropertyRow(string Name, string Value);
    private sealed record BoqRow(string Classification, string Unit, double Quantity, int SourceCount);
}
