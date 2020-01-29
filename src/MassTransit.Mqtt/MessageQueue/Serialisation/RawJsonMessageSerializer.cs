using System;
using System.IO;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using MassTransit.Serialization;
using Newtonsoft.Json;

namespace MassTransit.Mqtt.MessageQueue.Serialisation
{
  public class RawJsonMessageSerializer :
      IMessageSerializer
  {
    public const string ContentTypeHeaderValue = "application/json";
    public static readonly ContentType RawJsonContentType = new ContentType(ContentTypeHeaderValue);

    static readonly Lazy<JsonSerializer> _deserializer;
    static readonly Lazy<Encoding> _encoding;
    static readonly Lazy<JsonSerializer> _serializer;
    private readonly ContentType contentType;

    static RawJsonMessageSerializer()
    {
      _encoding = new Lazy<Encoding>(() => new UTF8Encoding(false, true), LazyThreadSafetyMode.PublicationOnly);

      _deserializer = new Lazy<JsonSerializer>(() => JsonSerializer.Create(JsonMessageSerializer.DeserializerSettings));
      _serializer = new Lazy<JsonSerializer>(() => JsonSerializer.Create(JsonMessageSerializer.SerializerSettings));
    }

    public static JsonSerializer Deserializer => _deserializer.Value;

    public static JsonSerializer Serializer => _serializer.Value;

    public RawJsonMessageSerializer(ContentType contentType)
    {
      this.contentType = contentType;
    }

    public void Serialize<T>(Stream stream, SendContext<T> context)
        where T : class
    {
      try
      {
        context.ContentType = contentType;

        using var writer = new StreamWriter(stream, _encoding.Value, 1024, true);
        using var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };

        _serializer.Value.Serialize(jsonWriter, context.Message, typeof(T));

        jsonWriter.Flush();
        writer.Flush();
      }
      catch (SerializationException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new SerializationException("Failed to serialize message", ex);
      }
    }

    public ContentType ContentType => contentType;
  }
}
