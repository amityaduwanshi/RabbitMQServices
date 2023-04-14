namespace Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        
        public Worker(ILogger<Worker> logger,IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                RabbitMQProcessor.Bootstrap(_configuration);
                RabbitMQProcessor.StartConsumer();
                RabbitMQProcessor.StartPublisher();
                await Task.Delay(1000000, stoppingToken);
            }
        }

            
    }
}