namespace CanvasAppExtractorApp
{
    public partial class Form1 : Form
    {
        private readonly CanvasAppExtractorSettings _settings;
        private readonly FileSystemWatcher _fileWatcher;
        private bool _allowClose;
        private bool _allowVisible = true;

        public Form1() : this(null, null)
        {
        }

        public Form1(CanvasAppExtractorSettings? settings, FileSystemWatcher? fileWatcher)
        {
            Opacity = 0.01;
            InitializeComponent();
            _settings = settings ?? new();
            _fileWatcher = fileWatcher ?? new();
            ShowInTaskbar = false;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(_allowVisible && value);
        }

        private void CanvasAppExtractorNotifier_BalloonTipClicked(object sender, EventArgs e)
        {
            ShowApp();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            Close();
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowApp();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowClose)
            {
                return;
            }

            e.Cancel = true;
            HideApp();
        }

        private void ShowApp()
        {
            _allowVisible = true;
            Visible = true;
        }

        private void HideApp()
        {
            _allowVisible = false;
            Visible = false;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Opacity = 1;
            CanvasAppExtractorNotifier.ShowBalloonTip(1000, "CanvasApp Extractor", "Canvas App Extractor is running!", ToolTipIcon.Info);
            HideApp();
            var service = new WindowsBackgroundService(_fileWatcher, _settings, new FormTextBoxLogger<WindowsBackgroundService>(textBox1), CanvasAppExtractorNotifier);
        }

        private void CanvasAppExtractorNotifier_MouseClick(object sender, MouseEventArgs e)
        {
            ShowApp();
        }
    }
}
