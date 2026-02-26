using Serilog;
using System.Windows;

namespace ScrabbleUIWPF
{
    public partial class App : Application
    {
        public App()
        {
            Serilog.Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        }
    }
}
