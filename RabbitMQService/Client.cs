using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQService.Model;

namespace RabbitMQService
{
    /// <summary>
    /// Rabbit MQ Client Abstract Class
    /// </summary>
    public abstract class Client
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        protected readonly ClientModel _rabbitMQClientModel;

        protected Client(ClientModel rabbitMQClientModel)
        {
            _rabbitMQClientModel = rabbitMQClientModel ?? throw new ArgumentNullException(nameof(rabbitMQClientModel));
            try
            {
                ConnectionFactory connectionFactory = GetConnectionFactory();
                _connection = connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
            }
            catch (BrokerUnreachableException e)
            {
                throw e;
            }
        }

        #region Public Property

        /// <summary>
        /// Rabbit MQ Channel
        /// </summary>
        protected IModel Channel
        {
            get
            {
                lock (_channel)
                {
                    return _channel;
                }
            }
        }
        #endregion

        #region Public Method

        /// <summary>
        /// Rabbit MQ Bind 
        /// </summary>
        /// <param name="arguments"></param>
        public void BindQueue(IDictionary<string, object>? arguments = null)
        {
            _channel.QueueBind(_rabbitMQClientModel.Queue.QueueName, _rabbitMQClientModel.Queue.ExchangeName, _rabbitMQClientModel.Queue.RoutingKey, arguments);
        }
        #endregion

        #region Private Method
        private ConnectionFactory GetConnectionFactory()
        {
            var connectionFactory = new ConnectionFactory
            {
                UserName = _rabbitMQClientModel.Username,
                Password = _rabbitMQClientModel.Password,
                VirtualHost = _rabbitMQClientModel.VirtualHostName,
                HostName = _rabbitMQClientModel.HostName
            };
            string fClientProvidedName = _rabbitMQClientModel.ClientProvidedName;
            if (!string.IsNullOrEmpty(fClientProvidedName))
            {
                connectionFactory.ClientProvidedName = fClientProvidedName;
            }
            connectionFactory.AutomaticRecoveryEnabled = _rabbitMQClientModel.AutomaticRecoveryEnabled;
            connectionFactory.TopologyRecoveryEnabled = _rabbitMQClientModel.TopologyRecoveryEnabled;
            return connectionFactory;
        }
        #endregion

        #region Finalize & Dispose Method
        ~Client()
        {
            Dispose(false);
        }

        public void Dispose(bool isDispose)
        {
            if (isDispose)
            {
                _channel.Dispose();
                _connection.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        }
        #endregion
    }
}