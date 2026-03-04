using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Utility
{
    public static class PointFExtensions
    {
        public static float DistanceTo(this PointF point, PointF b)
        {
            var dx = b.X - point.X;
            var dy = b.Y - point.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float Length(this PointF point)
        {
            return (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
        }
    }
}
