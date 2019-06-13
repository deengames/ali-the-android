using System;
using System.Collections;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Model.Events
{
    public class EventBus
    {
        // For parallel testing, don't share the EventBus instance; that leads to collection modification
        // exceptions because all the threads use the same event bus (same event listener collections).
        public static bool UseConcurrentModel = false; 

        private IDictionary<GameEvent, List<Action<object>>> eventListeners = new Dictionary<GameEvent, List<Action<object>>>();
        private static EventBus instance = new EventBus();

        public static EventBus Instance { get {
            if (!UseConcurrentModel)
            {
                return Instance; // prod workflow
            }
            else
            {
                return new EventBus();  // parallelized tests flow
            }
        } }

        private EventBus()
        {
            EventBus.instance = this;
        }

        public void AddListener(GameEvent eventName, Action<object> listener)
        {
            if (!this.eventListeners.ContainsKey(eventName))
            {
                this.eventListeners[eventName] = new List<Action<object>>();
            }

            this.eventListeners[eventName].Add(listener);
        }

        public void Broadcast(GameEvent eventName, object data)
        {
            if (this.eventListeners.ContainsKey(eventName))
            {
                // Make a copy so that, any events triggering things that bind events, can do so.
                // eg. move => lays egg => adds an event listener. Can't modify collection mid-iteration.
                foreach (var listener in this.eventListeners[eventName].ToArray())
                {
                    listener.Invoke(data);
                }
            }
        }

        public void RemoveListener(GameEvent eventName, object listener)
        {
            if (this.eventListeners.ContainsKey(eventName))
            {
                var toRemove = new List<Action<object>>();
                foreach (var l in this.eventListeners[eventName])
                {
                    if (l.Target == listener)
                    {
                        toRemove.Add(l);
                    }
                }
                this.eventListeners[eventName].RemoveAll(r => toRemove.Contains(r));
            }
        }
    }
}