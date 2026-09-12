using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QS3D.Core.BenchmarkParity;

namespace QS3D.QuantBim.Desktop;

internal sealed class SoftwareViewport : Panel, IQuantBimViewportRenderer
{
    private QuantBimViewportPresentation? _presentation;
    private double _yaw = -0.65d;
    private double _pitch = 0.5d;
    private double _zoom = 1d;
    private double _panX;
    private double _panY;

    public SoftwareViewport()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(28, 31, 36);
        Dock = DockStyle.Fill;
    }

    public void Present(QuantBimViewportPresentation presentation)
    {
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        Invalidate();
    }

    public void Navigate(IfcViewCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        switch (command.Kind)
        {
            case IfcViewCommandKind.FitAll:
            case IfcViewCommandKind.FocusSelection:
                _zoom = 1d;
                _panX = _panY = 0d;
                break;
            case IfcViewCommandKind.Orbit:
                _yaw += command.X;
                _pitch = Math.Max(-1.45d, Math.Min(1.45d, _pitch + command.Y));
                break;
            case IfcViewCommandKind.Pan:
                _panX += command.X;
                _panY += command.Y;
                break;
            case IfcViewCommandKind.Zoom:
                _zoom = Math.Max(0.05d, Math.Min(100d, _zoom * Math.Exp(command.X)));
                break;
        }
        Invalidate();
    }

    public void SetStandardView(QuantBimViewportStandardView view)
    {
        switch (view)
        {
            case QuantBimViewportStandardView.Top: _yaw = 0d; _pitch = Math.PI / 2d; break;
            case QuantBimViewportStandardView.Front: _yaw = 0d; _pitch = 0d; break;
            case QuantBimViewportStandardView.Right: _yaw = Math.PI / 2d; _pitch = 0d; break;
            default: _yaw = -0.65d; _pitch = 0.5d; break;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var presentation = _presentation;
        if (presentation == null || presentation.Scene.Nodes.Count == 0)
        {
            TextRenderer.DrawText(e.Graphics, "Open an IFC model", Font, ClientRectangle, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var visible = new HashSet<string>(presentation.VisibleGuids, StringComparer.OrdinalIgnoreCase);
        var points = presentation.Scene.Nodes.Where(n => visible.Contains(n.Guid)).SelectMany(n => n.Mesh.Vertices).ToList();
        if (points.Count == 0) return;

        var rotated = points.Select(Project3D).ToList();
        var minX = rotated.Min(p => p.X); var maxX = rotated.Max(p => p.X);
        var minY = rotated.Min(p => p.Y); var maxY = rotated.Max(p => p.Y);
        var width = Math.Max(1e-9, maxX - minX); var height = Math.Max(1e-9, maxY - minY);
        var scale = Math.Min(ClientSize.Width * 0.85d / width, ClientSize.Height * 0.85d / height) * _zoom;
        var cx = ClientSize.Width / 2d + _panX; var cy = ClientSize.Height / 2d + _panY;
        var centerX = (minX + maxX) / 2d; var centerY = (minY + maxY) / 2d;

        foreach (var node in presentation.Scene.Nodes.Where(n => visible.Contains(n.Guid)))
        {
            using var pen = new Pen(node.Selected ? Color.Gold : Color.SkyBlue, node.Selected ? 2.5f : 1f);
            var projected = node.Mesh.Vertices.Select(v => Project(v, centerX, centerY, scale, cx, cy)).ToArray();
            var idx = node.Mesh.TriangleIndices;
            for (var i = 0; i < idx.Count; i += 3)
            {
                e.Graphics.DrawLine(pen, projected[idx[i]], projected[idx[i + 1]]);
                e.Graphics.DrawLine(pen, projected[idx[i + 1]], projected[idx[i + 2]]);
                e.Graphics.DrawLine(pen, projected[idx[i + 2]], projected[idx[i]]);
            }
        }
    }

    private (double X, double Y, double Z) Project3D(IfcSceneVertex v)
    {
        var cy = Math.Cos(_yaw); var sy = Math.Sin(_yaw);
        var cp = Math.Cos(_pitch); var sp = Math.Sin(_pitch);
        var x1 = v.X * cy - v.Z * sy;
        var z1 = v.X * sy + v.Z * cy;
        var y2 = v.Y * cp - z1 * sp;
        var z2 = v.Y * sp + z1 * cp;
        return (x1, y2, z2);
    }

    private PointF Project(IfcSceneVertex v, double centerX, double centerY, double scale, double cx, double cy)
    {
        var p = Project3D(v);
        return new PointF((float)(cx + (p.X - centerX) * scale), (float)(cy - (p.Y - centerY) * scale));
    }
}

internal sealed class SemanticProxyGeometryResolver : IIfcGeometryResolver
{
    public IfcSceneMesh Resolve(string geometryReference)
    {
        if (string.IsNullOrWhiteSpace(geometryReference)) throw new ArgumentException("Geometry reference is required.", nameof(geometryReference));
        unchecked
        {
            var hash = 17;
            foreach (var ch in geometryReference) hash = hash * 31 + ch;
            var sx = 1d + Math.Abs(hash % 7) * 0.15d;
            var sy = 1d + Math.Abs((hash / 7) % 5) * 0.18d;
            var sz = 1d + Math.Abs((hash / 31) % 9) * 0.12d;
            var ox = ((hash & 0xff) - 128) * 0.03d;
            var oy = (((hash >> 8) & 0xff) - 128) * 0.03d;
            var oz = (((hash >> 16) & 0xff) - 128) * 0.03d;
            var v = new[]
            {
                new IfcSceneVertex(ox,oy,oz), new IfcSceneVertex(ox+sx,oy,oz), new IfcSceneVertex(ox+sx,oy+sy,oz), new IfcSceneVertex(ox,oy+sy,oz),
                new IfcSceneVertex(ox,oy,oz+sz), new IfcSceneVertex(ox+sx,oy,oz+sz), new IfcSceneVertex(ox+sx,oy+sy,oz+sz), new IfcSceneVertex(ox,oy+sy,oz+sz)
            };
            var t = new[] { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,5, 0,5,1, 1,5,6, 1,6,2, 2,6,7, 2,7,3, 3,7,4, 3,4,0 };
            return new IfcSceneMesh(v, t);
        }
    }
}
