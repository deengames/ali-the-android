using DeenGames.AliTheAndroid.Prototype.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Prototype {
    public class EventBus
    {
        private IDictionary<GameEvent, List<Action<object>>> eventListeners = new Dictionary<GameEvent, List<Action<object>>>();

        public static EventBus Instance { get; private set; } = new EventBus();

        private EventBus()
        {
            EventBus.Instance = this;
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
                foreach (var listener in this.eventListeners[eventName])
                {
                    listener.Invoke(data);
                }
            }
        }
    }
}