using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace TUIO
{
    /// <summary>
    /// Container state enum for thread-safe queue
    /// </summary>
    public enum TUIOContainerState
    {
        ADD,
        UPDATE,
        REMOVE
    }

    /// <summary>
    /// A Unity MonoBehaviour that manages TUIO input, connecting to a TUIO server and 
    /// handling TUIO events. The visualization is handled by TUIOVisualizer.
    /// </summary>
    public class TUIOManager : MonoBehaviour, TuioListener
    {
        [Header("TUIO Connection Settings")]
        [Tooltip("Port number to listen for TUIO messages")]
        public int port = 3333;

        [Header("TUIO Processing")]
        [Tooltip("Whether to ignore TUIO objects")]
        public bool ignoreTUIOObjects = true;
        [Tooltip("Whether to forward to the TUIOBroker")]
        public bool forwardToTUIOBroker = true;
        [Tooltip("Delay before starting TUIO client")]
        public float startDelay = 0f;

        // TUIO client instance
        public TuioClient client;

        // Thread-safe queue for TUIO container events
        private ConcurrentQueue<Tuple<TuioContainer, TUIOContainerState>> inputQueue =
            new ConcurrentQueue<Tuple<TuioContainer, TUIOContainerState>>();

        // Events
        public event Action<TuioContainer> OnNewContainer;
        public event Action<TuioContainer> OnUpdateContainer;
        public event Action<TuioContainer> OnRemoveContainer;

        private void Start()
        {
            Debug.Log("private void Start");
            StartCoroutine(StartTUIOClient());
        }

        private IEnumerator StartTUIOClient()
        {
            if (startDelay > 0)
            {
                yield return new WaitForSeconds(startDelay);
            }

            // Initialize TUIO with error suppression
            client = new TuioClient(port);

            client.addTuioListener(this);
            client.connect();
            Debug.Log($"TUIO client started on port {port}");
        }

        private void Update()
        {
            // Process all queued TUIO events in the main thread
            Tuple<TuioContainer, TUIOContainerState> containerEvent;
            while (inputQueue.TryDequeue(out containerEvent))
            {
                TuioContainer container = containerEvent.Item1;
                TUIOContainerState state = containerEvent.Item2;

                switch (state)
                {
                    case TUIOContainerState.ADD:
                        ProcessAddOnMainThread(container);
                        break;
                    case TUIOContainerState.UPDATE:
                        ProcessUpdateOnMainThread(container);
                        break;
                    case TUIOContainerState.REMOVE:
                        ProcessRemoveOnMainThread(container);
                        break;
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up TUIO resources
            if (client != null)
            {
                client.removeTuioListener(this);
                client.disconnect();
                client = null;
            }
        }

        #region Container Processing on Main Thread

        /// <summary>
        /// Process a new TUIO container on the main thread
        /// </summary>
        private void ProcessAddOnMainThread(TuioContainer container)
        {
            Debug.Log("add: " + container.SessionID);
            // Log basic information
            if (container is TuioCursor)
            {
                TuioCursor tcur = container as TuioCursor;
                // Debug.Log($"TUIO Cursor added: ID={tcur.SessionID}, Position=({tcur.X:F2}, {tcur.Y:F2})");
            }
            else if (container is TuioObject)
            {
                TuioObject tobj = container as TuioObject;
                // Debug.Log($"TUIO Object added: ID={tobj.SessionID}, SymbolID={tobj.SymbolID}, Position=({tobj.X:F2}, {tobj.Y:F2})");
            }
            else if (container is TuioBlob)
            {
                TuioBlob tblb = container as TuioBlob;
                // Debug.Log($"TUIO Blob added: ID={tblb.SessionID}, Position=({tblb.X:F2}, {tblb.Y:F2}), Size=({tblb.Width:F2}, {tblb.Height:F2})");
            }

            // Invoke local event
            OnNewContainer?.Invoke(container);

            // Forward to TUIOBroker if enabled
            if (forwardToTUIOBroker)
            {
                TUIOBroker.DispatchNewContainer(container);
            }
        }

        /// <summary>
        /// Process an update to a TUIO container on the main thread
        /// </summary>
        private void ProcessUpdateOnMainThread(TuioContainer container)
        {
            // Debug.Log("update: " + container.SessionID + " " + container.Position.X + " - " + container.Position.Y);
            // Always debug the first 10 frames, then every 60 frames to avoid spam
            if (Time.frameCount < 10 || Time.frameCount % 60 == 0)
            {
                if (container is TuioCursor)
                {
                    TuioCursor tcur = container as TuioCursor;
                    // Debug.Log($"TUIO Cursor updated: ID={tcur.SessionID}, Position=({tcur.X:F2}, {tcur.Y:F2}), Speed={tcur.MotionSpeed:F3}");
                }
                else if (container is TuioObject)
                {
                    TuioObject tobj = container as TuioObject;
                    // Debug.Log($"TUIO Object updated: ID={tobj.SessionID}, SymbolID={tobj.SymbolID}, Position=({tobj.X:F2}, {tobj.Y:F2}), Angle={tobj.Angle:F2}");
                }
                else if (container is TuioBlob)
                {
                    TuioBlob tblb = container as TuioBlob;
                    // Debug.Log($"TUIO Blob updated: ID={tblb.SessionID}, Position=({tblb.X:F2}, {tblb.Y:F2}), Size=({tblb.Width:F2}, {tblb.Height:F2})");
                }
            }

            // Invoke local event
            OnUpdateContainer?.Invoke(container);

            // Forward to TUIOBroker if enabled
            if (forwardToTUIOBroker)
            {
                TUIOBroker.DispatchUpdateContainer(container);
            }
        }

        /// <summary>
        /// Process the removal of a TUIO container on the main thread
        /// </summary>
        private void ProcessRemoveOnMainThread(TuioContainer container)
        {
            Debug.Log("remove: " + container.SessionID);
            // Log removal
            if (container is TuioCursor)
            {
                TuioCursor tcur = container as TuioCursor;
                // Debug.Log($"TUIO Cursor removed: ID={tcur.SessionID}");
            }
            else if (container is TuioObject)
            {
                TuioObject tobj = container as TuioObject;
                // Debug.Log($"TUIO Object removed: ID={tobj.SessionID}, SymbolID={tobj.SymbolID}");
            }
            else if (container is TuioBlob)
            {
                TuioBlob tblb = container as TuioBlob;
                // Debug.Log($"TUIO Blob removed: ID={tblb.SessionID}");
            }

            // Invoke local event
            OnRemoveContainer?.Invoke(container);

            // Forward to TUIOBroker if enabled
            if (forwardToTUIOBroker)
            {
                TUIOBroker.DispatchRemoveContainer(container);
            }
        }

        #endregion

        #region TuioListener Implementation - Queue events from background thread

        public void addTuioObject(TuioObject tobj)
        {
            Debug.Log("addTuioObject");
            if (ignoreTUIOObjects)
                return;

            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tobj, TUIOContainerState.ADD));
        }

        public void updateTuioObject(TuioObject tobj)
        {
            if (ignoreTUIOObjects)
                return;

            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tobj, TUIOContainerState.UPDATE));
        }

        public void removeTuioObject(TuioObject tobj)
        {
            if (ignoreTUIOObjects)
                return;

            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tobj, TUIOContainerState.REMOVE));
        }

        public void addTuioCursor(TuioCursor tcur)
        {
            Debug.Log("addTuioObject");
            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tcur, TUIOContainerState.ADD));
        }

        public void updateTuioCursor(TuioCursor tcur)
        {
            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tcur, TUIOContainerState.UPDATE));
        }

        public void removeTuioCursor(TuioCursor tcur)
        {
            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tcur, TUIOContainerState.REMOVE));
        }

        public void addTuioBlob(TuioBlob tblb)
        {
            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tblb, TUIOContainerState.ADD));
        }

        public void updateTuioBlob(TuioBlob tblb)
        {
            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tblb, TUIOContainerState.UPDATE));
        }

        public void removeTuioBlob(TuioBlob tblb)
        {
            inputQueue.Enqueue(new Tuple<TuioContainer, TUIOContainerState>(tblb, TUIOContainerState.REMOVE));
        }

        public void refresh(TuioTime ftime)
        {
            // Nothing to do here - we don't need to queue refresh events
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Returns a list of all active TUIO cursors
        /// </summary>
        public List<TuioCursor> GetAllCursors()
        {
            return client != null ? client.getTuioCursors() : new List<TuioCursor>();
        }

        /// <summary>
        /// Returns a list of all active TUIO objects
        /// </summary>
        public List<TuioObject> GetAllObjects()
        {
            return client != null ? client.getTuioObjects() : new List<TuioObject>();
        }

        /// <summary>
        /// Returns a list of all active TUIO blobs
        /// </summary>
        public List<TuioBlob> GetAllBlobs()
        {
            return client != null ? client.getTuioBlobs() : new List<TuioBlob>();
        }

        #endregion
    }
}