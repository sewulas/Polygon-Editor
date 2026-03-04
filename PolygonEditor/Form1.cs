using PolygonEditor.Controller;
using PolygonEditor.Model;
using PolygonEditor.Model.Constraints;
using PolygonEditor.View;
using PolygonEditor.View.DrawingStrategies;

namespace PolygonEditor
{
    public partial class Form1 : Form
    {
        private Scene _scene;
        private SceneView _view;
        private SceneController _controller;

        public Form1()
        {
            InitializeComponent();
            InitializeScene();
        }

        private void InitializeScene()
        {
            _scene = new Scene();
            _view = new SceneView
            {
                Dock = DockStyle.Fill,
                Scene = _scene
            };

            _controller = new SceneController(_view, _scene);
            _controller.OnResetBttnClicked();
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };

            var rbLib = new RadioButton { Text = "Algorytm biblioteczny", Checked = true, AutoSize = true };
            var rbBres = new RadioButton { Text = "Algorytm Bresenhama", AutoSize = true };

            rbLib.CheckedChanged += (s, e) =>
            {
                if (rbLib.Checked)
                    _controller.CheckedRadioButton(new LibraryDrawer());
            };

            rbBres.CheckedChanged += (s, e) =>
            {
                if (rbBres.Checked)
                    _controller.CheckedRadioButton(new BresenhamDrawer());
            };

            var btnManual = new Button { Text = "Opis sterowania", AutoSize = true };
            var btnAppDescription = new Button { Text = "Opis dzia³ania programu", AutoSize = true };
            var btnReset = new Button { Text = "Reset", AutoSize = true };

            btnManual.Click += (s, e) => _controller.ShowManual();
            btnAppDescription.Click += (s, e) => _controller.ShowAppDescription();
            btnReset.Click += (s, e) => _controller.OnResetBttnClicked();

            panel.Controls.Add(rbLib);
            panel.Controls.Add(rbBres);
            panel.Controls.Add(btnManual);
            panel.Controls.Add(btnAppDescription);
            panel.Controls.Add(btnReset);

            Controls.Add(_view);
            Controls.Add(panel);
        }
    }
}
