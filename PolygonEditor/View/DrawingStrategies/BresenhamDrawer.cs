using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PolygonEditor.View.DrawingStrategies
{
    public class BresenhamDrawer : IEdgeDrawer
    {
        private void PutPixel(Graphics g, int x, int y, Color color)
        {
            using (var brush = new SolidBrush(color))
                g.FillRectangle(brush, x, y, 1, 1);
        }
        private double NormalizeDeg(double a)
        {
            double r = a % 360.0;
            if (r < 0) r += 360.0;
            return r;
        }
        private bool IsAngleInArc(double angleDeg, double startDeg, double sweepDeg)
        {
            angleDeg = NormalizeDeg(angleDeg);
            startDeg = NormalizeDeg(startDeg);
            double endDeg = NormalizeDeg(startDeg + sweepDeg);

            if (sweepDeg >= 0)
            {
                // idziemy zgodnie z ruchem wskazówek zegara (CW)
                if (endDeg >= startDeg)
                    return angleDeg >= startDeg && angleDeg <= endDeg;
                else
                    return angleDeg >= startDeg || angleDeg <= endDeg;
            }
            else
            {
                // przeciwnie do ruchu wskazówek zegara (CCW)
                if (endDeg <= startDeg)
                    return angleDeg <= startDeg && angleDeg >= endDeg;
                else
                    return angleDeg <= startDeg || angleDeg >= endDeg;
            }
        }

        double AngleDegForPixel(double px, double py, PointF centerF)
        {
            double vx = px - centerF.X;
            double vy = py - centerF.Y;
            double angle = -Math.Atan2(vy, vx) * 180.0 / Math.PI; // w WinFormsach Y rośnie w dół => CW
            return NormalizeDeg(angle);
        }

        public void DrawLine(Graphics g, PointF p1, PointF p2, Pen pen)
        {
            int x1 = (int)Math.Round(p1.X);
            int y1 = (int)Math.Round(p1.Y);
            int x2 = (int)Math.Round(p2.X);
            int y2 = (int)Math.Round(p2.Y);

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);

            // zależnie od orientacji, albo inkrementujemy x jak w klasycznyum algorytmie Midpoint
            // (czyli dla x: idziemy Zachód->Wschód i y: Południe->Północ)
            // albo dekrementujemy (czyli idziemy x: Wschód->Zachód, y: Północ->Południe)
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;

            bool steep = dy > dx;
            if (steep)
            {
                // zamiana osi – rysujemy względem Y
                // Zamiana osi dla linii stromych: teraz iterujemy po osi dłuższej,
                // żeby zachować działanie algorytmu Midpoint.
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);
                (dx, dy) = (dy, dx);
                (sx, sy) = (sy, sx);
            }

            int d = 2 * dy - dx;
            int x = x1;
            int y = y1;
            int incrX = 2 * dy;         // inkrement służący do poruszania się E/W
            int incrY = 2 * (dy - dx);  // inkrement służący do poruszania się NE/NW/SE/SW

            for (x = x1; sx > 0 ? x <= x2 : x >= x2; x += sx)
            {
                if (steep)
                    PutPixel(g, y, x, pen.Color);  // zamienione osie
                else
                    PutPixel(g, x, y, pen.Color);

                if (d < 0)      // Select E/W
                {
                    d += incrX;
                }
                else            // Select NE/NW/SE/SW
                {
                    d+= incrY;
                    y += sy;
                }
            }
        }
        public void DrawDashedLine(Graphics g, PointF p1, PointF p2, Pen pen, int dashLength = 5, int gapLength = 3)
        {
            int x1 = (int)Math.Round(p1.X);
            int y1 = (int)Math.Round(p1.Y);
            int x2 = (int)Math.Round(p2.X);
            int y2 = (int)Math.Round(p2.Y);

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);

            // zależnie od orientacji, albo inkrementujemy x jak w klasycznyum algorytmie Midpoint
            // (czyli dla x: idziemy Zachód->Wschód i y: Południe->Północ)
            // albo dekrementujemy (czyli idziemy x: Wschód->Zachód, y: Północ->Południe)
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;

            bool steep = dy > dx;
            if (steep)
            {
                // zamiana osi – rysujemy względem Y
                // Zamiana osi dla linii stromych: teraz iterujemy po osi dłuższej,
                // żeby zachować działanie algorytmu Midpoint.
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);
                (dx, dy) = (dy, dx);
                (sx, sy) = (sy, sx);
            }

            int d = 2 * dy - dx;
            int x = x1;
            int y = y1;
            int incrX = 2 * dy;         // inkrement służący do poruszania się E/W
            int incrY = 2 * (dy - dx);  // inkrement służący do poruszania się NE/NW/SE/SW

            int patternLength = dashLength + gapLength;
            int patternPos = 0;

            for (x = x1; sx > 0 ? x <= x2 : x >= x2; x += sx)
            {
                // rysuj piksel tylko w części "włączonej"
                if (patternPos < dashLength)
                {
                    if (steep)
                        PutPixel(g, y, x, pen.Color);
                    else
                        PutPixel(g, x, y, pen.Color);
                }

                if (d < 0)      // Select E/W
                {
                    d += incrX;
                }
                else            // Select NE/NW/SE/SW
                {
                    d += incrY;
                    y += sy;
                }

                patternPos = (patternPos + 1) % patternLength;
            }
        }

        public void DrawArc(Graphics g, PointF center, float radius, float startAngle, float sweepAngle, Pen pen)
        {
            // Konwencja: kąty rosną przeciwnie do wskazówek zegara
            startAngle = -startAngle;
            sweepAngle = -sweepAngle;

            void Plot8SymmetricPointsIfInArc(PointF centerF, int xi, int yi)
            {
                var symPoints = new (double X, double Y)[]
                {
                    (centerF.X + xi, centerF.Y + yi),
                    (centerF.X + yi, centerF.Y + xi),
                    (centerF.X - xi, centerF.Y + yi),
                    (centerF.X - yi, centerF.Y + xi),
                    (centerF.X - xi, centerF.Y - yi),
                    (centerF.X - yi, centerF.Y - xi),
                    (centerF.X + xi, centerF.Y - yi),
                    (centerF.X + yi, centerF.Y - xi)
                };

                foreach (var (x, y) in symPoints)
                {
                    double angleDeg = AngleDegForPixel(x, y, centerF);
                    // Sprawdzenie, czy dany punkt leży w przedziale kątowym naszego łuku
                    if (IsAngleInArc(angleDeg, startAngle, sweepAngle))
                    {
                        PutPixel(g, (int)Math.Round(x), (int)Math.Round(y), pen.Color);
                    }
                }
            }

            int R = (int)Math.Round(radius);
            if (R <= 0)
            {
                return;
            }

            int d = 1 - R;
            int x = 0;
            int y = R;
            int deltaE = 3;
            int deltaSE = 5 - 2 * R;

            Plot8SymmetricPointsIfInArc(center, x, y);

            while (y > x)
            {
                if (d < 0)  //Select E
                {
                    d += deltaE;
                    deltaE += 2;
                    deltaSE += 2;
                }
                else        //Select SE
                {
                    d += deltaSE;
                    deltaE += 2;
                    deltaSE += 4;
                    y--;
                }
                x++;

                Plot8SymmetricPointsIfInArc(center, x, y);
            }
        }

        public void DrawBezier(Graphics g, PointF p0, PointF p1, PointF p2, PointF p3, Pen pen)
        {
            // Algorytm przyrostowy rysowania krzywej Beziera 3-go stopnia
            // z wykorzystaniem bazy potęgowej: B(t) = A*t^3 + B*t^2 + C*t + D
            // gdzie t jest z przedziału [0, 1]

            const int steps = 100; 
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
                DrawLine(g, new PointF(x, y), new PointF(x_next, y_next), pen);

                // Aktualizacja zmiennych (przyrost)
                t = t_next;
                t2 = t2_next;
                t3 = t3_next;
                x = x_next;
                y = y_next;
            }
        }

        public void DrawVertex(Graphics g, Pen pen, PointF center, float radius)
        {
            int x0 = (int)Math.Round(center.X);
            int y0 = (int)Math.Round(center.Y);
            int R = (int)Math.Round(radius);

            int deltaE = 3;
            int deltaSE = 5 - 2 * R;
            int d = 1 - R;
            int x = 0;
            int y = R;

            PutPixel(g, x0 + x, y0 + y, pen.Color);
            PutPixel(g, x0 - x, y0 + y, pen.Color);
            PutPixel(g, x0 + x, y0 - y, pen.Color);
            PutPixel(g, x0 - x, y0 - y, pen.Color);
            PutPixel(g, x0 + y, y0 + x, pen.Color);
            PutPixel(g, x0 - y, y0 + x, pen.Color);
            PutPixel(g, x0 + y, y0 - x, pen.Color);
            PutPixel(g, x0 - y, y0 - x, pen.Color);

            while (y >= x)
            {

                if (d < 0) //Select E
                {
                    d += deltaE;
                    deltaE += 2;
                    deltaSE += 2;
                }
                else //Select SE
                {
                    d += deltaSE;
                    deltaE += 2;
                    deltaSE += 4;
                    y--;
                }
                x++;
                PutPixel(g, x0 + x, y0 + y, pen.Color);
                PutPixel(g, x0 - x, y0 + y, pen.Color);
                PutPixel(g, x0 + x, y0 - y, pen.Color);
                PutPixel(g, x0 - x, y0 - y, pen.Color);
                PutPixel(g, x0 + y, y0 + x, pen.Color);
                PutPixel(g, x0 - y, y0 + x, pen.Color);
                PutPixel(g, x0 + y, y0 - x, pen.Color);
                PutPixel(g, x0 - y, y0 - x, pen.Color);
            }
        }
    }
}