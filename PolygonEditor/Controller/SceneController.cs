using PolygonEditor.Model;
using PolygonEditor.Model.Constraints;
using PolygonEditor.Model.Continuities;
using PolygonEditor.Model.Utility;
using PolygonEditor.View;
using PolygonEditor.View.DrawingStrategies;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace PolygonEditor.Controller
{
    public class SceneController
    {
        private readonly SceneView _view;
        private readonly Scene _scene;

        private Vertex _draggedVertex;
        private PointF _dragOffset;

        // context menu for edge editing
        private ContextMenuStrip _contextMenuEdge;
        private ContextMenuStrip _contextMenuVertex;

        private Polygon _selectedPolygon;
        private int _selectedEdgeIndex = -1; 
        private int _selectedVertexIndex = -1; 
        private (Edge edge, int index)? _draggedControlPoint = null;
        // index: 1 = CP1, 2 = CP2
        private ContinuityClass _defualtForBezier = ContinuityClass.C1;

        public SceneController(SceneView view, Scene scene)
        {
            _view = view;
            _scene = scene;

            _scene.SceneChanged += OnSceneChanged;

            InitializeContextsMenu();

            // Subskrybujemy zdarzenia myszki z widoku
            _view.MouseDown += OnMouseDown;
            _view.MouseMove += OnMouseMove;
            _view.MouseUp += OnMouseUp;
        }

        private void OnSceneChanged(Scene scene)
        {
            _view.Invalidate();
        }

        private void InitializeContextsMenu()
        {
            // --- MENU DLA KRAWĘDZI ---
            _contextMenuEdge = new ContextMenuStrip();
            _contextMenuEdge.Opening += OnEdgeContextMenuOpening;

            _contextMenuEdge.Items.Add("Dodaj wierzchołek", null, OnAddVertexClicked!);

            // --- PODMENU: Ograniczenia krawędzi ---
            var constraintMenu = new ToolStripMenuItem("Dodaj ograniczenie");
            var verticalItem = new ToolStripMenuItem("Pionowa")
            {
                CheckOnClick = true
            };
            verticalItem.Click += OnVerticalConstraintClicked;
            constraintMenu.DropDownItems.Add(verticalItem);

            var fixedLengthItem = new ToolStripMenuItem("Długość")
            {
                CheckOnClick = true
            };
            fixedLengthItem.Click += OnFixedLengthConstraintClicked;
            constraintMenu.DropDownItems.Add(fixedLengthItem);

            var Angle45Item = new ToolStripMenuItem("45°")
            {
                CheckOnClick = true
            };
            Angle45Item.Click += OnAngle45ConstraintClicked;
            constraintMenu.DropDownItems.Add(Angle45Item);

            _contextMenuEdge.Items.Add(constraintMenu);
            var arcItem = new ToolStripMenuItem("Zmień na łuk okręgu")
            {
                CheckOnClick = true
            };
            arcItem.Click += OnArcSegmentToggleClicked;
            _contextMenuEdge.Items.Add(arcItem);
            var bezierItem = new ToolStripMenuItem("Zmień na krzywą Beziera")
            {
                CheckOnClick = true
            };
            bezierItem.Click += OnBezierSegmentToggleClicked;
            _contextMenuEdge.Items.Add(bezierItem);

            var removeConstraintItem = new ToolStripMenuItem("Usuń ograniczenia");
            removeConstraintItem.Click += OnRemoveConstraintClicked;
            _contextMenuEdge.Items.Add(removeConstraintItem);

            // --- MENU DLA WIERZCHOŁKA ---
            _contextMenuVertex = new ContextMenuStrip();
            _contextMenuVertex.Items.Add("Usuń wierzchołek", null, OnRemoveVertexClicked!);

            // --- PODMENU: Klasy ciągłości ---
            var continuityMenu = new ToolStripMenuItem("Klasa ciągłości");

            var g0Item = new ToolStripMenuItem("G0") { CheckOnClick = true };
            g0Item.Click += (s, e) => OnContinuityClassClicked(ContinuityClass.G0);

            var g1Item = new ToolStripMenuItem("G1") { CheckOnClick = true };
            g1Item.Click += (s, e) => OnContinuityClassClicked(ContinuityClass.G1);

            var c1Item = new ToolStripMenuItem("C1") { CheckOnClick = true };
            c1Item.Click += (s, e) => OnContinuityClassClicked(ContinuityClass.C1);

            continuityMenu.DropDownItems.AddRange(new ToolStripItem[] { g0Item, g1Item, c1Item });
            _contextMenuVertex.Items.Add(continuityMenu);

            // Aktualizacja zaznaczenia przy otwieraniu menu wierzchołka
            _contextMenuVertex.Opening += OnVertexContextMenuOpening;
        }

        private void OnRemoveConstraintClicked(object? sender, EventArgs e)
        {
            if (_selectedPolygon == null || _selectedEdgeIndex < 0)
                return;

            _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new NoConstraint());
            OnSceneChanged(_scene);
        }

        private void OnContinuityClassClicked(ContinuityClass selectedClass)
        {
            if (_selectedPolygon == null || _selectedVertexIndex < 0)
                return;

            var vertex = _selectedPolygon.Vertices[_selectedVertexIndex];
            bool flag;
            String msg;
            (flag, msg) = _selectedPolygon.ValidateContinuityChange(_selectedVertexIndex, selectedClass);
            if (!flag)
            {
                ShowErrorMessage(msg);
                return;
            }
            vertex.ContinuityType = selectedClass;
            switch(selectedClass)
            {
                case ContinuityClass.G0:
                    vertex.continuity = new G0();
                    _view.Invalidate();
                    return;
                case ContinuityClass.G1:
                    var g1 = new G1();
                    g1.Initialize(vertex);
                    vertex.continuity = g1;
                    break;
                case ContinuityClass.C1:
                    vertex.continuity = new C1();
                    break;
            }

            InitContinuityBehavior(_selectedPolygon, vertex);
            _view.Invalidate();
        }

        private void InitContinuityBehavior(Polygon polygon, Vertex vertex)
        {
            var vertexIndex = polygon.Vertices.IndexOf(vertex);

            // CCW
            int dir = 1;
            var bezier = polygon.Edges[vertexIndex];
            var prevEdge = polygon.Edges[mod(vertexIndex - 1, polygon.M)];
            PointF prevPos;
            if (prevEdge.Type == SegmentType.BezierCubic)
            {
                prevPos = prevEdge.Cp2;
            }
            else
            {
                prevPos = polygon.Vertices[prevEdge.Start].Position;
            }
            vertex.continuity.Apply(vertex, prevPos, prevEdge, bezier, dir);

            // CW
            dir = -1;
            bezier = polygon.Edges[mod(vertexIndex - 1, polygon.M)];
            prevEdge = polygon.Edges[vertexIndex];
            if (prevEdge.Type == SegmentType.BezierCubic)
            {
                prevPos = prevEdge.Cp1;
            }
            else
            {
                prevPos = polygon.Vertices[prevEdge.End].Position;
            }
            vertex.continuity.Apply(vertex, prevPos, prevEdge, bezier, dir);
        }
        private void OnVertexContextMenuOpening(object? sender, CancelEventArgs e)
        {
            if (_selectedPolygon == null || _selectedVertexIndex < 0)
                return;

            var vertex = _selectedPolygon.Vertices[_selectedVertexIndex];

            var continuityMenu = _contextMenuVertex.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text == "Klasa ciągłości");

            if (continuityMenu == null) return;

            foreach (ToolStripMenuItem item in continuityMenu.DropDownItems)
            {
                switch (item.Text)
                {
                    case "G0": item.Checked = vertex.ContinuityType == ContinuityClass.G0; break;
                    case "G1": item.Checked = vertex.ContinuityType == ContinuityClass.G1; break;
                    case "C1": item.Checked = vertex.ContinuityType == ContinuityClass.C1; break;
                }
            }
        }


        private void OnEdgeContextMenuOpening(object sender, CancelEventArgs e)
        {
            var edge = _selectedPolygon.Edges[_selectedEdgeIndex];
            if (edge == null) return;

            // znajdź menu "Dodaj ograniczenie"
            var constraintMenu = (ToolStripMenuItem)_contextMenuEdge.Items
                .Cast<ToolStripItem>()
                .FirstOrDefault(i => i.Text == "Dodaj ograniczenie")!;
            var bezierItem = _contextMenuEdge.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text == "Zmień na krzywą Beziera");
            if (bezierItem != null)
            {
                bezierItem.Checked = edge.Type == SegmentType.BezierCubic;
            }
            var arcItem = _contextMenuEdge.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text == "Zmień na łuk okręgu");
            if (arcItem != null)
            {
                arcItem.Checked = edge.Type == SegmentType.Arc;
            }
            if (constraintMenu == null) return;

            // zaktualizuj stan zaznaczeń
            foreach (ToolStripMenuItem item in constraintMenu.DropDownItems)
            {
                switch (item.Text)
                {
                    case "Pionowa":
                        item.Checked = edge.Constraint is VerticalConstraint;
                        break;
                    case "45°":
                        item.Checked = edge.Constraint is AngleConstraint45;
                        break;
                    case "Długość":
                        item.Checked = edge.Constraint is FixedLengthConstraint;
                        break;
                }
            }
        }

        private void OnArcSegmentToggleClicked(object? sender, EventArgs e)
        {
            if (_selectedPolygon == null || _selectedEdgeIndex < 0)
                return;
            var edge = _selectedPolygon.Edges[_selectedEdgeIndex];
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            if (menuItem.Checked)
            {
                edge.Type = SegmentType.Arc;
                edge.Constraint = new NoConstraint();
                _selectedPolygon.ComputeArcCenter(edge);
                CheckArcSegmentInit(_selectedPolygon, edge);
            }
            else
            {
                edge.Type = SegmentType.Line;
                edge.Center = PointF.Empty;
            }

            // odśwież widok
            _view.Invalidate();
        }

        private void CheckArcSegmentInit(Polygon polygon, Edge edge)
        {
            var v1 = polygon.Vertices[edge.Start];
            var v2 = polygon.Vertices[edge.End];

            if (v1.ContinuityType == ContinuityClass.G1 && v2.ContinuityType == ContinuityClass.G1)
            {
                v2.continuity = new G0();
                v2.ContinuityType = ContinuityClass.G0;
            }
            if (v1.ContinuityType == ContinuityClass.C1)
            {
                v1.continuity = new G0();
                v1.ContinuityType = ContinuityClass.G0;
            }
            if (v2.ContinuityType == ContinuityClass.C1)
            {
                v2.continuity = new G0();
                v2.ContinuityType = ContinuityClass.G0;
            }
        }
        private void OnBezierSegmentToggleClicked(object? sender, EventArgs e)
        {
            if (_selectedPolygon == null || _selectedEdgeIndex < 0)
                return;

            var edge = _selectedPolygon.Edges[_selectedEdgeIndex];
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            if (menuItem.Checked)
            {
                edge.Type = SegmentType.BezierCubic;
                edge.Constraint = new NoConstraint();

                var start = _selectedPolygon.Vertices[edge.Start].Position;
                var end = _selectedPolygon.Vertices[edge.End].Position;
                float dx = end.X - start.X;
                float dy = end.Y - start.Y;
                float len = MathF.Sqrt(dx * dx + dy * dy);
                if (len < 1e-5f) 
                    len = 1f;

                float nx = -dy / len;
                float ny = dx / len;
                float offset = 60f;

                edge.Cp1 = new PointF(
                    start.X + dx / 3f + nx * offset,
                    start.Y + dy / 3f + ny * offset
                );
                edge.Cp2 = new PointF(
                    start.X + 2f * dx / 3f + nx * offset,
                    start.Y + 2f * dy / 3f + ny * offset
                );

                var v1 = _selectedPolygon.Vertices[edge.Start];
                var v2 = _selectedPolygon.Vertices[edge.End];
                switch (_defualtForBezier)
                {
                    case ContinuityClass.G0:
                        v1.continuity = new G0();
                        v2.continuity = new G0();
                        v1.ContinuityType = ContinuityClass.G0;
                        v2.ContinuityType = ContinuityClass.G0;
                        break; 
                    case ContinuityClass.G1:
                        v1.continuity = new G1();
                        v2.continuity = new G1();
                        v1.ContinuityType = ContinuityClass.G1;
                        v2.ContinuityType = ContinuityClass.G1;
                        break;
                    case ContinuityClass.C1:
                        v1.continuity = new C1();
                        v2.continuity = new C1();
                        v1.ContinuityType = ContinuityClass.C1;
                        v2.ContinuityType = ContinuityClass.C1;
                        break;
                }
                InitContinuityBehavior(_selectedPolygon, v1);
                InitContinuityBehavior(_selectedPolygon, v2);
            }
            else
            {
                edge.Type = SegmentType.Line;
                edge.Cp1 = PointF.Empty;
                edge.Cp2 = PointF.Empty;
            }

            // odśwież widok
            _view.Invalidate();
        }
        private void OnAngle45ConstraintClicked(object? sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender!;
            if (item.Checked)
            {
                if (_selectedPolygon == null || _selectedEdgeIndex == -1)
                    return;
                string? msg;
                bool flag;
                (flag, msg) = _selectedPolygon.Solver.Validate(_selectedPolygon, _selectedEdgeIndex, ConstraintType.Angle45);
                if (false == flag)
                {
                    ShowErrorMessage(msg!);
                    return;
                }

                _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new AngleConstraint45());
                var start = _selectedPolygon.Vertices[_selectedEdgeIndex];
                var end = _selectedPolygon.Vertices[(_selectedEdgeIndex + 1) % _selectedPolygon.Vertices.Count];
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

                var offset = new PointF(-orthogonal.X, -orthogonal.Y);
                _selectedPolygon.ApplyMove(end, offset);
            }
            else
            {
                _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new NoConstraint());
            }
            _view.Invalidate();
        }

        private void OnFixedLengthConstraintClicked(object? sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender!;
            if (item.Checked)
            {
                if (_selectedPolygon == null || _selectedEdgeIndex == -1)
                    return;

                var flag = _selectedPolygon.ValidateConstraintChange(_selectedEdgeIndex, ConstraintType.FixedLength);
                if (!flag)
                {
                    ShowErrorMessage("Nieosiągalna operacja: dwie sąsiadujące krawędzie z ustawioną długością.");
                    item.Checked = false;
                    return;
                }

                var currentLength = _selectedPolygon.GetEdgeLength(_selectedEdgeIndex);
                using (var dialog = new LengthInputDialog(currentLength))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var newLength = dialog.LengthValue;
                        string? msg;
                        (flag, msg) = _selectedPolygon.Solver.Validate(_selectedPolygon, _selectedEdgeIndex, ConstraintType.FixedLength, newLength);
                        if (false == flag)
                        {
                            ShowErrorMessage(msg!);
                            return;
                        }

                        var a = _selectedPolygon.Vertices[_selectedEdgeIndex];
                        var b = _selectedPolygon.Vertices[mod(_selectedEdgeIndex + 1, _selectedPolygon.N)];
                        var dx = b.Position.X - a.Position.X;
                        var dy = b.Position.Y - a.Position.Y;
                        var scale = newLength / currentLength;
                        var offset = new PointF(dx * scale, dy * scale);

                        _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new FixedLengthConstraint(newLength));
                        _selectedPolygon.Edges[_selectedEdgeIndex].FixedLength = newLength;
                        _selectedPolygon.ApplyMove(_selectedPolygon.Vertices[mod(_selectedEdgeIndex + 1, _selectedPolygon.N)], offset);
                    }
                    else
                    {
                        item.Checked = false;
                    }
                }
            }
            else
            {
                // Usunięcie constraintu
                _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new NoConstraint());
                _selectedPolygon.Edges[_selectedEdgeIndex].FixedLength = 0;
            }
            _view.Invalidate();
        }

        private void OnVerticalConstraintClicked(object? sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender!;

            if (item.Checked) 
            {
                if (_selectedPolygon == null || _selectedEdgeIndex == -1)
                    return;
                var flag = _selectedPolygon.ValidateConstraintChange(_selectedEdgeIndex, ConstraintType.Vertical);
                if (!flag)
                {
                    ShowErrorMessage("Nieosiągalna operacja: dwie sąsiadujące pionowe krawędzie.");
                    return;
                }
                else
                {
                    string? msg;
                    (flag, msg) = _selectedPolygon.Solver.Validate(_selectedPolygon, _selectedEdgeIndex, ConstraintType.Vertical);
                    if (false == flag)
                    {
                        ShowErrorMessage(msg!);
                        return;
                    }

                    _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new VerticalConstraint());
                    var start = _selectedPolygon.Vertices[_selectedEdgeIndex];
                    var end = _selectedPolygon.Vertices[(_selectedEdgeIndex + 1) % _selectedPolygon.Vertices.Count];
                    end.Position = new PointF(start.Position.X, end.Position.Y);
                    var offset = new PointF(end.Position.X - start.Position.X, 0);
                    _selectedPolygon.ApplyMove(end, offset);
                }
            }
            else
            {
                _selectedPolygon.SetConstraintOnEdge(_selectedEdgeIndex, new NoConstraint());
            }
            _view.Invalidate();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                // Sprawdzenie, czy kliknięto w jakiś wierzchołek
                foreach (var polygon in _scene.Polygons)
                {
                    foreach (var edge in polygon.Edges)
                    {
                        if (edge.Type == SegmentType.BezierCubic)
                        {
                            if (edge.Cp1.DistanceTo( e.Location) < 8)
                            {
                                _selectedPolygon = polygon;
                                _draggedControlPoint = (edge, 1);
                                _dragOffset = new PointF(edge.Cp1.X - e.X, edge.Cp1.Y - e.Y);
                                return;
                            }
                            if (edge.Cp2.DistanceTo(e.Location) < 8)
                            {
                                _selectedPolygon = polygon;
                                _draggedControlPoint = (edge, 2);
                                _dragOffset = new PointF(edge.Cp2.X - e.X, edge.Cp2.Y - e.Y);
                                return;
                            }
                        }
                    }
                    foreach (var v in polygon.Vertices)
                    {
                        if (v.Position.DistanceTo(e.Location) < 10)
                        {
                            _selectedPolygon = polygon;
                            _draggedVertex = v;
                            _dragOffset = new PointF(v.Position.X - e.X, v.Position.Y - e.Y);
                            return;
                        }
                    }
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                _selectedPolygon = null;
                _selectedEdgeIndex = -1;

                foreach (var polygon in _scene.Polygons)
                {
                    for (int i = 0; i < polygon.Vertices.Count; i++)
                    {
                        var v = polygon.Vertices[i].Position;
                        if (v.DistanceTo(e.Location) < 10)
                        {
                            _selectedPolygon = polygon;
                            _selectedVertexIndex = i;
                            _contextMenuVertex.Show(_view, e.Location);
                            return;
                        }
                    }

                    for (int i = 0; i < polygon.Vertices.Count; i++)
                    {
                        var v1 = polygon.Vertices[i].Position;
                        var v2 = polygon.Vertices[(i + 1) % polygon.Vertices.Count].Position;

                        float dist = DistancePointToSegment(e.Location, v1, v2);
                        if (dist < 8) // jeśli kliknięcie blisko krawędzi
                        {
                            _selectedPolygon = polygon;
                            _selectedEdgeIndex = i;
                            _contextMenuEdge.Show(_view, e.Location);
                            return;
                        }
                    }
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedPolygon == null)
                return;
            if (_draggedControlPoint.HasValue && e.Button == MouseButtons.Left)
            {
                var (edge, index) = _draggedControlPoint.Value;
                if (!_draggedControlPoint.HasValue)
                    return;
                var newPos = new PointF(e.X + _dragOffset.X, e.Y + _dragOffset.Y);
                var posBefore = _selectedPolygon.GetCP(_draggedControlPoint.Value.edge,
                                                    _draggedControlPoint.Value.index);
                if (posBefore.IsEmpty)
                    throw new Exception();
                var offset = new PointF(newPos.X - posBefore.X, newPos.Y - posBefore.Y);
                if (index == 1)
                    edge.Cp1 = newPos;
                else
                    edge.Cp2 = newPos;
                _selectedPolygon.ApplyMoveCP(_draggedControlPoint.Value, offset, newPos);
                _view.Invalidate();
                return;
            }
            if (_draggedVertex != null && e.Button == MouseButtons.Left)
            {
                var posBefore = _draggedVertex.Position;
                _draggedVertex.Position = new PointF(e.X + _dragOffset.X, e.Y + _dragOffset.Y);
                var offset = new PointF(_draggedVertex.Position.X - posBefore.X, _draggedVertex.Position.Y - posBefore.Y);
                _selectedPolygon.ApplyMove(_draggedVertex, offset);
                _view.Invalidate(); // odśwież widok
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _draggedVertex = null;
            _draggedControlPoint = null;
        }

        private void OnAddVertexClicked(object sender, EventArgs e)
        {
            if (_selectedPolygon == null || _selectedEdgeIndex == -1)
                return;
            _selectedPolygon.InsertVertexAtEdge(_selectedEdgeIndex);

            _view.Invalidate();
        }

        private void OnRemoveVertexClicked(object sender, EventArgs e)
        {
            if (_selectedPolygon == null || _selectedVertexIndex == -1)
                return;

            if (_selectedPolygon.Vertices.Count <= 3)
            {
                ShowErrorMessage("Nieprawidłowa operacja: liczba wierzchołków jest mniejsza niż 3!");
                return;
            }

            _selectedPolygon.RemoveVertex(_selectedVertexIndex);
            _selectedVertexIndex = -1;
            _selectedPolygon = null;

            _view.Invalidate();
        }

        public void CheckedRadioButton(IEdgeDrawer pickedStrategy)
        {
            _view.SetDrawingStrategy(pickedStrategy);
            _view.Invalidate();
        }

        public void OnResetBttnClicked()
        {
            _scene.Clear();
            var polygon = new Polygon(new[]
            {
                new Vertex(200, 100),
                new Vertex(200, 350),
                new Vertex(350, 300),
                new Vertex(450, 150),
                new Vertex(350, 100)
            });
            polygon.SetConstraintOnEdge(0, new VerticalConstraint());
            _selectedPolygon = polygon;
            _selectedEdgeIndex = 2;
            var fakeMenuItem = new ToolStripMenuItem() { Checked = true };
            OnBezierSegmentToggleClicked(fakeMenuItem, EventArgs.Empty);
            polygon.SetConstraintOnEdge(3, new FixedLengthConstraint(150));
            polygon.Edges[3].FixedLength = 150;
            polygon.ApplyMove(polygon.Vertices[3], new PointF(1, 1));
            _scene.AddPolygon(polygon);
            _view.Invalidate();
        }

        public void ShowManual()
        {
            MessageBox.Show(
                "Sterowanie intuicyjne:\n\n" +
                "-> Przesuwanie wierzchołków i punktów kontrolnych Beziera za pomocą LPM.\n\n" +
                "-> Wszystkie inne interakcje związane ze zmianą zachowania wierzchołka/krawędzi" +
                " są dostępne po najechaniu na dany element i wciśnięciu PPM.\n" +
                " To otwiera odpowiednie menu kontekstowe ze wszystkimi dostępnymi opcjami edycji.\n\n" +
                "-> Otwarcie menu kontekstowego krawędzi będącej krzywą Beziera lub łukiem okręgu" +
                " polega na kliknięciu PPM na przerywaną linię między wierzchołkami tej krawędzi.\n\n" +
                "-> Dostępne opcje edycji (działanie zgodne z intuicją):\n" +
                "   - Dodaj wierzchołek (w połowie długości krawędzi; usuwa wszystkie ograniczenia)\n" +
                "   - Usuń wierzchołek\n" +
                "   - Ustaw klasę ciągłości\n" +
                "   - Dodaj ograniczenie\n" +
                "   - Zmień na łuk okręgu\n" +
                "   - Zmień na krzywą Beziera\n" +
                "   - Usuń ograniczenia\n\n" +
                "-> Nad sceną znajduje się panel z dwoma radiobuttonami i przyciskami, których zachowanie jest intuicyjne" +
                " tzn:\n" +
                "   - radiobuttony zmieniają algorytm rysowania (dostępne dwa: biblioteczny i Bresenhama)\n" +
                "   - przyciski służą do wyświetlania pomocy (sterowanie i opis działania programu)\n" +
                "   - przycisk 'Reset' resetuje scenę, czyli usuwa wszystkie obecne na niej wielokąty, i z powrotem" +
                " wyświetla początkowy wielokąt.",
                "Opis sterowania",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        public void ShowAppDescription()
        {
            MessageBox.Show(
                "-> Główny zamysł architektury projektu:\n" +
                "   - Wykorzystanie modelu MVC do podziału separacji różnych aspektów aplikacji tj. " +
                "modelu danych (wielokąt, krawędź itp.), rysowanie na ekranie, zarządzanie logiką i interakcją" +
                " z użytkownikiem.\n" +
                "   - Wykorzystanie wzorców projektowych do modularyzacji kodu w przypadku rozszerzenia " +
                "funkcjonalności programu o kolejne ograniczenia krawędzi, klasy ciągłości itp.\n\n" + 
                "-> Opis działania ograniczeń i ich egzekwowania:\n" +
                "   - ograniczenia są implementowane jako obiekty klas implementujące interfejs IConstraint, gdzie" +
                " każda klasa definuje strategię (wzorzec projektowy Strategia) naprawy pozycji wierzchołków," +
                " aby zachować dane ograniczenie.\n" +
                "   - przy wykryciu ruchu wierzchołka, SceneController powiadamia dany wielokąt ze sceny o potrzebie" +
                " zmiany jego struktury (tj. pozycji wierzchołków), który następnie wykorzystuje obiekt klasy ConstraintSolver" +
                " do poprawnego ruchu wielokąta, tak aby zachować wprowadzone ograniczenia.\n" +
                "   - ConstraintSolver propaguje zmiany w obie strony od poruszonego wierzchołka, wywołując przy każdym" +
                " przejściu na krawędzi przypisane jej ograniczenie, aby poprawić strukturę.\n" +
                "   - przed wprowadzeniem danego ograniczenia jest również wykonywana przez ConstraintSolver walidacja" +
                " poprawności danego ograniczenia w obecnej strkutrze, polegająca na propagacji zmian na kopii wielokąta" +
                " i sprawdzenie czy strukutra mieści się w dopuszczalnych normach.\n" +
                "   - zastosowane algorytmy poprawy globalnej strkutry wielokąta po ruchu jednego z jego wierzchołków " +
                "są algorytmami heurystycznymi i dają przybliżone rozwiązania, mieszczące się" +
                " w ustalonej tolerancji " +
                "(jest to kompromis między poprawności algorytmów a stopniem ich skomplikowania).\n\n" +
                "-> Opis działania ciągłości wierzchołków:\n" +
                "   - analogiczny sposób działania do poprawy ograniczeń, tylko że logika egzekwowana jest przez" +
                " obiekt klasy ContinuitiesSolver (tutaj również zastosowano różne strategie do różnych klas ciągłości).\n\n" +
                "-> Rysowanie:\n" +
                "   - odbywa się za pomocą SceneView, które odpowiednio interpretuje dane modelu. Wywołanie rysowania " +
                "jest wykonywane przez SceneControllera, który przy zmianie w strukutrze modelu, wywołuje odpowiednie funkcje.\n" +
                "   - różne algorytmy rysowania zostały zaimplementowane jako odpowiednie strategie rysowania, mające" +
                " wspólny interfejs IEdgeDrawer. SceneController zmienia/ustawia wybraną strategię SceneView, który następnie" +
                " wywołuje funkcje rysowania na ekranie owej wybranej strategii.",
                "Opis działania programu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ShowErrorMessage(string message) => MessageBox.Show(message,
                                                                         "Błąd",
                                                                         MessageBoxButtons.OK,
                                                                         MessageBoxIcon.Error);
     

        private float DistancePointToSegment(PointF p, PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            if (dx == 0 && dy == 0)
                return p.DistanceTo(a);

            float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            float projX = a.X + t * dx;
            float projY = a.Y + t * dy;
            return p.DistanceTo(new PointF(projX, projY));
        }
        public int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
