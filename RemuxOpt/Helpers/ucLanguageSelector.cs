namespace RemuxOpt
{
    public partial class ucLanguageSelector : UserControl
    {
        public event EventHandler? EnterPressed;

        public LanguageObject? SelectedLanguage
        {
            get
            {
                return cbLanguages.SelectedItem as LanguageObject;
            }
        }

        public ucLanguageSelector()
        {
            InitializeComponent();
        }

        public ucLanguageSelector(List<string> selectedCodes)
        {
            InitializeComponent();
            PopulateComboBox(selectedCodes);
            ConfigureComboBox();

            HandleCreated += (s, e) =>
            {
                BeginInvoke(new Action(() => cbLanguages.Focus()));
            };
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

        private void ConfigureComboBox()
        {
            cbLanguages.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbLanguages.AutoCompleteSource = AutoCompleteSource.ListItems;

            cbLanguages.KeyDown += CbLanguages_KeyDown;
        }

        private void CbLanguages_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                EnterPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}