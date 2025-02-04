using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CanvasAppExtractorApp
{
    public static class Program
    {

        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var serviceCollection = new ServiceCollection();
            var success = false;
            try
            {
                ConfigureServices(serviceCollection);
                success = true;
            }
            catch(InvalidDataException ex)
            {
                if(ex.InnerException?.InnerException is JsonException jsException)
                {
                    ShowError(ex.Message + " " + jsException.Message);
                }
                else
                {
                    ShowError(ex.ToString());
                }
            }
            catch(Exception ex)
            {
                ShowError(ex.ToString());
            }

            if (success)
            {
                ServiceProvider = serviceCollection.BuildServiceProvider();
                Application.Run(ServiceProvider.GetRequiredService<Form1>());
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, @"Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            serviceCollection.Configure<CanvasAppExtractorSettings>(builder.GetSection(nameof(CanvasAppExtractorSettings)));
            serviceCollection
                .AddTransient<FileSystemWatcher>()
                .AddTransient<Form1>()
                .AddSingleton(s => s.GetRequiredService<IOptions<CanvasAppExtractorSettings>>().Value);
        }
    }
}