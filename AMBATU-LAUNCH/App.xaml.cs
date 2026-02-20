using Microsoft.UI.Xaml;

namespace AMBATU_LAUNCH
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try 
            {
                m_window = new MainWindow();
                MainWindow = m_window;
                m_window.Activate();
            }
            catch (System.Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), ex.ToString());
                throw;
            }
        }

        private Window m_window;
    }
}
