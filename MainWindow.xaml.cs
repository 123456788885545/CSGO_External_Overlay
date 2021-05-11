using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace CSGO_External_Overlay
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Overlay overlay;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Main_Closing(object sender, CancelEventArgs e)
        {
            if (overlay != null)
            {
                overlay.Dispose();
            }
        }

        private void Button_Overaly_Run_Click(object sender, RoutedEventArgs e)
        {
            if (overlay == null)
            {
                Task t = new Task(() =>
                {
                    GameOverlay.TimerService.EnableHighPrecisionTimers();

                    if (Process.GetProcessesByName("csgo").ToList().Count > 0)
                    {
                        overlay = new Overlay();
                        overlay.Run();
                    }
                    else
                    {
                        MessageBox.Show("未发现CSGO进程", " 错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

                t.Start();
            }
            else
            {
                MessageBox.Show("请勿重复启动", " 警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.OriginalString);
            e.Handled = true;
        }
    }
}
