using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonEditor.Model
{
    public class LengthInputDialog : Form
    {
        private readonly NumericUpDown _numericUpDown;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

        public float LengthValue => (float)_numericUpDown.Value;

        public LengthInputDialog(float currentLength)
        {
            Text = "Ustaw długość krawędzi";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            Width = 250;
            Height = 150;

            Label label = new Label
            {
                Text = "Długość:",
                AutoSize = true,
                Left = 20,
                Top = 20
            };
            Controls.Add(label);

            _numericUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = (decimal)currentLength,
                Left = 90,
                Top = 18,
                Width = 100
            };
            Controls.Add(_numericUpDown);

            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 40,
                Top = 60,
                Width = 70
            };
            Controls.Add(_okButton);

            _cancelButton = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Left = 120,
                Top = 60,
                Width = 70
            };
            Controls.Add(_cancelButton);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }
    }
}
