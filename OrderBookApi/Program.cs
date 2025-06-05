using CorrelationId;
using CorrelationId.DependencyInjection;
using OrderBookApi;
using OrderBookCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .MinimumLevel.Debug()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();
    
builder.Host.UseSerilog();

builder.Services.AddDefaultCorrelationId(options =>
{
    options.RequestHeader = "X-Correlation-ID";
    options.ResponseHeader = "X-Correlation-ID";
    options.UpdateTraceIdentifier = true;
});

builder.Services.AddOpenApi();

builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<PublishQueueService>();

builder.Services.AddScoped<OrderRepository>(_ =>
    new OrderRepository(builder.Configuration.GetConnectionString("YugabyteDbConnection")?? string.Empty)
);

builder.Services.AddScoped<OrderBookService>();

var app = builder.Build();

app.UseCorrelationId();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseOrderBookRoutes();

app.Run();

Log.CloseAndFlush();