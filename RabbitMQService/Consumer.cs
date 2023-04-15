using RabbitMQ.Client.Events;
using RabbitMQService.Model;

namespace RabbitMQService
{
    /// <summary>
    /// Rabbit MQ Consumer Class
    /// </summary>
    public class Consumer : Client
    {
        #region Private Member
        private readonly ConsumerModel _rabbitMQConsumerModel;
        #endregion

        #region ctor
        public Consumer(ClientModel rabbitMQClientModel, ConsumerModel rabbitMQConsumerModel) : base(rabbitMQClientModel)
        {
            _rabbitMQConsumerModel = rabbitMQConsumerModel;
        }
        #endregion

        #region Public Method

        /// <summary>
        /// Consume Consumer
        /// </summary>
        /// <param name="receivedCallback"></param>
        /// <param name="noLocal"></param>
        /// <param name="exclusive"></param>
        /// <param name="autoAcknowledgment"></param>
        /// <param name="consumerArguments"></param>
        /// <param name="basicAck"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Consume(Action<object?, BasicDeliverEventArgs> receivedCallback, bool? noLocal = null, bool? exclusive = null, bool? autoAcknowledgment = null, IDictionary<string, object>? consumerArguments = null, bool basicAck = false)
        {
            if (receivedCallback == null)
            {
                throw new ArgumentNullException(nameof(receivedCallback));
            }

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (model, eventArgs) =>
            {
                receivedCallback(model, eventArgs);
            };
            Channel.BasicConsume(queue: _rabbitMQClientModel.Queue.QueueName,
                consumerTag: _rabbitMQConsumerModel.ConsumerTag,
                consumer: consumer,
                autoAck: autoAcknowledgment ?? _rabbitMQConsumerModel.AutoAcknowledgment,
                noLocal: noLocal ?? _rabbitMQConsumerModel.NoLocal,
                exclusive: exclusive ?? _rabbitMQConsumerModel.Exclusive,
                arguments: consumerArguments);

        }

        /// <summary>
        /// Possitive Acknowledgement 
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="multiple"></param>
        public void Ack(ulong deliveryTag, bool multiple)
        {
            Channel.BasicAck(deliveryTag, multiple);
        }

        /// <summary>
        /// Negative Acknowledgement
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="multiple"></param>
        /// <param name="requeue"></param>
        public void Nack(ulong deliveryTag, bool multiple, bool requeue)
        {
            Channel.BasicNack(deliveryTag, multiple, requeue);
        }

        /// <summary>
        /// Reject
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="requeue"></param>
        public void Reject(ulong deliveryTag, bool requeue)
        {
            Channel.BasicReject(deliveryTag, requeue);

        }

        /// <summary>
        /// Cancel
        /// </summary>
        public void Cancel()
        {
            Channel.BasicCancel(_rabbitMQConsumerModel.ConsumerTag);
        }
        #endregion

    }
}