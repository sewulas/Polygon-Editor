using PolygonEditor.Model.Constraints;
using PolygonEditor.Model.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Continuities
{
    public class ContinuitiesSolver
    {
        private ConstraintSolver _constraintSolver = new ConstraintSolver();
        public ContinuitiesSolver() { }
        public void Init(ConstraintSolver constraintSolver) 
        {
            this._constraintSolver = constraintSolver;
        }

        public void ApplyCP(Polygon polygon, Vertex a, (Edge edge, int idx) draggedCP, PointF offset)
        {
            var edgeIndx = polygon.Edges.IndexOf(draggedCP.edge);
            var CP = draggedCP.idx == 1 ? draggedCP.edge.Cp1 : draggedCP.edge.Cp2;
            switch (a.ContinuityType)
            {
                case ContinuityClass.G0:
                    return;
                case ContinuityClass.G1:
                    MoveG1Bezier(polygon, a, edgeIndx, CP, draggedCP.idx, offset);
                    return;
                case ContinuityClass.C1:
                    MoveC1Bezier(polygon, a, edgeIndx, CP, draggedCP.idx, offset);
                    return;
            }
        }

        public void ApplyArc(Polygon polygon, Vertex a, PointF offset, int dir = 1)
        {
            if (a.ContinuityType != ContinuityClass.G1)
                return;

            int aIdx = polygon.Vertices.IndexOf(a);
            var v = polygon.Vertices[polygon.mod(aIdx + dir, polygon.N)];
            

            _constraintSolver.Apply(polygon, v, offset, dir);
        }

        public bool ComputeArcCenterG1(PointF a, PointF v, PointF vPrev, out PointF center, out float radius)
        {
            center = new PointF();
            radius = 0f;

            // 1) kierunek stycznej w a
            var tangent = new PointF(a.X - vPrev.X, a.Y - vPrev.Y);
            float tlen = MathF.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
            if (tlen < 1e-8f)
                return false;
            tangent.X /= tlen;
            tangent.Y /= tlen;

            // 2) normal (prostopadły do stycznej)
            var normal = new PointF(-tangent.Y, tangent.X);

            // 3) wektor chordy a->v i jego długość
            var chord = new PointF(v.X - a.X, v.Y - a.Y);
            float chordLen = MathF.Sqrt(chord.X * chord.X + chord.Y * chord.Y);
            if (chordLen < 1e-8f) return false; // degeneracja: a i v zbieżne

            // jednostkowy wektor prostopadły do chordy (symetralna)
            var chordNormal = new PointF(-chord.Y / chordLen, chord.X / chordLen);
            var mid = new PointF((a.X + v.X) * 0.5f, (a.Y + v.Y) * 0.5f);

            // 4) przecięcie linii: a + normal*t1 = mid + chordNormal*t2
            float denom = chordNormal.Y * normal.X - chordNormal.X * normal.Y;

            if (MathF.Abs(denom) > 1e-8f)
            {
                var rhs = new PointF(mid.X - a.X, mid.Y - a.Y);
                float t1 = (rhs.X * chordNormal.Y - rhs.Y * chordNormal.X) / denom;
                center = new PointF(a.X + normal.X * t1, a.Y + normal.Y * t1);
            }
            else
            {
                // fallback: linie równoległe (prawie) -> ustaw centrum wzdłuż normalu, promień = pół chordy
                center = new PointF(a.X + normal.X * chordLen * 0.5f, a.Y + normal.Y * chordLen * 0.5f);
            }

            // 5) promień
            radius = MathF.Sqrt((center.X - a.X) * (center.X - a.X) + (center.Y - a.Y) * (center.Y - a.Y));

            return true;
        }

        public void MoveG1Bezier(Polygon polygon, Vertex a, int edgeIndx, PointF CP, int indx, PointF offset) 
        {
            int dir = indx == 1 ? -1 : 1;
            var prevEdge = polygon.Edges[mod(edgeIndx + dir, polygon.M)];
            // Linia v2 -- a -- Cp
            var dirVec = new PointF(a.Position.X - CP.X, a.Position.Y - CP.Y);
            float dirLen = MathF.Sqrt(dirVec.X * dirVec.X + dirVec.Y * dirVec.Y);
            if (dirLen < 1e-6f)
                return;

            dirVec.X /= dirLen;
            dirVec.Y /= dirLen;
            if (prevEdge.Type == SegmentType.BezierCubic)
            {
                if (indx == 1)
                {
                    var CP2 = prevEdge.Cp2;
                    var oldVec = new PointF(CP2.X - a.Position.X, CP2.Y - a.Position.Y);
                    float oldLen = MathF.Sqrt(oldVec.X * oldVec.X + oldVec.Y * oldVec.Y);
                    if (oldLen < 1e-6f)
                        oldLen = 1f;

                    prevEdge.Cp2 = new PointF(
                                a.Position.X + dirVec.X * oldLen,
                                a.Position.Y + dirVec.Y * oldLen
                            );
                }
                else
                {
                    var CP1 = prevEdge.Cp1;
                    var oldVec = new PointF(CP1.X - a.Position.X, CP1.Y - a.Position.Y);
                    float oldLen = MathF.Sqrt(oldVec.X * oldVec.X + oldVec.Y * oldVec.Y);
                    if (oldLen < 1e-6f)
                        oldLen = 1f;

                    prevEdge.Cp1 = new PointF(
                                a.Position.X + dirVec.X * oldLen,
                                a.Position.Y + dirVec.Y * oldLen
                            );
                }
                return;
            }
            else
            {
                Vertex v2 = dir == 1
                    ? polygon.Vertices[prevEdge.End]
                    : polygon.Vertices[prevEdge.Start];
                if (prevEdge.Constraint.type == ConstraintType.FixedLength ||
                    prevEdge.Constraint.type == ConstraintType.None)
                {
                    var oldVec = new PointF(v2.Position.X - a.Position.X, v2.Position.Y - a.Position.Y);
                    float oldLen = MathF.Sqrt(oldVec.X * oldVec.X + oldVec.Y * oldVec.Y);
                    if (oldLen < 1e-6f)
                        oldLen = 1f; // unikamy dzielenia przez zero

                    v2.Position = new PointF(
                                a.Position.X + dirVec.X * oldLen,
                                a.Position.Y + dirVec.Y * oldLen
                            );

                    _constraintSolver.Apply(polygon, v2, offset);
                }
                else if (prevEdge.Constraint is VerticalConstraint ||
                         prevEdge.Constraint is AngleConstraint45)
                {
                    a.Position = new PointF(a.Position.X + offset.X,
                                            a.Position.Y + offset.Y);
                    v2.Position = new PointF(v2.Position.X + offset.X,
                                             v2.Position.Y + offset.Y);
                    if (a.continuity is G1)
                    {
                        (a.continuity as G1).UpdateLastPosition(a);
                    }
                    _constraintSolver.Apply(polygon, v2, offset, dir);
                }
            }

            return;
        }

        public void MoveC1Bezier(Polygon polygon, Vertex a, int edgeIndx, PointF CP, int indx, PointF offset)
        {
            int dir = indx == 1 ? -1 : 1;
            var prevEdge = polygon.Edges[mod(edgeIndx + dir, polygon.M)];
            // Linia v2 -- a -- Cp
            var dirVec = new PointF(a.Position.X - CP.X, a.Position.Y - CP.Y);
            var dirLen = MathF.Sqrt(dirVec.X * dirVec.X + dirVec.Y * dirVec.Y);
            if (dirLen < 1e-6f)
                return; // brak sensownego kierunku
            if (prevEdge.Type == SegmentType.BezierCubic)
            {
                if (indx == 1)
                {

                    prevEdge.Cp2 = new PointF(
                                a.Position.X + dirVec.X,
                                a.Position.Y + dirVec.Y
                            );
                }
                else
                {
                    prevEdge.Cp1 = new PointF(
                                a.Position.X + dirVec.X,
                                a.Position.Y + dirVec.Y
                            );
                }
                return;
            }
            else
            {
                Vertex v2 = dir == 1
                    ? polygon.Vertices[prevEdge.End]
                    : polygon.Vertices[prevEdge.Start];
                if (prevEdge.Constraint is NoConstraint)
                {
                    v2.Position = new PointF(
                                a.Position.X + dirVec.X,
                                a.Position.Y + dirVec.Y
                            );

                    _constraintSolver.Apply(polygon, v2, offset);
                }
                else if (prevEdge.Constraint.type == ConstraintType.Vertical)
                {
                    a.Position = new PointF(a.Position.X + offset.X,
                                            a.Position.Y);
                    v2.Position = new PointF(v2.Position.X + offset.X,
                                             a.Position.Y + dirVec.Y);
                    _constraintSolver.Apply(polygon, v2, offset, dir);
                }
                else if (prevEdge.Constraint.type == ConstraintType.FixedLength)
                {
                    a.Position = new PointF(a.Position.X + offset.X,
                                            a.Position.Y + offset.Y);
                    v2.Position = new PointF(v2.Position.X + offset.X,
                                             v2.Position.Y + offset.Y);

                    _constraintSolver.Apply(polygon, v2, offset, dir);
                }
                else if (prevEdge.Constraint.type == ConstraintType.Angle45)
                {
                    a.Position = new PointF(a.Position.X + offset.X,
                                            a.Position.Y + offset.Y);
                    v2.Position = new PointF(v2.Position.X + offset.X,
                                             v2.Position.Y + offset.Y);

                    _constraintSolver.Apply(polygon, v2, offset, dir);
                }
            }
            return;
        }

        public int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
