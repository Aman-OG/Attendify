using System.Windows.Controls;

namespace Attendify.Views
{
    public partial class LoadingOverlay : UserControl
    {
        public LoadingOverlay()
        {
            InitializeComponent();
        }

        public string Message
        {
            get => StatusText.Text;
            set => StatusText.Text = value;
        }
    }
}
