using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderBookCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace OrderBookWorker
{
    public class Program
    {
        private static OrderRepository? _orderRepository;
        private static ConsumerQueueService? _queueService;
        private static OrderBook _orderBook;

        private const string YugabyteDefaultConnStr =
            "Host=localhost;Port=5433;Username=yugabyte;Password=yugabyte;Database=yugabyte";
        
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();
                
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    // Add other configuration sources if needed (e.g., environment variables)
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register your background service
                    services.Configure<RabbitMQOptions>(hostContext.Configuration.GetSection(RabbitMQOptions.SectionName));
                    services.AddSingleton<ConsumerQueueService>();
                    services.AddScoped<OrderRepository>((_)=> 
                        new OrderRepository(hostContext.Configuration.GetConnectionString("YugabyteDbConnection")));
                    services.AddHostedService<OrderBookService>();
                    
                });
    }
}

