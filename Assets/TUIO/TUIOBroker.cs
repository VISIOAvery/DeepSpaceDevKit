using System;
using System.Collections.Generic;
using TUIO;
using UnityEngine;

namespace TUIO
{
    /// <summary>
    /// Interface for any class that wants to receive TUIO events
    /// </summary>
    public interface ITUIOReceiver
    {
        void OnNewTUIOContainer(TuioContainer container);
        void OnUpdateTUIOContainer(TuioContainer container);
        void OnRemoveTUIOContainer(TuioContainer container);
    }

    /// <summary>
    /// Static broker class that handles dispatching TUIO events to all registered receivers
    /// </summary>
    public static class TUIOBroker
    {
        // Events for subscribers
        public static event Action<TuioContainer> OnNewContainer;
        public static event Action<TuioContainer> OnUpdateContainer;
        public static event Action<TuioContainer> OnRemoveContainer;
        
        // List of all registered TUIO receivers
        private static List<ITUIOReceiver> receivers = new List<ITUIOReceiver>();
        
        // Current active containers
        public static List<TuioContainer> CurrentContainers = new List<TuioContainer>();

        /// <summary>
        /// Register a new TUIO receiver to get events
        /// </summary>
        public static void RegisterTUIOReceiver(ITUIOReceiver receiver)
        {
            if (receivers.Contains(receiver))
            {
                Debug.LogWarning($"Could not add receiver, already registered: {receiver}");
                return;
            }

            receivers.Add(receiver);
        }

        /// <summary>
        /// Unregister a TUIO receiver
        /// </summary>
        public static void UnregisterTUIOReceiver(ITUIOReceiver receiver)
        {
            if (!receivers.Contains(receiver))
            {
                Debug.LogWarning($"Could not unregister receiver, does not exist: {receiver}");
                return;
            }

            receivers.Remove(receiver);
        }

        /// <summary>
        /// Dispatch a new TUIO container event to all receivers
        /// </summary>
        public static void DispatchNewContainer(TuioContainer container)
        {
            CurrentContainers.Add(container);
            
            foreach (ITUIOReceiver receiver in receivers)
            {
                receiver.OnNewTUIOContainer(container);
            }
            
            OnNewContainer?.Invoke(container);
        }

        /// <summary>
        /// Dispatch an update TUIO container event to all receivers
        /// </summary>
        public static void DispatchUpdateContainer(TuioContainer container)
        {
            foreach (ITUIOReceiver receiver in receivers)
            {
                receiver.OnUpdateTUIOContainer(container);
            }
            
            OnUpdateContainer?.Invoke(container);
        }

        /// <summary>
        /// Dispatch a remove TUIO container event to all receivers
        /// </summary>
        public static void DispatchRemoveContainer(TuioContainer container)
        {
            CurrentContainers.Remove(container);
            
            foreach (ITUIOReceiver receiver in receivers)
            {
                receiver.OnRemoveTUIOContainer(container);
            }
            
            OnRemoveContainer?.Invoke(container);
        }
    }
}