using PolygonEditor.Model.Continuities;
using PolygonEditor.Model.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Constraints
{
    public class ConstraintSolver
    {
        private const float Tolerance = 1f; // dopuszczalna różnica pozycji

        /// <summary>
        /// Aplikuje wskazany przez parametry ruch wierzchołka i propaguje dalej
        /// zmiany zgodnie z ograniczeniami narzuconymi na krawędzie.
        /// </summary>
        public void Apply(Polygon polygon, Vertex draggedVertex, PointF offset, int? dir = null)
        {
            int index = polygon.Vertices.IndexOf(draggedVertex);

            if (dir is null)
            {
                Propagate(polygon, index, -1, draggedVertex);
                Propagate(polygon, index, 1, draggedVertex);
                Propagate(polygon, index, -1, draggedVertex);
                Propagate(polygon, index, 1, draggedVertex);
            }
            else if (dir == 1) 
            {
                Propagate(polygon, index, 1, draggedVertex);
            }
            else if (dir == -1)
            {
                Propagate(polygon, index, -1, draggedVertex);
            }
        }

        /// <summary>
        /// Sprawdza, czy można w obecnym położeniu krawędzi i wierzchołków, nadać krawędzi
        /// dane ograniczenie, wykonując serię sprawdzeń na kopii wielokąta.
        /// </summary>
        public (bool, string?) Validate(Polygon polygon, int edgeIndex, ConstraintType type, float fixedLength = 0) 
        {
            var clone = polygon.Clone();
            var start = clone.Vertices[edgeIndex];
            var end = clone.Vertices[mod(edgeIndex + 1, polygon.Vertices.Count)];
            var e1 = polygon.Edges[mod(edgeIndex - 1, polygon.Edges.Count)];
            var e2 = polygon.Edges[mod(edgeIndex + 1, polygon.Edges.Count)];
            var n = polygon.Vertices.Count;

            switch (type)
            {
                case ConstraintType.FixedLength:
                    // nierówność trójkąta
                    if (n == 3 && e1.Constraint is FixedLengthConstraint && e2.Constraint is FixedLengthConstraint)
                    {
                        var msg = "Nierówność trójkąta nie jest spełniona";
                        if (fixedLength > e1.FixedLength + e2.FixedLength)
                            return (false, msg);
                        if (e1.FixedLength > fixedLength + e2.FixedLength)
                            return (false, msg);
                        if (e2.FixedLength > fixedLength + e1.FixedLength)
                            return (false, msg);
                        return (true, null);
                    }

                    var dx = end.Position.X - start.Position.X;
                    var dy = end.Position.Y - start.Position.Y;
                    var currentLength = start.Position.DistanceTo(end.Position);
                    var scale = fixedLength / currentLength - 1;
                    var offset = new PointF(dx * scale, dy * scale);

                    clone.SetConstraintOnEdge(edgeIndex, new FixedLengthConstraint(fixedLength));
                    clone.Edges[edgeIndex].FixedLength = fixedLength;
                    clone.ApplyMove(end, offset);

                    foreach (var e in clone.Edges)
                    {
                        // sprawdzenie zachowania długości innych FixedLengthConstraint
                        if (e.Constraint.type != ConstraintType.FixedLength)
                            continue;
                        var len = clone.GetEdgeLength(e.Start);
                        if (Math.Abs(len - e.FixedLength) > Tolerance)
                        {
                            return (false, "Operacja niemożliwa");
                        }
                    }

                    break;

                case ConstraintType.Vertical:

                    clone.SetConstraintOnEdge(edgeIndex, new VerticalConstraint());
                    offset = new PointF(start.Position.X - end.Position.X, 0);
                    clone.ApplyMove(end, offset);
                    var dif = start.Position.X - end.Position.X;
                    if (!(start.Position.X != end.Position.X && Math.Abs(dif) >= Tolerance))
                        break;

                    clone = polygon.Clone();
                    clone.SetConstraintOnEdge(edgeIndex, new VerticalConstraint());
                    end = clone.Vertices[edgeIndex];
                    start = clone.Vertices[mod(edgeIndex + 1, polygon.Vertices.Count)];
                    offset = new PointF(start.Position.X - end.Position.X, 0);
                    clone.ApplyMove(end, offset);
                    dif = start.Position.X - end.Position.X;
                    if (!(start.Position.X != end.Position.X && Math.Abs(dif) >= Tolerance))
                        break;
                    return (false, "Operacja niemożliwa");

                case ConstraintType.Angle45:
                    clone.SetConstraintOnEdge(edgeIndex, new AngleConstraint45());
                    
                    float vx = end.Position.X - start.Position.X;
                    float vy = end.Position.Y - start.Position.Y;
                    float invSqrt2 = 1f / (float)Math.Sqrt(2f);
                    var u = new PointF(invSqrt2, -invSqrt2);

                    float dot_au = vx * u.X + vy * u.Y;
                    float dot_uu = u.X * u.X + u.Y * u.Y;
                    var orthogonal = new PointF(
                        vx - (dot_au / dot_uu) * u.X,
                        vy - (dot_au / dot_uu) * u.Y
                    );
                    offset = new PointF(-orthogonal.X, -orthogonal.Y);
                    clone.ApplyMove(end, offset);
                    var difX = Math.Abs(end.Position.X - start.Position.X);
                    var difY = Math.Abs(end.Position.Y - start.Position.Y);
                    if (Math.Abs(difX - difY) < Tolerance)
                        break;

                    // Sprawdzamy, czy zamiast ruszając początkiem krawędzi zamiast końcem, uzyskamy
                    // lepsze przybliżenie
                    clone = polygon.Clone();
                    clone.SetConstraintOnEdge(edgeIndex, new AngleConstraint45());
                    end = clone.Vertices[edgeIndex];
                    start = clone.Vertices[mod(edgeIndex + 1, polygon.Vertices.Count)];
                    vx = end.Position.X - start.Position.X;
                    vy = end.Position.Y - start.Position.Y;

                    dot_au = vx * u.X + vy * u.Y;
                    dot_uu = u.X * u.X + u.Y * u.Y;
                    orthogonal = new PointF(
                        vx - (dot_au / dot_uu) * u.X,
                        vy - (dot_au / dot_uu) * u.Y
                    );
                    offset = new PointF(-orthogonal.X, -orthogonal.Y);
                    clone.ApplyMove(end, offset);
                    difX = Math.Abs(end.Position.X - start.Position.X);
                    difY = Math.Abs(end.Position.Y - start.Position.Y);
                    if (Math.Abs(difX - difY) < Tolerance)
                        break;

                    // Sprawdzamy, czy zamiast rzutowania na prostą 45deg przesunięcie o odpowiedni y
                    // wystarczy aby zachować constrainty
                    clone = polygon.Clone();
                    start = clone.Vertices[edgeIndex];
                    end = clone.Vertices[mod(edgeIndex + 1, clone.Vertices.Count)];

                    dy = start.Position.X - end.Position.X + start.Position.Y - end.Position.Y;
                    end.Position = new PointF(end.Position.X, end.Position.Y + dy);
                    offset = new PointF(0, 0);
                    clone.ApplyMove(end, offset);
                    difX = Math.Abs(end.Position.X - start.Position.X);
                    difY = Math.Abs(end.Position.Y - start.Position.Y);
                    if (Math.Abs(difX - difY) < Tolerance)
                        break;

                    return (false, "Operacja niemożliwa");
            }

            return (true, null);
        }
        /// <summary>
        /// Poprawia lokalne ograniczenia wielokąta w sposób iteracyjny, idąc w kieurnku zgodnie
        /// z wartością zmiennej 'dir': 
        /// 1   = przeciwnie do wskazówek zegara
        /// -1  = zgodnie ze wskazówkami zegara
        /// </summary>
        private void Propagate(Polygon polygon, int start, int dir, Vertex draggedVertex, int calledCpIdx = -1)
        {
            int current = start;

            while (true)
            {
                int edgeIndex = dir == -1
                    ? polygon.mod(current - 1, polygon.Edges.Count)
                    : current;

                var edge = polygon.Edges[edgeIndex];
                var v = dir == -1 ? polygon.Vertices[edge.End] : polygon.Vertices[edge.Start];
                if (edge.Type == SegmentType.BezierCubic)
                {
                    PropagateBezier(polygon, v, dir, edgeIndex);
                    return;
                }
                else if (edge.Constraint is NoConstraint)
                {
                    v = dir == -1 ? polygon.Vertices[edge.Start] : polygon.Vertices[edge.End];
                    if (calledCpIdx == -1 || !ReferenceEquals(v, polygon.Vertices[calledCpIdx]))
                    {
                        PropagateBezier(polygon, v, dir, mod(edgeIndex + dir, polygon.M));
                        return;
                    }
                }
                if (edge.Constraint is NoConstraint)
                    break;

                int next = polygon.mod(current + dir, polygon.Vertices.Count);
                if (start == next)
                    break;

                var v1 = polygon.Vertices[current];
                var v2 = polygon.Vertices[next];

                edge.Constraint.Apply(v1, v2, draggedVertex);
                current = next;
            }
        }

        /// <summary>
        /// Poprawia lokalne ograniczenia wielokąta w sposób iteracyjny, 
        /// związane z krzymi Beziera.
        /// </summary>
        private void PropagateBezier(Polygon polygon, Vertex a, int dir, int edgeIndex)
        {
            var edge = polygon.Edges[edgeIndex];
            var prevEdge = polygon.Edges[mod(edgeIndex - dir, polygon.M)]; // git
            PointF prevPos = new PointF();
            if (prevEdge.Type == SegmentType.BezierCubic)
            {
                if (dir == 1)
                    prevPos = prevEdge.Cp2;
                else if (dir == -1)
                    prevPos = prevEdge.Cp1;
            }
            else
            {
                prevPos = dir == -1 
                    ? polygon.Vertices[prevEdge.End].Position
                    : polygon.Vertices[prevEdge.Start].Position;
            }
            a.continuity.Apply(a, prevPos,
                    prevEdge,
                    edge, dir);
            return;
        }

        public int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
