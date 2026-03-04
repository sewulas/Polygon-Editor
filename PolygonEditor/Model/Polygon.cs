using PolygonEditor.Model.Constraints;
using PolygonEditor.Model.Continuities;
using PolygonEditor.Model.Utility;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PolygonEditor.Model
{
    public class Polygon
    {
        public List<Vertex> Vertices { get; } = new List<Vertex>();
        public List<Edge> Edges { get; } = new List<Edge>();

        public int N { get { return Vertices.Count; } }

        public int M { get { return Edges.Count; } }

        public event Action<Polygon> PolygonChanged;
        public ConstraintSolver Solver { get; } = new();
        public ContinuitiesSolver ContinuitiesSolver { get; }
        protected void RaiseChanged() => PolygonChanged?.Invoke(this);

        public Polygon() 
        {
            ContinuitiesSolver = new ContinuitiesSolver();
            ContinuitiesSolver.Init(this.Solver);
        }
        public Polygon(Vertex[] vertices) 
        {
            Vertices = vertices.ToList();
            CreateEdges();
            ContinuitiesSolver = new ContinuitiesSolver();
            ContinuitiesSolver.Init(this.Solver);
        }

        public Polygon Clone()
        {
            var p = new Polygon();
            foreach (var v in Vertices) p.Vertices.Add(v.Clone());
            foreach (var e in Edges) p.Edges.Add(e.Clone());
            return p;
        }

        private void CreateEdges()
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Edges.Add(new Edge(i, (i + 1)%Vertices.Count));
            }
        }

        private void UpdateEdges(int edgeIndx)
        {
            for (int i = edgeIndx; i < Edges.Count; i++)
            {
                Edges[i].Start = i;
                Edges[i].End = (i+1)%Edges.Count;
            }
        } 

        /// <summary>
        /// Przesuwa cały polygon wraz z kontrolnymi punktami Bezierów.
        /// </summary>
        public void Translate(float dx, float dy, Vertex? dragged = null)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                if (dragged == v)
                    continue;
                v.Position = new PointF(v.Position.X + dx, v.Position.Y + dy);
            }
        }

        public void ApplyMove(Vertex dragged, PointF offset)
        {
            var indx = Vertices.IndexOf(dragged);
            var e1 = Edges[indx];
            var e2 = Edges[mod(indx - 1, M)];
            if (e1.Type == SegmentType.Arc)
                ComputeArcCenter(e1);
            if (e2.Type == SegmentType.Arc)
                ComputeArcCenter(e2);
            Solver.Apply(this, dragged, offset);
            RecomputeArcCenters();
        }

        public void ApplyMoveArc(Vertex dragged, PointF offset, int dir = 1)
        {
            ContinuitiesSolver.ApplyArc(this, dragged, offset, dir);
            RecomputeArcCenters();
        }

        public void ApplyMoveCP((Edge edge, int idx) Cp, PointF offset, PointF newPos)
        {
            Vertex a;
            if (Cp.Item2 == 1)
            {
                Cp.edge.Cp1 = new PointF(newPos.X, newPos.Y);
                a = Vertices[Cp.edge.Start];
            }
            else // if (Cp.Item2 == 2) 
            {
                Cp.edge.Cp2 = new PointF(newPos.X, newPos.Y);
                a = Vertices[Cp.edge.End];
            }
            ContinuitiesSolver.ApplyCP(this, a, Cp, offset);
            RecomputeArcCenters();
        }

        public (bool,string) ValidateContinuityChange(int vertexIndx, ContinuityClass type) 
        {
            if (type == ContinuityClass.G0)
                return (true,String.Empty);
            var edge = Edges[vertexIndx];
            var v1 = Vertices[edge.End];
            var prevEdge = Edges[mod(vertexIndx - 1, M)];
            var v2 = Vertices[prevEdge.Start];

            if (edge.Type == SegmentType.Arc || prevEdge.Type == SegmentType.Arc)
            {
                if (type == ContinuityClass.C1)
                {
                    return (false, "Wierzchołki graniczące w łuk mogą mieć jedynie klasę ciągłości G0 lub G1.");
                }
            }

            if ((edge.Type == SegmentType.Arc && v1.ContinuityType == ContinuityClass.G1) || 
                (prevEdge.Type == SegmentType.Arc && v2.ContinuityType == ContinuityClass.G1))
            {
                return (false, "Tylko jeden wierzchołek łuku może mieć klasę ciągłości G1!");
            }

            return (true,String.Empty);
        }

        public bool ValidateConstraintChange(int edgeInd, ConstraintType constraintType) 
        {
            var n = Edges.Count;
            var before = Edges[mod(edgeInd - 1, n)];
            var after = Edges[(edgeInd + 1) % n];
            if (constraintType == ConstraintType.Vertical && (before.Constraint.type == constraintType || after.Constraint.type == constraintType))
                return false;

            return true;
        }

        public void SetConstraintOnEdge(int edgeIndx, IConstraint type) 
        {
            var edge = Edges[edgeIndx];
            edge.Constraint = type;
            if (edge.Type != SegmentType.Line)
            {
                edge.Type = SegmentType.Line;
                edge.Cp1 = PointF.Empty;
                edge.Cp2 = PointF.Empty;
            }
        }

        /// <summary>
        /// Wstawia vertex na krawędzi wskazanej indeksem, w parametrze t (0..1).
        /// Jeżeli krawędź jest Bezierem - dzieli ją na dwa Beziery (De Casteljau),
        /// jeśli linią - doda wierzchołek w środku (lub wg t).
        /// </summary>
        public void InsertVertexAtEdge(int edgeIndex, float t = 0.5f)
        {
            if (edgeIndex < 0 || edgeIndex >= Edges.Count)
                throw new ArgumentOutOfRangeException(nameof(edgeIndex));
            t = Math.Max(0f, Math.Min(1f, t));

            var edge = Edges[edgeIndex];
            var a = Vertices[edge.Start].Position;
            var b = Vertices[edge.End].Position;

            int insertIndex = edgeIndex + 1;

            var mid = new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            Vertices.Insert(insertIndex, new Vertex(mid));
            Edges.RemoveAt(edgeIndex);
            Edges.Insert(edgeIndex, new Edge(edge.Start, insertIndex, SegmentType.Line));
            Edges.Insert(edgeIndex + 1, new Edge(insertIndex, mod(insertIndex + 1, Vertices.Count), SegmentType.Line));

            UpdateEdges(edgeIndex);

            RaiseChanged();
        }

        /// <summary>
        /// Usuwa wierzchołek o podanym indeksie. Odbudowuje krawędzie jako kolejne pozycje.
        /// Nie usuwa, jeśli liczba wierzchołków < 3.
        /// </summary>
        public void RemoveVertex(int vertexIndex)
        {
            if (vertexIndex < 0 || vertexIndex >= Vertices.Count) throw new ArgumentOutOfRangeException(nameof(vertexIndex));
            if (Vertices.Count <= 3) return; // minimalny polygon

            Vertices.RemoveAt(vertexIndex);
            Edges.RemoveAt(vertexIndex);
            Edges.RemoveAt(mod(vertexIndex - 1, Edges.Count));
            Edges.Insert(mod(vertexIndex - 1, Vertices.Count),
                new Edge(mod(vertexIndex-1, Vertices.Count), mod(vertexIndex, Vertices.Count),
                SegmentType.Line));

            UpdateEdges(vertexIndex);
            RaiseChanged();
        }

        public PointF GetCP(Edge edge, int index) 
        {
            if (edge.Type!=SegmentType.BezierCubic)
                return new PointF();

            return index == 1 ? edge.Cp1 : edge.Cp2;
        }

        public bool IsPartOfArc(Vertex a, int whichEdge)
        {
            // whichEdge = 1  - krawędź, której początkiem jest wierzchołek a
            //           = -1 - krawędź, której koćem jest wierzchołek a
            var indx = Vertices.IndexOf(a);
            var e1 = Edges[indx];
            var e2 = Edges[mod(indx - 1, M)];
            switch (whichEdge)
            {
                case 1:
                    if (e1.Type == SegmentType.Arc)
                        return true;
                    break;
                case -1:
                    if (e2.Type == SegmentType.Arc)
                        return true;
                    break;
            }
            return false;
        }

        public void RecomputeArcCenters()
        {
            for (int i = 0; i < N; i++)
            {
                var v = Vertices[i];
                if (IsPartOfArc(v,1))
                    ComputeArcCenter(Edges[i]);
            }
        }

        public void ComputeArcCenter(Edge edge)
        {
            var p0 = Vertices[edge.Start].Position;
            var p1 = Vertices[edge.End].Position;

            // d = wektor p0->p1
            var d = new PointF(p1.X - p0.X, p1.Y - p0.Y);
            var dLen = p0.DistanceTo(p1); //Distance(p0, p1);
            if (dLen < 1e-6f) return;
            var mid = new PointF((p0.X + p1.X) / 2, (p0.Y + p1.Y) / 2);

            var normal = Normalize(new PointF(-d.Y, d.X));
            float radius = (float)(dLen / 2f);  // domyślnie łuk = półokrąg
            float h = MathF.Sqrt(MathF.Max(0,
                (float)(radius * radius - (dLen / 2f) * (dLen / 2f)))); // h = sqrt(r^2 - d^2)

            // sprawdzenie, po której stronie średnicy/cięciwy ma być łuk
            // czyli na "zewnątrz" wielokąta
            var prev = Vertices[mod(edge.Start - 1, M)].Position;
            var cross = (p1.X - p0.X) * (prev.Y - p0.Y) - (p1.Y - p0.Y) * (prev.X - p0.X);

            // jeśli cross < 0 => wielokąt idzie CW, więc łuk rysujemy na zewnątrz
            if (cross < 0)
            {
                normal.X = -normal.X;
                normal.Y = -normal.Y;
            }

            edge.Center = new PointF(
                mid.X + normal.X * h,
                mid.Y + normal.Y * h
            );
        }

        public bool ComputeArcCenterG1(PointF a, PointF v, PointF vPrev, out PointF center, out float radius)
        {
            center = new PointF();
            radius = 0f;

            var tangent = new PointF(a.X - vPrev.X, a.Y - vPrev.Y);
            float tlen = tangent.Length(); // MathF.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
            if (tlen < 1e-8f) 
                return false;
            tangent.X /= tlen;
            tangent.Y /= tlen;

            var normal = new PointF(-tangent.Y, tangent.X);
            float orient = (v.X - a.X) * (vPrev.Y - a.Y) - (v.Y - a.Y) * (vPrev.X - a.X);
            if (orient > 0)
            {
                normal.X = -normal.X;
                normal.Y = -normal.Y;
            }

            var d = new PointF(v.X - a.X, v.Y - a.Y);
            float dLen = d.Length(); // MathF.Sqrt(d.X * d.X + d.Y * d.Y);
            if (dLen < 1e-8f)
                return false; // degeneracja: a i v zbieżne
            var dNormal = new PointF(-d.Y / dLen, d.X / dLen);
            var mid = new PointF((a.X + v.X) * 0.5f, (a.Y + v.Y) * 0.5f);

            var denom = dNormal.Y * normal.X - dNormal.X * normal.Y;
            if (MathF.Abs(denom) > 1e-8f)
            {
                var rhs = new PointF(mid.X - a.X, mid.Y - a.Y);
                var t1 = (rhs.X * dNormal.Y - rhs.Y * dNormal.X) / denom;
                center = new PointF(a.X + normal.X * t1, a.Y + normal.Y * t1);
            }
            else
            {
                // centrum wzdłuż normalu, a promień = pół średnicy
                center = new PointF(a.X + normal.X * dLen * 0.5f, a.Y + normal.Y * dLen * 0.5f);
            }

            radius = MathF.Sqrt((center.X - a.X) * (center.X - a.X) + (center.Y - a.Y) * (center.Y - a.Y));
            return true;
        }

        private PointF Normalize(PointF v)
        {
            float len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-6f)
                return new PointF(0, 0);
            return new PointF(v.X / len, v.Y / len);
        }

        // Utilities
        public float GetEdgeLength(int edgeIndx)
        {
            var a = Vertices[edgeIndx].Position;
            var b = Vertices[mod(edgeIndx + 1, N)].Position;
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
        public int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
