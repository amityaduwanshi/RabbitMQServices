using System.Runtime.CompilerServices;
using Services.Processors;

namespace Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var rmqConfiguraiton = _configuration.GetSection("Configuration:Queues:RabbitMQ");
                if (rmqConfiguraiton != null)
                {
                    BootStrapRabbitMQAsync(rmqConfiguraiton);
                }
                BootStrapFileSystemMonitorAsync();
                await Task.Delay(1000000, stoppingToken);
            }
        }

        private async void BootStrapRabbitMQAsync(IConfigurationSection configuration)
        {
            await Task.Run(() =>
            {
                RabbitMQProcessor.Bootstrap(configuration);
                RabbitMQProcessor.StartConsumer();
            });
        }

        private async void BootStrapFileSystemMonitorAsync()
        {
            await Task.Run(() =>
            {
                FileSystemProcessor.Bootstrap(_configuration);
                FileSystemProcessor.ActivateMonitor();
            });
        }

    }
}