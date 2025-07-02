namespace RemuxOpt
{
    public partial class FrmLanguageTrackCorrection : Form
    {
        public List<MkvFileInfo> SelectedFiles { get; set; } = new List<MkvFileInfo>();
        public string FieldName { get; set; }

        public FrmLanguageTrackCorrection()
        {
            InitializeComponent();
        }

        private void FrmLanguageTrackCorrection_Load(object sender, EventArgs e)
        {
            lbInfo.Text = $"Update the language code on field \"{FieldName}\" for the loaded data ({SelectedFiles.Count} files selected)";
        }
    }
}
