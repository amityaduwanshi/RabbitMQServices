using RabbitMQService;
using RabbitMQService.Model;
using Services.Helper;
using System.Text;

namespace Services.Processors
{
    public static class RabbitMQProcessor
    {
        private static readonly IList<Consumer> _consumerManagers;
        private static readonly IList<Publisher> _publisherManagers;

        #region ctor
        static RabbitMQProcessor()
        {
            _consumerManagers = new List<Consumer>();
            _publisherManagers = new List<Publisher>();
        }
        #endregion

        #region Public Method
        /// <summary>
        /// Configure Consumer and Publisher with it's respective configuration
        /// </summary>
        /// <param name="configuration"></param>
        public static void Bootstrap(IConfigurationSection configuration)
        {
            var queueManagers = configuration.GetSection("QueueManagers");
            var consumers = configuration.GetSection("Consumers");
            var publishers = configuration.GetSection("Publishers");

            foreach (var consumer in consumers.GetChildren())
            {
                var consumerDic = ConfigHelper.GetDictionary(consumer);
                if (consumerDic == null) continue;
                SetConsumer(consumerDic, queueManagers);
            }         
        }

        /// <summary>
        /// Activate Consumers
        /// </summary>
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

        public static ClientModel GetQueueManager(IDictionary<string, string> queueManager, string queueName, string exchangeKey, string routingKey)
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
                },
                Ssl = new ClientSSL()
                {
                    Enabled = queueManager["SslEnabled"] != null ? Convert.ToBoolean(queueManager["SslEnabled"]) : false,
                    ServerName = queueManager["ServerName"],
                    CertPath = queueManager["CertPath"],
                    CertPassphrase = queueManager["CertPassphrase"]
                }
            };

        }

        public static Publisher? GetPublisher(IDictionary<string, string> publisherDic, IConfigurationSection queueManagers)
        {
            Publisher? publisher = null;
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
                publisher = new Publisher(clientModel, rmqcPublisher);
            }

            return publisher;
        }
        #endregion

        #region Private Method

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
            var publisher = GetPublisher(publisherDic, queueManagers);
            if (publisher != null)
            {
                _publisherManagers.Add(publisher);
            }
           
        }
        #endregion
    }
}
