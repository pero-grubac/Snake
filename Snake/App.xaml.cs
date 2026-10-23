using System.IO;
using System.Windows;

namespace Snake
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Started with <c>--demo</c>: every snake steers itself until a player presses a direction, and scores are not saved.</summary>
        public static bool DemoMode { get; private set; }

        /// <summary>Where settings and high scores live; <c>--data-dir &lt;path&gt;</c> overrides it.</summary>
        public static string DataDirectory { get; private set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Blavor");

        protected override void OnStartup(StartupEventArgs e)
        {
            for (int i = 0; i < e.Args.Length; i++)
            {
                if (string.Equals(e.Args[i], "--demo", StringComparison.OrdinalIgnoreCase))
                {
                    DemoMode = true;
                }
                else if (string.Equals(e.Args[i], "--data-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < e.Args.Length)
                {
                    DataDirectory = Path.GetFullPath(e.Args[++i]);
                }
            }
            base.OnStartup(e);
        }
    }
}
