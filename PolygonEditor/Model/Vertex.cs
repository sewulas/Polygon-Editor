using PolygonEditor.Model.Continuities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model
{
    /// <summary>
    /// Represents a vertex of a polygon
    /// </summary>
    public class Vertex
    {
        public PointF Position { get; set; }
        public ContinuityClass ContinuityType { get; set; } = ContinuityClass.G0;
        public IContinuity continuity { get; set; } = new G0();
        public Vertex(float x, float y)
        {
            Position = new PointF(x, y);
        }

        public Vertex(PointF p)
        {
            Position = p;
        }

        public Vertex Clone() => new Vertex(Position) { ContinuityType = this.ContinuityType,
                                                        continuity = this.continuity}; // tu moze byc przypal
    }
}
