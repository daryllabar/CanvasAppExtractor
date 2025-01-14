using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            Application.Run(ServiceProvider.GetRequiredService<Form1>());
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