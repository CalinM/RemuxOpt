namespace RemuxOpt
{
    public partial class FrmOptions : Form
    {
        private AppOptions _appOptions;

        public FrmOptions(AppOptions appOptions)
        {
            InitializeComponent();
            _appOptions = appOptions;

            this.Load += FrmOptions_Load;

            

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
        }

        private void BSave_Click(object? sender, EventArgs e)
        {
            _appOptions.ReadFilesRecursively = chkReadFilesRecursively.Checked;
            _appOptions.DeleteOriginalsAfterSuccessfulRemux = chkDeleteOriginal.Checked;

            DialogResult = DialogResult.OK;
        }
    }
}
