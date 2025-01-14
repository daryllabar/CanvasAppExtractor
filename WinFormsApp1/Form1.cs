namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CanvasAppExtractorNotifier.Visible = true;
            CanvasAppExtractorNotifier.ShowBalloonTip(1000, "CanvasApp Extractor", "Canvas App Extractor is running!", ToolTipIcon.Info);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        private void CanvasAppExtractorNotifier_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

            }
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
        }

        private void CanvasAppExtractorNotifier_BalloonTipClicked(object sender, EventArgs e)
        {
            this.Visible = false;
        }
    }
}
