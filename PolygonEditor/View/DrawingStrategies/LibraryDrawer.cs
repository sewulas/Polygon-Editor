using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.View.DrawingStrategies
{
    public class LibraryDrawer : IEdgeDrawer
    {
        public void DrawArc(Graphics g, PointF center, float radius, float startAngle, float sweepAngle, Pen pen)
        {
            var rect = new RectangleF(
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2
                    );
            g.DrawArc(pen, rect, startAngle, sweepAngle);
        }

        public void DrawBezier(Graphics g, PointF p0, PointF p1, PointF p2, PointF p3, Pen pen)
        {
            // Algorytm przyrostowy rysowania krzywej Beziera 3-go stopnia
            // z wykorzystaniem bazy potęgowej: B(t) = A*t^3 + B*t^2 + C*t + D
            // gdzie t [0, 1]

            const int steps = 100;     // liczba podziałów parametru t
            float dt = 1f / steps;

            float Ax = -p0.X + 3 * p1.X - 3 * p2.X + p3.X;
            float Ay = -p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y;
            float Bx = 3 * p0.X - 6 * p1.X + 3 * p2.X;
            float By = 3 * p0.Y - 6 * p1.Y + 3 * p2.Y;
            float Cx = -3 * p0.X + 3 * p1.X;
            float Cy = -3 * p0.Y + 3 * p1.Y;
            float Dx = p0.X;
            float Dy = p0.Y;

            float t = 0f;
            float t2 = 0f;
            float t3 = 0f;
            var x = Dx;
            var y = Dy;

            // Przyrostowe obliczanie kolejnych punktów
            for (int i = 0; i < steps; i++)
            {
                var t_next = t + dt;
                var t2_next = t2 + 2 * t * dt + dt * dt;
                var t3_next = t3 + 3 * t2 * dt + 3 * t * dt * dt + dt * dt * dt;

                var x_next = Ax * t3_next + Bx * t2_next + Cx * t_next + Dx;
                var y_next = Ay * t3_next + By * t2_next + Cy * t_next + Dy;
                g.DrawLine(pen, x, y, x_next, y_next);

                // Aktualizacja zmiennych (przyrost)
                t = t_next;
                t2 = t2_next;
                t3 = t3_next;
                x = x_next;
                y = y_next;
            }
        }

        public void DrawDashedLine(Graphics g, PointF p1, PointF p2, Pen pen, int dashLength = 5, int gapLength = 3)
        {
            using var dashPen = new Pen(pen.Color, 1) { DashPattern = new float[] { dashLength, gapLength } };
            g.DrawLine(pen, p1, p2);
        }

        public void DrawEllipse(Graphics g, Pen pen, RectangleF rect)
        {
            g.DrawEllipse(pen, rect);
        }

        public void DrawLine(Graphics g, PointF p0, PointF p1, Pen pen)
        {
            g.DrawLine(pen, p0, p1);
        }

        public void DrawVertex(Graphics g, Pen pen, PointF center, float radius)
        {
            var rect = new RectangleF(
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2
                    );
            g.DrawEllipse(pen, rect);
        }
    }
}
