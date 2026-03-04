using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Constraints
{
    public class NoConstraint : IConstraint
    {
        public ConstraintType type => ConstraintType.None;
        public void Apply(Vertex a, Vertex b, Vertex m)
        {
        }
    }

    public class VerticalConstraint : IConstraint
    {
        public ConstraintType type => ConstraintType.Vertical;

        public void Apply(Vertex a, Vertex b, Vertex draggedVertex)
        {
            if (ReferenceEquals(draggedVertex, b))
                a.Position = new PointF(b.Position.X, a.Position.Y);
            else
                b.Position = new PointF(a.Position.X, b.Position.Y); // domyślnie
        }
    }

    public class HorizontalConstraint : IConstraint
    {
        public ConstraintType type => ConstraintType.Horizontal;
        public void Apply(Vertex a, Vertex b, Vertex draggedVertex)
        {
            b.Position = new PointF(b.Position.X, a.Position.Y);
        }
    }

    public class FixedLengthConstraint : IConstraint
    {
        public ConstraintType type => ConstraintType.FixedLength;
        private readonly float _length;

        public FixedLengthConstraint(float length)
        {
            _length = length;
        }

        public void Apply(Vertex a, Vertex b, Vertex draggedVertex)
        {
            var dx = b.Position.X - a.Position.X;
            var dy = b.Position.Y - a.Position.Y;
            var currentLength = MathF.Sqrt(dx * dx + dy * dy);
            if (currentLength == 0 || currentLength == _length)
                return;
            if (currentLength < 1e-3f)
                return;

            var ux = dx / currentLength;
            var uy = dy / currentLength;
            if (ReferenceEquals(draggedVertex, b))
            {
                // przesuwamy tylko 'a'
                a.Position = new PointF(
                    b.Position.X - ux * _length,
                    b.Position.Y - uy * _length
                );
            }
            else if (ReferenceEquals(draggedVertex, a))
            {
                // przesuwamy tylko 'b'
                b.Position = new PointF(
                    a.Position.X + ux * _length,
                    a.Position.Y + uy * _length
                );
            }
            else
            {
                b.Position = new PointF(
                    a.Position.X + ux * _length,
                    a.Position.Y + uy * _length
                );
            }

        }
    }

    public class AngleConstraint45 : IConstraint
    {
        public ConstraintType type => ConstraintType.Angle45;
        public void Apply(Vertex a, Vertex b, Vertex draggedVertex)
        {
            float vx = b.Position.X - a.Position.X;
            float vy = b.Position.Y - a.Position.Y;
            float invSqrt2 = 1f / (float)Math.Sqrt(2f);
            var u = new PointF(invSqrt2, -invSqrt2);

            float dot_au = vx * u.X + vy * u.Y;
            float dot_uu = u.X * u.X + u.Y * u.Y;
            var orthogonal = new PointF(
                vx - (dot_au / dot_uu) * u.X,
                vy - (dot_au / dot_uu) * u.Y
            );

            b.Position = new PointF(
                b.Position.X - orthogonal.X,
                b.Position.Y - orthogonal.Y
            );
        }
    }
}
