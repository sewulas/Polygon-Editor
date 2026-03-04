using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Constraints
{
    public interface IConstraint
    {
        void Apply(Vertex a, Vertex b, Vertex draggedVertex);
        public ConstraintType type { get; }
    }
}
