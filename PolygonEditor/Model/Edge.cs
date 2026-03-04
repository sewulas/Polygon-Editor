using PolygonEditor.Model.Constraints;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model
{
    /// <summary>
    /// Reprezentuje krawędź między dwoma wierzchołkami wielokąta.
    /// Przechowuje typ (Line/Bezier/Arc) i kontrolne dane.
    /// </summary>
    public class Edge
    {
        public int Start { get; set; }
        public int End { get; set; }
        public SegmentType Type { get; set; } = SegmentType.Line;

        public PointF Cp1 { get; set; } = PointF.Empty;
        public PointF Cp2 { get; set; } = PointF.Empty;

        public IConstraint Constraint { get; set; } = new NoConstraint();
        public float FixedLength { get; set; } = 0f; // używane gdy Constraint == FixedLength

        // Arc
        public PointF Center { get; set; } = PointF.Empty;

        public Edge(int start, int end, SegmentType type = SegmentType.Line)
        {
            Start = start;
            End = end;
            Type = type;
        }

        public Edge Clone() => new Edge(Start, End, Type)
        {
            Cp1 = this.Cp1,
            Cp2 = this.Cp2,
            Constraint = this.Constraint,
            FixedLength = this.FixedLength
        };

        public bool HasConstraint(ConstraintType type)
        {
            return Constraint.type == type;
        }
    }
}
