using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.View.DrawingStrategies
{
    public interface IEdgeDrawer
    {
        void DrawLine(Graphics g, PointF p0, PointF p1, Pen pen);
        void DrawDashedLine(Graphics g, PointF p0, PointF p1, Pen pen, int dashLength = 5, int gapLength = 3);

        void DrawArc(Graphics g, PointF center, float radius, float startAngle, float sweepAngle, Pen pen);

        void DrawBezier(Graphics g, PointF p0, PointF p1, PointF p2, PointF p3, Pen pen);
        void DrawVertex(Graphics g, Pen pen, PointF center, float radius);
    }
}
