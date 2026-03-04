using PolygonEditor.Model.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model.Continuities
{
    public class G0 : IContinuity
    {
        public void Apply(Vertex a, PointF prev, Edge prevEdge, Edge bezier, int dir)
        {
        }
    }

    public class G1 : IContinuity
    {
        private PointF oldPosition;

        public G1()
        {
            oldPosition = PointF.Empty;
        }

        public void Initialize(Vertex a)
        {
            oldPosition = a.Position;
        }
        public void UpdateLastPosition(Vertex a) 
        {
            oldPosition = a.Position;
        }
        public void Apply(Vertex a, PointF prev, Edge prevEdge,
            Edge bezier, int dir)
        {
            var cp =  (dir == 1 ? bezier.Cp1 : bezier.Cp2);

            var oldVec = new PointF(cp.X - oldPosition.X, cp.Y - oldPosition.Y);
            float oldLen = oldVec.Length();// MathF.Sqrt(oldVec.X * oldVec.X + oldVec.Y * oldVec.Y);
            if (oldLen < 1e-6f)
                oldLen = 1f;
            // kierunek styczności — linia prev -> a
            var dirVec = new PointF(a.Position.X - prev.X, a.Position.Y - prev.Y);
            float dirLen = dirVec.Length(); // MathF.Sqrt(dirVec.X * dirVec.X + dirVec.Y * dirVec.Y);
            if (dirLen < 1e-6f)
                return; 
            dirVec.X /= dirLen;
            dirVec.Y /= dirLen;

            cp = new PointF(
                a.Position.X + dirVec.X * oldLen,
                a.Position.Y + dirVec.Y * oldLen
            );

            // Upewniamy się, że Cp leży PO stronie przeciwnej względem prev
            // (czyli zachowujemy kolejność prev–a–Cp)
            var vecPrevA = new PointF(a.Position.X - prev.X, a.Position.Y - prev.Y);
            var vecACp = new PointF(cp.X - a.Position.X, cp.Y - a.Position.Y);
            float dot = vecPrevA.X * vecACp.X + vecPrevA.Y * vecACp.Y;
            if (dot < 0)
            {
                // Cp znalazł się po złej stronie — odwracamy kierunek
                dirVec.X = -dirVec.X;
                dirVec.Y = -dirVec.Y;
                cp = new PointF(
                a.Position.X + dirVec.X * oldLen,
                a.Position.Y + dirVec.Y * oldLen
            );
            }

            if (dir == 1)
                bezier.Cp1 = cp;
            else
                bezier.Cp2 = cp;

            oldPosition = a.Position;
        }
    }

    public class C1 : IContinuity
    {
        public void Apply(Vertex a, PointF prev, Edge prevEdge, Edge bezier, int dir)
        {
            var dirVec = new PointF(a.Position.X - prev.X, a.Position.Y - prev.Y);
            float dirLen = dirVec.Length(); // MathF.Sqrt(dirVec.X * dirVec.X + dirVec.Y * dirVec.Y);
            if (dirLen < 1e-6f)
                return;

            var cp = new PointF(
                a.Position.X + dirVec.X,
                a.Position.Y + dirVec.Y
            );

            // Upewniamy się, że Cp leży PO stronie przeciwnej względem prev
            // (czyli zachowujemy kolejność prev–a–Cp)
            var vecPrevA = new PointF(a.Position.X - prev.X, a.Position.Y - prev.Y);
            var vecACp = new PointF(cp.X - a.Position.X, cp.Y - a.Position.Y);
            float dot = vecPrevA.X * vecACp.X + vecPrevA.Y * vecACp.Y;
            if (dot < 0)
            {
                // Cp znalazł się po złej stronie — odwracamy kierunek
                dirVec.X = -dirVec.X;
                dirVec.Y = -dirVec.Y;
                cp = new PointF(
                    a.Position.X + dirVec.X,
                    a.Position.Y + dirVec.Y 
                );
            }

            if (dir == 1)
                bezier.Cp1 = cp;
            else
                bezier.Cp2 = cp;
        }
    }
}
