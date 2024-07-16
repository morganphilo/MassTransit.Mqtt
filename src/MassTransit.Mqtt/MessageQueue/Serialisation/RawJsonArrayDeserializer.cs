using MassTransit.Serialization;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace MassTransit.Mqtt.MessageQueue.Serialisation
{
  public class SystemTextJsonArrayRawMessageSerializerFactory :
      ISerializerFactory
  {
    readonly Lazy<SystemTextJsonArrayRawMessageSerializer> _serializer;

    public SystemTextJsonArrayRawMessageSerializerFactory(RawSerializerOptions options = RawSerializerOptions.Default)
    {
      _serializer = new Lazy<SystemTextJsonArrayRawMessageSerializer>(() => new SystemTextJsonArrayRawMessageSerializer(options));
    }

    public ContentType ContentType => SystemTextJsonRawMessageSerializer.JsonContentType;

    public IMessageSerializer CreateSerializer()
    {
      return _serializer.Value;
    }

    public IMessageDeserializer CreateDeserializer()
    {
      return _serializer.Value;
    }
  }

  public class SystemTextJsonArrayRawMessageSerializer :
      RawMessageSerializer,
      IMessageDeserializer,
      IMessageSerializer
  {
    public static readonly ContentType JsonContentType = new ContentType("application/json");

    readonly RawSerializerOptions _options;

    public SystemTextJsonArrayRawMessageSerializer(RawSerializerOptions options = RawSerializerOptions.Default)
    {
      _options = options;
    }

    public ContentType ContentType => JsonContentType;

    public void Probe(ProbeContext context)
    {
      var scope = context.CreateScope("json");
      scope.Add("contentType", ContentType.MediaType);
      scope.Add("provider", "System.Text.Json");
    }

    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
      return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
    }

    public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
    {
      try
      {
        JsonElement? bodyElement;
        if (body is JsonMessageBody jsonMessageBody)
          bodyElement = jsonMessageBody.GetJsonElement(SystemTextJsonMessageSerializer.Options);
        else
        {
          var bytes = body.GetBytes();
          bodyElement = bytes.Length > 0
              ? JsonSerializer.Deserialize<JsonElement>(bytes, SystemTextJsonMessageSerializer.Options)
              : null;
        }

        bodyElement ??= JsonDocument.Parse("{}").RootElement;

        var messageTypes = headers.GetMessageTypes();

        var messageContext = new RawMessageContext(headers, destinationAddress, _options);

        var serializerContext = new SystemTextJsonArrayRawSerializerContext(SystemTextJsonMessageSerializer.Instance,
            SystemTextJsonMessageSerializer.Options, ContentType, messageContext, messageTypes, _options, bodyElement.Value);

        return serializerContext;
      }
      catch (SerializationException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new SerializationException("An error occured while deserializing the message enveloper", ex);
      }
    }

    public MessageBody GetMessageBody(string text)
    {
      return new StringMessageBody(text);
    }

    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
      if (_options.HasFlag(RawSerializerOptions.AddTransportHeaders))
        SetRawMessageHeaders(context);

      return new SystemTextJsonRawMessageBody<T>(context, SystemTextJsonMessageSerializer.Options);
    }
  }

  public class SystemTextJsonArrayRawSerializerContext :
        SystemTextJsonArraySerializerContext
  {
    readonly RawSerializerOptions _rawOptions;

    public SystemTextJsonArrayRawSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, RawSerializerOptions rawOptions, JsonElement message)
        : base(objectDeserializer, options, contentType, messageContext, messageTypes, message: message)
    {
      _rawOptions = rawOptions;
    }

    public override IMessageSerializer GetMessageSerializer()
    {
      return new SystemTextJsonBodyMessageSerializer(Message, ContentType, Options, _rawOptions);
    }

    public override bool IsSupportedMessageType<T>()
    {
      var typeUrn = MessageUrn.ForTypeString<T>();

      return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
          || SupportedMessageTypes.Length == 0
          || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    public override bool IsSupportedMessageType(Type messageType)
    {
      var typeUrn = MessageUrn.ForTypeString(messageType);

      return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
          || SupportedMessageTypes.Length == 0
          || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
      if (message == null)
        throw new ArgumentNullException(nameof(message));

      return new SystemTextJsonBodyMessageSerializer(message, ContentType, Options, _rawOptions);
    }

    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
      var serializer = new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options, _rawOptions);

      serializer.Overlay(message);

      return serializer;
    }
  }

  public class SystemTextJsonArraySerializerContext :
      BaseSerializerContext
  {
    readonly MessageEnvelope? _envelope;

    public SystemTextJsonArraySerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, MessageEnvelope? envelope = null, object? message = null)
        : base(objectDeserializer, messageContext, messageTypes)
    {
      _envelope = envelope;
      ContentType = contentType;
      Message = message ?? envelope?.Message ?? throw new ArgumentNullException(nameof(envelope));
      Options = options;
    }

    protected object Message { get; }
    protected ContentType ContentType { get; }
    protected JsonSerializerOptions Options { get; }

    public override bool TryGetMessage<T>(out T? message)
        where T : class
    {
      var jsonElement = GetJsonElement(Message);

      if (typeof(T) == typeof(JsonObject))
      {
        message = JsonObject.Create(jsonElement) as T;
        return message != null;
      }

      if (IsSupportedMessageType<T>())
      {
        if (Message is T messageOfT)
        {
          message = messageOfT;
          return true;
        }

        /*
         * Array Tolerant Message Parsing
         */
        var rawJsonInterface = typeof(T).GetInterface("IRawJsonArrayMessage`1", true);
        if (rawJsonInterface != null) {
          var mrt = Activator.CreateInstance(typeof(T));

          var typeToDeserialize = rawJsonInterface.GenericTypeArguments[0];

          var itemsType = typeof(List<>).MakeGenericType(typeToDeserialize);

          var methods = itemsType.GetMethods();

          // if the element is an array, switch to array deserialization
          if (jsonElement.ValueKind == JsonValueKind.Array)
          {
            var items = jsonElement.Deserialize(itemsType, Options);

            typeof(T).GetProperty(nameof(IRawJsonArrayMessage<object>.Items)).SetValue(mrt, items);

            message = (T?)mrt;
            return message != null;
          }
          // fallback to object deserialization and populate the Items with a single value
          else
          {
            var items = Activator.CreateInstance(itemsType);

            var item = jsonElement.Deserialize(typeToDeserialize);

            itemsType.GetMethod(nameof(List<object>.Add), [typeToDeserialize]).Invoke(items, [item]);

            typeof(T).GetProperty(nameof(IRawJsonArrayMessage<object>.Items)).SetValue(mrt, items);
            message = (T?)mrt;
            return message != null;
          }
        }

        message = jsonElement.Deserialize<T>(Options);
        return message != null;
      }

      message = null;
      return false;
    }

    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
      var jsonElement = GetJsonElement(Message);

      message = jsonElement.Deserialize(messageType, Options);

      return message != null;
    }

    public override IMessageSerializer GetMessageSerializer()
    {
      if (_envelope == null)
        throw new InvalidOperationException("This should be overloaded");

      return new SystemTextJsonBodyMessageSerializer(_envelope, ContentType, Options);
    }

    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
      var serializer = new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options);

      serializer.Overlay(message);

      return serializer;
    }

    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
      if (message == null)
        throw new ArgumentNullException(nameof(message));

      var envelope = new JsonMessageEnvelope(this, message, messageTypes);

      return new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options, messageTypes);
    }

    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
      return message == null
          ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
          : JsonSerializer.SerializeToElement(message, Options).Deserialize<Dictionary<string, object>>()!;
    }

    static JsonElement GetJsonElement(object message)
    {
      return message is JsonElement element
          ? element.ValueKind == JsonValueKind.Null
              ? new JsonElement()
              : element
          : new JsonElement();
    }
  }

}
