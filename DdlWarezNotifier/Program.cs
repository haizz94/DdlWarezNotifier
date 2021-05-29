using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DdlWarezNotifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "MM/dd/yyyy HH:mm:ss ";
                }));
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("DdlWarezNotifier is trying to start...");

            try
            {
                CreateHostBuilder(args, loggerFactory).Build().Run();
            }
            catch (System.Exception e)
            {
                logger.LogError("DdlWarezNotifier needs to shutdown because of the following exception: {exception}", e.Message);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, ILoggerFactory loggerFactory) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var settings = hostContext.Configuration.GetSection("Settings");
                services.Configure<DdlWarezNotifierSettings>(settings);
                services.AddHostedService<DdlWarezNotifierService>();
                services.AddSingleton(loggerFactory);
            });
    }
}
