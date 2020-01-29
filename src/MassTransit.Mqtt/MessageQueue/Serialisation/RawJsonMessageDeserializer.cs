﻿using System;
using System.IO;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using GreenPipes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MassTransit.Mqtt.MessageQueue.Serialisation
{
  public class RawJsonMessageDeserializer :
      IMessageDeserializer
  {
    private readonly ContentType contentType;
    readonly JsonSerializer _deserializer;

    public RawJsonMessageDeserializer(ContentType contentType, JsonSerializer deserializer)
    {
      this.contentType = contentType;
      _deserializer = deserializer;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
      var scope = context.CreateScope("json");
      scope.Add("contentType", contentType.MediaType);
    }

    ContentType IMessageDeserializer.ContentType => contentType;

    ConsumeContext IMessageDeserializer.Deserialize(ReceiveContext receiveContext)
    {
      try
      {
        var messageEncoding = GetMessageEncoding(receiveContext);

        using var body = receiveContext.GetBodyStream();
        using var reader = new StreamReader(body, messageEncoding, false, 1024, true);
        using var jsonReader = new JsonTextReader(reader);

        var messageToken = _deserializer.Deserialize<JToken>(jsonReader);

        Guid? messageId = default;
        if (receiveContext.TransportHeaders.TryGetHeader(/*MessageHeaders.MessageId*/ "MessageId", out var headerValue)
            && headerValue is string value && Guid.TryParse(value, out var id))
          messageId = id;

        return new RawJsonConsumeContext(_deserializer, receiveContext, messageToken, messageId);
      }
      catch (JsonSerializationException ex)
      {
        throw new SerializationException("A JSON serialization exception occurred while deserializing the message", ex);
      }
      catch (SerializationException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new SerializationException("An exception occurred while deserializing the message", ex);
      }
    }

    static Encoding GetMessageEncoding(ReceiveContext receiveContext)
    {
      var contentEncoding = receiveContext.TransportHeaders.Get("Content-Encoding", default(string));

      return string.IsNullOrWhiteSpace(contentEncoding) ? Encoding.UTF8 : Encoding.GetEncoding(contentEncoding);
    }
  }
}
