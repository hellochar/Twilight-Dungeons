using System;
using System.Collections.Generic;
using Priority_Queue;
using System.Runtime.Serialization;
using System.Linq;

[Serializable]
public class TimedEventManager : ISerializable {

  [NonSerialized]
  private SimplePriorityQueue<TimedEvent, float> timedEvents = new SimplePriorityQueue<TimedEvent, float>();

  public TimedEventManager() { }

  public TimedEventManager(SerializationInfo info, StreamingContext context) {
    var list = (List<TimedEvent>) info.GetValue("list", typeof(List<TimedEvent>));
    foreach (var item in list) {
      timedEvents.Enqueue(item, item.time);
    }
  }

  public void GetObjectData(SerializationInfo info, StreamingContext context) {
    info.AddValue("list", timedEvents.ToList(), typeof(List<TimedEvent>));
  }

  public TimedEvent Next() => (timedEvents.Count == 0) ? null : timedEvents.First;

  public void Register(TimedEvent evt) {
    timedEvents.Enqueue(evt, evt.time);
  }

  public void Unregister(TimedEvent evt) {
    timedEvents.TryRemove(evt);
    evt.UnregisterFromOwner();
  }
}