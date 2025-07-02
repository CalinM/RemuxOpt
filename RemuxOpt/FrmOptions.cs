namespace RemuxOpt
{
    public partial class FrmOptions : Form
    {
        private AppOptions _appOptions;

        public FrmOptions(AppOptions appOptions)
        {
            InitializeComponent();
            _appOptions = appOptions;

            Load += FrmOptions_Load;
            bSave.Click += BSave_Click;
        }

        private void FrmOptions_Load(object? sender, EventArgs e)
        {
            SetValuesFromAppOptions();
        }

        private void SetValuesFromAppOptions()
        {
            chkReadFilesRecursively.Checked = _appOptions.ReadFilesRecursively;
            chkDeleteOriginal.Checked = _appOptions.DeleteOriginalsAfterSuccessfulRemux;
            chkRemoveUnlistedLanguageTracks.Checked = _appOptions.RemoveUnlistedLanguageTracks;
            chkApplyNamingConventions.Checked = _appOptions.ApplyNamingConventions;
        }

        private void BSave_Click(object? sender, EventArgs e)
        {
            _appOptions.ReadFilesRecursively = chkReadFilesRecursively.Checked;
            _appOptions.DeleteOriginalsAfterSuccessfulRemux = chkDeleteOriginal.Checked;
            _appOptions.ApplyNamingConventions = chkApplyNamingConventions.Checked;
            _appOptions.RemoveUnlistedLanguageTracks = chkRemoveUnlistedLanguageTracks.Checked;

            DialogResult = DialogResult.OK;
        }
    }
}
