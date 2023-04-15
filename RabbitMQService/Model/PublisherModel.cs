using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class PublisherModel
    {
        public bool Mandatory { get; set; }
        public bool ConfirmPublish { get; set; }

    }
}
