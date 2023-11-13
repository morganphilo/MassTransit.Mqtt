using System.Net.Mime;

namespace MassTransit.Mqtt.MessageQueue.Serialisation
{
  public class RawTextMessage
  {
    public string Body { get; internal set; }
  }

  public class RawTextSerializer : IMessageSerializer
  {
    public ContentType ContentType => new ContentType("text/plain");

    public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
    {
      var body = (context.Message as RawTextMessage)?.Body;
      if (string.IsNullOrEmpty(body))
      {
        body = context.Message.ToString();
      }

      return new StringMessageBody(body);
    }
  }
}
