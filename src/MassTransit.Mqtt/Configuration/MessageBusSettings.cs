using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MassTransit.Mqtt.Configuration
{
  public class MessageBusSettings
  {
    public string Url { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
  }
}
