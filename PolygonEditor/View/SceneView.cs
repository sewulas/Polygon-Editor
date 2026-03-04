using PolygonEditor.Model;
using PolygonEditor.Model.Constraints;
using PolygonEditor.Model.Continuities;
using PolygonEditor.Model.Utility;
using PolygonEditor.View.DrawingStrategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.View
{
    public class SceneView : UserControl
    {
        private Scene _scene;
        private IEdgeDrawer _drawer = new LibraryDrawer();
        private bool _isDrawVertexIndicesOn = true;
        private const float vertexRadius = 6f;
        public Scene Scene
        {
            get => _scene;
            set { _scene = value; Invalidate(); }
        }

        public SceneView()
        {
            DoubleBuffered = true;
            ResizeRedraw = true; 
            BackColor = Color.White;
        }

        public void SetDrawingStrategy(IEdgeDrawer drawingStartegy)
        {
            _drawer = drawingStartegy;
        }

        public void SetDrawingVertexIndices(bool b)
        {
            _isDrawVertexIndicesOn = b;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_scene == null)
                return;

            foreach (var polygon in _scene.Polygons)
            {
                DrawPolygon(e.Graphics, polygon);
            }
        }

        private void DrawPolygon(Graphics g, Polygon polygon)
        {
            if (polygon.Vertices.Count < 2)
                return;

            DrawPolygonsEdges(g, polygon);

            // Rysowanie wierzchołków
            foreach (var v in polygon.Vertices)
            {
                var center = v.Position;
                _drawer.DrawVertex(g, new Pen(Color.Black, 1), center, vertexRadius);
                DrawContinuityClasses(g, polygon, v);
            }
            if (_isDrawVertexIndicesOn)
                DrawVertexIndex(g, polygon);
        }

        private void DrawPolygonsEdges(Graphics g, Polygon polygon)
        {
            using var edgePen = new Pen(Color.Black, 1);

            // Rysowanie krawędzi
            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                if (polygon.Edges[i].Type == SegmentType.BezierCubic)
                {
                    var edge = polygon.Edges[i];
                    var p0 = polygon.Vertices[edge.Start].Position;
                    var p1 = edge.Cp1;
                    var p2 = edge.Cp2;
                    var p3 = polygon.Vertices[edge.End].Position;

                    using var pen = new Pen(Color.DarkOrange, 1f);
                    _drawer.DrawBezier(g, p0, p1, p2, p3, pen);

                    using var dashPen = new Pen(Color.Gray, 1) { DashPattern = new float[] { 3, 3 } };
                    PointF[] points = { p0, p1, p2, p3 };
                    for (int j = 0; j < 4; j++)
                    {
                        _drawer.DrawDashedLine(g, points[j], points[(j + 1) % 4], dashPen);
                    }

                    DrawControlPoint(g, p1);
                    DrawControlPoint(g, p2);
                    continue;
                }
                else if (polygon.Edges[i].Type == SegmentType.Arc)
                {
                    var edge = polygon.Edges[i];
                    var p0 = polygon.Vertices[edge.Start].Position;
                    var p1 = polygon.Vertices[edge.End].Position;
                    var center = edge.Center;

                    float radius = (float)center.DistanceTo(p0);
                    float startAngle = MathF.Atan2(p0.Y - center.Y, p0.X - center.X) * 180f / MathF.PI;
                    float endAngle = MathF.Atan2(p1.Y - center.Y, p1.X - center.X) * 180f / MathF.PI;

                    // Warunek aby zawsze rysować połówkę okręgu na zewnątrz idąc CCW
                    if (edge.Start < edge.End || edge.Start == (polygon.N - 1))
                    {
                        if (endAngle > startAngle)
                            endAngle -= 360f;
                    }
                    else
                    {
                        if (endAngle < startAngle)
                            endAngle += 360f;
                    }

                    float sweepAngle = endAngle - startAngle;

                    var rect = new RectangleF(
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2
                    );
                    var v1 = polygon.Vertices[edge.Start];
                    var v2 = polygon.Vertices[edge.End];
                    Vertex vPrev = new Vertex(PointF.Empty);
                    using var pen = new Pen(Color.MediumVioletRed, 1f);
                    if (v1.ContinuityType == ContinuityClass.G1 || v2.ContinuityType == ContinuityClass.G1)
                    {
                        if (v1.ContinuityType == ContinuityClass.G1)
                        {
                            vPrev = polygon.Vertices[polygon.mod(i - 1, polygon.N)];
                        }
                        else
                        {
                            (p0, p1) = (p1, p0);
                            //p0 = v2.Position;
                            //p1 = v1.Position;
                            vPrev = polygon.Vertices[polygon.mod(edge.End + 1, polygon.N)];
                        }

                        polygon.ComputeArcCenterG1(p0, p1, vPrev.Position, out center, out radius);
                        startAngle = MathF.Atan2(p0.Y - center.Y, p0.X - center.X) * 180f / MathF.PI;
                        endAngle = MathF.Atan2(p1.Y - center.Y, p1.X - center.X) * 180f / MathF.PI;
                        var cross = CrossProduct(vPrev.Position, p0, p1);
                        if (cross > 0)
                        {
                            if (endAngle > startAngle)
                                endAngle -= 360f;
                        }
                        else
                        {
                            if (endAngle < startAngle)
                                endAngle += 360f;
                        }
                        float sweep = endAngle - startAngle;

                        if (radius >= 1e-8f)
                        {
                            _drawer.DrawArc(g, center, radius, startAngle, sweep, pen);
                        }
                    }
                    else
                    {
                        _drawer.DrawArc(g, center, radius, startAngle, sweepAngle, pen);
                    }
                    using var dashPen = new Pen(Color.Gray, 1) { DashPattern = new float[] { 3, 3 } };
                    _drawer.DrawDashedLine(g, p0, p1, dashPen);
                }
                else
                {
                    var v1 = polygon.Vertices[i].Position;
                    var v2 = polygon.Vertices[(i + 1) % polygon.Vertices.Count].Position;
                    var constraint = polygon.Edges[i].Constraint;
                    if (constraint is VerticalConstraint)
                    {
                        _drawer.DrawLine(g, v1, v2, new Pen(Color.Red, 1));
                    }
                    else if (constraint is FixedLengthConstraint)
                    {
                        _drawer.DrawLine(g, v1, v2, new Pen(Color.BlueViolet, 1));
                    }
                    else if (constraint is AngleConstraint45)
                    {
                        _drawer.DrawLine(g, v1, v2, new Pen(Color.DarkOliveGreen));
                        var midX = (v1.X + v2.X) / 2;
                        var midY = (v1.Y + v2.Y) / 2;
                        using (var font = new Font("Arial", 8))
                        using (var brush = new SolidBrush(Color.DarkOliveGreen))
                        {
                            g.DrawString("45°", font, brush, midX + 6, midY - 12);
                        }
                    }
                    else
                        _drawer.DrawLine(g, v1, v2, edgePen);
                    if (constraint is FixedLengthConstraint)
                    {
                        var midX = (v1.X + v2.X) / 2;
                        var midY = (v1.Y + v2.Y) / 2;

                        int length = (int)Math.Round(Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2)));

                        var text = $"{length}";
                        using (var font = new Font("Arial", 8))
                        using (var brush = new SolidBrush(Color.BlueViolet))
                        {
                            g.DrawString(text, font, brush, midX + 4, midY - 12);
                        }
                    }
                }
            }

        }

        private void DrawVertexIndex(Graphics g, Polygon polygon)
        {
            int ind = 0;
            foreach (var v in polygon.Vertices)
            {
                var center = v.Position;
                var text = $"{ind}";
                using (var font = new Font("Arial", 8))
                using (var brush = new SolidBrush(Color.Black))
                {
                    g.DrawString(text, font, brush, center.X + 4, center.Y - 20);
                }
                ind++;
            }
        }

        private void DrawContinuityClasses(Graphics g, Polygon polygon, Vertex v)
        {
            using (var font = new Font("Arial", 8))
            using (var brush = new SolidBrush(Color.Black))
            {
                var center = v.Position;
                var vIdx = polygon.Vertices.IndexOf(v);
                var edge = polygon.Edges[vIdx];
                var prevEdge = polygon.Edges[polygon.mod(vIdx - 1, polygon.M)];
                if (edge.Type != SegmentType.Line ||
                    prevEdge.Type != SegmentType.Line)
                {
                    string continuityType = "";
                    switch (v.continuity)
                    {
                        case G0:
                            continuityType = "G0";
                            break;
                        case G1:
                            continuityType = "G1";
                            break;
                        case C1:
                            continuityType = "C1";
                            break;
                    }
                    g.DrawString(continuityType, font, brush, center.X + 4, center.Y + 5);
                }
            }
        }

       private void DrawControlPoint(Graphics g, PointF p)
        {
            float radius = 5f;
            using (var dashedPen = new Pen(Color.DarkGray, 1) { DashPattern = new float[] { 2, 2 } })
            {
                _drawer.DrawVertex(g, dashedPen, p, radius);
            }
        }

        public float CrossProduct(PointF v1, PointF v2, PointF v3)
        {
            return -((v2.X - v1.X) * (v3.Y - v1.Y) - (v2.Y - v1.Y) * (v3.X - v1.X));
        }
    }
}