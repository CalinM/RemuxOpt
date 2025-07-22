namespace RemuxOpt
{
    public partial class FrmAddLanguage : Form
    {
        private ucLanguageSelector _ucLanguageSelector;
        public LanguageObject SelectedLanguage { get; private set; }

        public FrmAddLanguage(List<string> selectedCodes, string infoText)
        {
            InitializeComponent();

            _ucLanguageSelector = new ucLanguageSelector(selectedCodes)
            {
                Location = new Point(12, 58)
            };

            _ucLanguageSelector.EnterPressed += (s, e) => btnOk.PerformClick();

            Controls.Add(_ucLanguageSelector);

            lbInfo.Text = infoText;
            btnOk.Click += BtnOk_Click;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_ucLanguageSelector.SelectedLanguage == null)
            {
                MsgBox.Show(this, "Invalid language selection!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedLanguage = _ucLanguageSelector.SelectedLanguage;
            DialogResult = DialogResult.OK;
        }
    }
}