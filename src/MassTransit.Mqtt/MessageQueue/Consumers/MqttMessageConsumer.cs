using MassTransit.Mqtt.MessageQueue.Messages;
using MassTransit.Mqtt.MessageQueue.Serialisation;
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
      // Read the inbound message from the Device
      foreach (var item in context.Message.Items)
      {
        Console.WriteLine(item.Mac);
      }

      /*
       * The routing key is the topic that the inbound message was written to.
       * You should be creating a rabbitmq user per device, and applying topic based permissions
       * This limits what topics mqtt clients can read and write to/from.
       */
      var routingKey = context.RoutingKey();


      // Now lets write a message back to the device

      var deviceId = "your device Id";

      /*
       * IMPORTANT
       * For this to work, you will need to bind the exchange manually in RabbitMq Admin.
       
       * Go to the exchange MassTransit.Mqtt.MessageQueue.Serialisation:RawTextMessage
       * and make a binding to masstransit.mqtt with the routing key '#'
       */

      await context.Publish(new RawTextMessage
      {
        Body = "message to device"
      }, x =>
      {
        x.Serializer = new RawTextSerializer();
        /*
         * The routing key here is the topic that the device listens on
         * It is converted from MQTT topic to Rabbit MQ Topic format
         * https://www.rabbitmq.com/mqtt.html#implementation
         */
        x.SetRoutingKey($"device.{deviceId}.response");
      });

      await Task.CompletedTask;
    }
  }
}