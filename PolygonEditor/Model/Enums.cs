using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model
{
    public enum SegmentType
    {
        Line,
        BezierCubic,
        Arc
    }

    public enum ConstraintType
    {
        None,
        Horizontal,
        Vertical,
        Angle45,
        FixedLength
    }

    public enum ContinuityClass
    {
        G0,
        G1,
        C1
    }
}
