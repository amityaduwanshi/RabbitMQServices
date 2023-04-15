using RabbitMQService;
using RabbitMQService.Model;
using Services.Helper;
using System.Text;

namespace Services
{
    public static class RabbitMQProcessor
    {
        private static IList<Consumer> _consumerManagers;
        private static IList<Publisher> _publisherManagers;

        static RabbitMQProcessor()
        {
            _consumerManagers = new List<Consumer>();
            _publisherManagers = new List<Publisher>();
        }

        public static void Bootstrap(IConfiguration configuration)
        {
            var queueManagers = configuration.GetSection("Configuration:Queues:RabbitMQ:QueueManagers");
            var consumers = configuration.GetSection("Configuration:Queues:RabbitMQ:Consumers");
            var publishers = configuration.GetSection("Configuration:Queues:RabbitMQ:Publishers");

            foreach (var consumer in consumers.GetChildren())
            {
                var consumerDic = ConfigHelper.GetDictionary(consumer);
                if (consumerDic == null) continue;
                SetConsumer(consumerDic, queueManagers);
            }

            foreach (var publisher in publishers.GetChildren())
            {
                var publisherDic = ConfigHelper.GetDictionary(publisher);
                if (publisherDic == null) continue;
                SetPublisher(publisherDic, queueManagers);
            }
        }

        public static void StartConsumer()
        {
            foreach (Consumer consumer in _consumerManagers)
            {
                consumer.BindQueue();
                consumer.Consume((obj, eventArgs) =>
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($" [x] Received {message}");
                });
            }
        }

        public static void StartPublisher()
        {
            foreach (Publisher publisher in _publisherManagers)
            {
                publisher.BindQueue();
                Console.WriteLine("Wite Data to Publish:");
                var data = Console.ReadLine();
                var byteData = Encoding.UTF8.GetBytes(data);
                publisher.Publish(byteData);

            }
        }

        private static ClientModel GetQueueManager(IDictionary<string, string> queueManager, string queueName, string exchangeKey, string routingKey)
        {
            return new ClientModel
            {
                HostName = queueManager["HostName"],
                VirtualHostName = queueManager["VirtualHostName"],
                Username = queueManager["Username"],
                Password = queueManager["Password"],
                ClientProvidedName = queueManager["ClientProvidedName"],
                Queue = new QueueModel()
                {
                    QueueName = queueName,
                    ExchangeName = exchangeKey,
                    RoutingKey = routingKey
                }
            };

        }
        private static void SetConsumer(IDictionary<string, string> consumerDic, IConfigurationSection queueManagers)
        {
            var rmqcConsumer = new ConsumerModel
            {
                ConsumerTag = consumerDic["ConsumerTag"],
                AutoAcknowledgment = Convert.ToBoolean(consumerDic["AutoAcknowledgment"]),
                Exclusive = Convert.ToBoolean(consumerDic["Exclusive"]),
                NoLocal = Convert.ToBoolean(consumerDic["NoLocal"]),
            };

            string queueManagerName = consumerDic["QueueManager"];
            var queueManager = ConfigHelper.GetDictionary(queueManagers.GetSection(queueManagerName));
            if (queueManager != null)
            {
                var clientModel = GetQueueManager(queueManager, consumerDic["QueueName"], consumerDic["ExchangeName"], consumerDic["RoutingKey"]);
                _consumerManagers.Add(new Consumer(clientModel, rmqcConsumer));
            }

        }

        private static void SetPublisher(IDictionary<string, string> publisherDic, IConfigurationSection queueManagers)
        {
            var rmqcPublisher = new PublisherModel
            {
                ConfirmPublish = Convert.ToBoolean(publisherDic["ConfirmPublish"]),
                Mandatory = Convert.ToBoolean(publisherDic["Mandatory"])
            };

            string queueManagerName = publisherDic["QueueManager"];
            var queueManager = ConfigHelper.GetDictionary(queueManagers.GetSection(queueManagerName));
            if (queueManager != null)
            {
                var clientModel = GetQueueManager(queueManager, publisherDic["QueueName"], publisherDic["ExchangeName"], publisherDic["RoutingKey"]);
                _publisherManagers.Add(new Publisher(clientModel, rmqcPublisher));
            }
        }
    }
}
