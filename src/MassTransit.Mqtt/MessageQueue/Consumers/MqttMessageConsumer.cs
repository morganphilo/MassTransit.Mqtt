using MassTransit.Mqtt.MessageQueue.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MassTransit.Mqtt.MessageQueue.Consumers
{
  public class MqttMessageConsumer : IConsumer<MqttMessage>
  {
    public async Task Consume(ConsumeContext<MqttMessage> context)
    {
      //context.Message.



      await Task.CompletedTask;
    }
  }
}