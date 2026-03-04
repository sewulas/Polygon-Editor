using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Continuities
{
    public interface IContinuity
    {
        void Apply(Vertex a, PointF prev, Edge prevEdge, Edge bezier, int dir);
    }
}
