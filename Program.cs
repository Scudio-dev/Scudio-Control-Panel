using System;
using System.Threading.Tasks;
using Avalonia;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;

namespace ScudioControlPanel
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static async Task Main(string[] args)
        {
            await InitAutoUpdaterAsync();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static async Task InitAutoUpdaterAsync()
        {
            var appCastUrl = "http://localhost:8000/appcast.xml"; 
            var signatureVerifier = new Ed25519Checker(SecurityMode.Unsafe); 
            var sparkle = new SparkleUpdater(appCastUrl, signatureVerifier);

            await sparkle.CheckForUpdatesQuietly();
        }
    }
}
