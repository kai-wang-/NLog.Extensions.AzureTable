using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Sample
{
    class Program
    {
        private static IConfiguration Configuration;
        public static async Task Main(string[] args) {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices)
                .Build();
            await host.RunAsync();
        }
        static void ConfigureServices(HostBuilderContext context, IServiceCollection services) {
            Configuration = context.Configuration;
            services
                .AddLogging(ConfigLogging)
                .Configure<HostOptions>(option => option.ShutdownTimeout = System.TimeSpan.FromSeconds(20))
                .Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true)
                .AddHostedService<SampleService>();
        }
        private static void ConfigLogging(ILoggingBuilder builder) {
            LogManager.Configuration = new NLogLoggingConfiguration(Configuration.GetSection("NLog"));
            builder.ClearProviders()
                .AddConfiguration(Configuration.GetSection("Logging"))
                .AddNLog(Configuration);            
        }
    }

    public class SampleService : BackgroundService
    {    
        ILogger<SampleService> _logger;
        EventId _eventId = new EventId(1, "EventIdHelloWorld");
        private readonly IHostApplicationLifetime _appLifetime;
        public SampleService (
            ILogger<SampleService> logger,
            IHostApplicationLifetime appLifetime
            ){
            _logger = logger;
            _appLifetime = appLifetime;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(_eventId, "Hello world!");
            await Task.CompletedTask;
            _appLifetime.StopApplication();            
        }
    }
}
 