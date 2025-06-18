using System.Data;

namespace RemuxOpt
{
    public partial class FrmAddLanguage : Form
    {
        public LanguageObject SelectedLanguage { get; private set; }

        public FrmAddLanguage(List<string> selectedCodes)
        {
            InitializeComponent();

            PopulateComboBox(selectedCodes);

            cbLanguages.AutoCompleteSource = AutoCompleteSource.ListItems;

            cbLanguages.TextChanged += CbLanguages_TextChanged;
            cbLanguages.KeyPress += CbLanguages_KeyPress;
            cbLanguages.MouseClick += CbLanguages_MouseClick;

            btnOk.Click += BtnOk_Click;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            SelectedLanguage = (LanguageObject)cbLanguages.SelectedItem;
        }

        private void CbLanguages_MouseClick(object? sender, MouseEventArgs e)
        {
            cbLanguages.DroppedDown = true;
        }

        private void CbLanguages_TextChanged(object? sender, EventArgs e)
        {
            string typedText = cbLanguages.Text;

            var match = cbLanguages.Items.Cast<LanguageObject>()
                .FirstOrDefault(lang => lang.Name.Equals(typedText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                cbLanguages.SelectedItem = match; // Select the matching object
            }

            btnOk.Enabled = cbLanguages.SelectedIndex != -1;
        }

        private void CbLanguages_KeyPress(object sender, KeyPressEventArgs e)
        {
            cbLanguages.DroppedDown = true;
        }


        private void PopulateComboBox(List<string> providedCodes)
        {
            var filteredLanguages = Languages.Iso639
                .Where(lang => !providedCodes.Contains(lang.Abr3a))
                .ToList();

            cbLanguages.DataSource = filteredLanguages;
            cbLanguages.DisplayMember = "Name";
            cbLanguages.ValueMember = "Abr3a";

            cbLanguages.SelectedIndex = -1; // No selection by default
        }
    }
}
