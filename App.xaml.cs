using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CSGO_External_Overlay
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        static Mutex mutex;
        bool createNew;

        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new Mutex(true, ResourceAssembly.GetName().Name, out createNew);

            if (createNew)
            {
                base.OnStartup(e);

                // UI线程未捕获异常处理事件（UI主线程）
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                // 非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                // Task线程内未捕获异常处理事件
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            }
            else
            {
                MessageBox.Show("请不要重复打开，程序已经运行\n如果一直提示，请到\"任务管理器-详细信息（win7为进程）\"里结束本程序",
                    " 警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                Current.Shutdown();
            }
        }

        // UI线程未捕获异常处理事件（UI主线程）
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            string msg = string.Format($"{ex.Message}\n\n{ ex.StackTrace}");    // 异常信息 和 调用堆栈信息
            MessageBox.Show(msg, " UI线程异常");
            MyUtil.SaveAppLogFile("ErrorLog", msg);
        }

        // 非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
        // 如果UI线程异常DispatcherUnhandledException未注册，则如果发生了UI线程未处理异常也会触发此异常事件
        // 此机制的异常捕获后应用程序会直接终止。没有像DispatcherUnhandledException事件中的Handler=true的处理方式，可以通过比如Dispatcher.Invoke将子线程异常丢在UI主线程异常处理机制中处理
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                string msg = string.Format($"{ex.Message}\n\n{ ex.StackTrace}");    // 异常信息 和 调用堆栈信息
                MessageBox.Show(msg, " 非UI线程异常");
                MyUtil.SaveAppLogFile("ErrorLog", msg);
            }
        }

        // Task线程内未捕获异常处理事件
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            string msg = string.Format($"{ex.Message}\n\n{ ex.StackTrace}");
            MessageBox.Show(msg, " Task异常");
            MyUtil.SaveAppLogFile("ErrorLog", msg);
        }
    }
}
