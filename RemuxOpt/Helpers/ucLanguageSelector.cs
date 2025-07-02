using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemuxOpt
{
    public partial class ucLanguageSelector : UserControl
    {
        public LanguageObject? SelectedLanguage
        {
            get
            {
                if (cbLanguages.SelectedItem is LanguageObject selectedLanguage)
                {
                    return selectedLanguage;
                }

                return null;
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

            cbLanguages.AutoCompleteSource = AutoCompleteSource.ListItems;

            cbLanguages.TextChanged += CbLanguages_TextChanged;
            cbLanguages.KeyPress += CbLanguages_KeyPress;
            cbLanguages.MouseClick += CbLanguages_MouseClick;
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
        }

        private void CbLanguages_KeyPress(object sender, KeyPressEventArgs e)
        {
            cbLanguages.DroppedDown = true;
        }
    }
}
