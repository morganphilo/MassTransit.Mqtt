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
    public int GatewayFree { get; set; }
    public double GatewayLoad { get; set; }
    public string BleName { get; set; }
    public string IbeaconUuid { get; set; }
    public double IbeaconMajor { get; set; }
    public double IbeaconMinor { get; set; }
    public double IbeaconTxPower { get; set; }
    public double Battery { get; set; }

    public string Rssi { get; set; }
    public string RawData { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }

  }
}
