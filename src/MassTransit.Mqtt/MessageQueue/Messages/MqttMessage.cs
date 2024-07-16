using MassTransit.Mqtt.MessageQueue.Serialisation;
using System;
using System.Collections.Generic;

namespace MassTransit.Mqtt.MessageQueue.Messages
{
  public class MqttMessage : IRawJsonArrayMessage<MqttItem>
  {
    public List<MqttItem> Items { get; set; }
  }

  /// <summary>
  /// An inbound MQTT Message, this should match your accepted inbound MQTT message
  /// structure, what is shown here is only to demonstrate a real world example
  /// </summary>
  public class MqttItem
  {
    public DateTimeOffset? Timestamp { get; set; }
    public string Type { get; set; }
    public string Mac { get; set; }
    public int Rssi { get; set; }

  }
}
