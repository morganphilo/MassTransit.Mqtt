using MassTransit.Context;
using MassTransit.Metadata;
using MassTransit.Serialization;
using MassTransit.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MassTransit.Mqtt.MessageQueue.Serialisation
{
  public class RawJsonConsumeContext : DeserializerConsumeContext
  {
    readonly PendingTaskCollection _consumeTasks;
    readonly JsonSerializer _deserializer;
    readonly JToken _messageToken;
    readonly IDictionary<Type, ConsumeContext> _messageTypes;

    Guid? _conversationId;
    Guid? _correlationId;
    Uri _destinationAddress;
    Uri _faultAddress;
    Headers _headers;
    Guid? _initiatorId;
    Guid? _messageId;
    Guid? _requestId;
    Uri _responseAddress;
    Uri _sourceAddress;

    public RawJsonConsumeContext(JsonSerializer deserializer, ReceiveContext receiveContext, JToken messageToken, Guid? messageId = default)
        : base(receiveContext)
    {
      _messageToken = messageToken ?? new JObject();
      _messageId = messageId;

      _deserializer = deserializer;
      _messageTypes = new Dictionary<Type, ConsumeContext>();
      _consumeTasks = new PendingTaskCollection(4);
    }

    public override Task ConsumeCompleted => _consumeTasks.Completed(CancellationToken);

    public override Guid? MessageId => _messageId;
    public override Guid? RequestId => _requestId;
    public override Guid? CorrelationId => _correlationId;
    public override Guid? ConversationId => _conversationId;
    public override Guid? InitiatorId => _initiatorId;
    public override DateTime? ExpirationTime => default;
    public override Uri SourceAddress => _sourceAddress;
    public override Uri DestinationAddress => _destinationAddress;
    public override Uri ResponseAddress => _responseAddress;
    public override Uri FaultAddress => _faultAddress;
    public override DateTime? SentTime => default;

    public override Headers Headers => NoMessageHeaders.Instance;

    public override HostInfo Host => default;
    public override IEnumerable<string> SupportedMessageTypes => Enumerable.Empty<string>();

    public override bool HasMessageType(Type messageType)
    {
      lock (_messageTypes)
      {
        if (_messageTypes.TryGetValue(messageType, out var existing))
          return existing != null;
      }

      return false;
    }

    public override bool TryGetMessage<T>(out ConsumeContext<T> message)
    {
      lock (_messageTypes)
      {
        if (_messageTypes.TryGetValue(typeof(T), out var existing))
        {
          message = existing as ConsumeContext<T>;
          return message != null;
        }

        if (typeof(T) == typeof(JToken))
        {
          _messageTypes[typeof(T)] = message = new MessageConsumeContext<T>(this, _messageToken as T);
          return true;
        }

        string typeUrn = MessageUrn.ForTypeString<T>();

        try
        {
          object obj;
          Type deserializeType = typeof(T);

          if (deserializeType.GetTypeInfo().IsInterface && TypeMetadataCache<T>.IsValidMessageType) {
            deserializeType = TypeMetadataCache<T>.ImplementationType;
          }

          var messageToken = _messageToken;
          var jsonArrayType = deserializeType.GetInterfaces().FirstOrDefault(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IRawJsonArrayMessage<>));

          var isJsonArrayType = jsonArrayType != null && jsonArrayType.GenericTypeArguments.Length > 0;
          if (isJsonArrayType)
          {
            var parentMessageObj = new JObject();
            parentMessageObj.Add("items", _messageToken);
            messageToken = parentMessageObj;
          }

          using (JsonReader jsonReader = messageToken.CreateReader())
          {
            obj = _deserializer.Deserialize(jsonReader, deserializeType);
          }

          _messageTypes[typeof(T)] = message = new MessageConsumeContext<T>(this, (T)obj);
          return true;
        }
        catch (Exception e)
        {
          _messageTypes[typeof(T)] = message = null;
          return false;
        }
      }
    }

    public override void AddConsumeTask(Task task)
    {
      _consumeTasks.Add(task);
    }
  }
}
