using System.Collections.Generic;

namespace MassTransit.Mqtt.MessageQueue.Serialisation
{
  /// <summary>
  /// Indicates the message JSON is actually an array of type <typeparamref name="T"/>.
  /// </summary>
  /// <remarks>
  /// If your message JSON is an array, there is no way to map this to a single MassTransit message.
  /// We indicate this on the message decleration and the deserializer will assign the array objects
  /// to the <see cref="IRawJsonArrayMessage{T}.Items"/> array.
  /// </remarks>
  /// <typeparam name="T"></typeparam>
  public interface IRawJsonArrayMessage<T>
  {
    IEnumerable<T> Items { get; set; }
  }
}
