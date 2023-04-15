using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class ConsumerModel
    {
        public string ConsumerTag { get; set; } = string.Empty;
        public bool NoLocal { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoAcknowledgment { get; set; }
    }
}
