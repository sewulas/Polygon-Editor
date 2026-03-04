using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model
{
    /// <summary>
    /// Kontener sceny. Trzyma listę polygonów i event przy zmianie sceny.
    /// </summary>
    public class Scene
    {
        private readonly List<Polygon> _polygons = new List<Polygon>();
        public IReadOnlyList<Polygon> Polygons => _polygons.AsReadOnly();

        public int ActivePolygonIndex { get; private set; } = -1;
        public Polygon ActivePolygon => (ActivePolygonIndex >= 0 && ActivePolygonIndex < _polygons.Count) ? _polygons[ActivePolygonIndex] : null;

        public event Action<Scene> SceneChanged;

        protected void RaiseChanged() => SceneChanged?.Invoke(this);

        public Scene() { }

        public void AddPolygon(Polygon p, bool setActive = true)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            _polygons.Add(p);
            SubscribePolygon(p);
            if (setActive) ActivePolygonIndex = _polygons.Count - 1;
            RaiseChanged();
        }

        public void RemovePolygonAt(int index)
        {
            if (index < 0 || index >= _polygons.Count) throw new ArgumentOutOfRangeException(nameof(index));
            UnsubscribePolygon(_polygons[index]);
            _polygons.RemoveAt(index);
            if (_polygons.Count == 0) ActivePolygonIndex = -1;
            else ActivePolygonIndex = Math.Min(ActivePolygonIndex, _polygons.Count - 1);
            RaiseChanged();
        }

        public void SetActivePolygon(int index)
        {
            if (index < -1 || index >= _polygons.Count) throw new ArgumentOutOfRangeException(nameof(index));
            ActivePolygonIndex = index;
            RaiseChanged();
        }

        private void SubscribePolygon(Polygon p)
        {
            p.PolygonChanged += OnPolygonChanged;
        }

        private void UnsubscribePolygon(Polygon p)
        {
            p.PolygonChanged -= OnPolygonChanged;
        }

        private void OnPolygonChanged(Polygon p)
        {
            RaiseChanged();
        }


        public void Clear()
        {
            foreach (var p in _polygons) UnsubscribePolygon(p);
            _polygons.Clear();
            ActivePolygonIndex = -1;
            RaiseChanged();
        }
    }
}
