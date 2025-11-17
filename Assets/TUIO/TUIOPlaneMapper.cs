using UnityEngine;
using TUIO;
using System.Collections.Generic;

namespace TUIO
{
    /// <summary>
    /// Maps TUIO input to a plane in 3D space
    /// </summary>
    public class TUIOPlaneMapper : MonoBehaviour, ITUIOReceiver
    {
        // Events für Cursor-Änderungen
        public event System.Action<long, Vector3> OnCursorAdded;
        public event System.Action<long, Vector3> OnCursorUpdated;
        public event System.Action<long> OnCursorRemoved;

        [Header("Plane Settings")]
        [Tooltip("Width of the plane in world units")]
        public float width = 1.0f;

        [Tooltip("Height of the plane in world units")]
        public float height = 1.0f;

        [Tooltip("Whether to flip the Y axis")]
        public bool flipY = false;

        [Header("TUIO Settings")]
        [Tooltip("The TUIOManager to receive events from")]
        public TUIOManager tuioManager;

        [Tooltip("Whether to receive events from TUIOBroker")]
        public bool useTUIOBroker = true;

        [Tooltip("Whether to show debug visualization")]
        public bool showDebug = true;

        [Tooltip("Color for debug visualization")]
        public Color debugColor = Color.red;

        [Tooltip("Size of debug visualization")]
        public float debugSize = 0.1f;

        // List of active cursors in world space
        public Dictionary<long, Vector3> activeCursors = new Dictionary<long, Vector3>();

        private void Start()
        {
            if (useTUIOBroker)
            {
                TUIOBroker.RegisterTUIOReceiver(this);
            }

            if (tuioManager != null)
            {
                tuioManager.OnNewContainer += OnNewTUIOContainer;
                tuioManager.OnUpdateContainer += OnUpdateTUIOContainer;
                tuioManager.OnRemoveContainer += OnRemoveTUIOContainer;
            }
        }

        private void OnDestroy()
        {
            if (useTUIOBroker)
            {
                TUIOBroker.UnregisterTUIOReceiver(this);
            }

            if (tuioManager != null)
            {
                tuioManager.OnNewContainer -= OnNewTUIOContainer;
                tuioManager.OnUpdateContainer -= OnUpdateTUIOContainer;
                tuioManager.OnRemoveContainer -= OnRemoveTUIOContainer;
            }
        }

        /// <summary>
        /// Maps TUIO coordinates (0-1) to world space on the plane
        /// </summary>
        private Vector3 MapToWorldSpace(float x, float y)
        {
            // Flip Y coordinate if needed
            float mappedY = flipY ? (1.0f - y) : y;

            // Calculate position in local space
            Vector3 localPos = new Vector3(
                (x - 0.5f) * width,
                0,
                (mappedY - 0.5f) * height
            );

            // Transform to world space
            return transform.TransformPoint(localPos);
        }

        #region ITUIOReceiver Implementation

        public void OnNewTUIOContainer(TuioContainer container)
        {
            if (container is TuioCursor)
            {
                TuioCursor cursor = container as TuioCursor;
                Vector3 worldPos = MapToWorldSpace(cursor.X, cursor.Y);
                activeCursors[cursor.SessionID] = worldPos;

                OnCursorAdded?.Invoke(cursor.SessionID, worldPos);

                if (showDebug)
                {
                    Debug.Log($"New TUIO cursor {cursor.SessionID} at world position {worldPos}");
                }
            }
        }

        public void OnUpdateTUIOContainer(TuioContainer container)
        {
            if (container is TuioCursor)
            {
                TuioCursor cursor = container as TuioCursor;
                Vector3 worldPos = MapToWorldSpace(cursor.X, cursor.Y);
                activeCursors[cursor.SessionID] = worldPos;

                OnCursorUpdated?.Invoke(cursor.SessionID, worldPos);

                if (showDebug)
                {
                    Debug.Log($"Updated TUIO cursor {cursor.SessionID} to world position {worldPos}");
                }
            }
        }

        public void OnRemoveTUIOContainer(TuioContainer container)
        {
            if (container is TuioCursor)
            {
                TuioCursor cursor = container as TuioCursor;
                activeCursors.Remove(cursor.SessionID);

                OnCursorRemoved?.Invoke(cursor.SessionID);

                if (showDebug)
                {
                    Debug.Log($"Removed TUIO cursor {cursor.SessionID}");
                }
            }
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (!showDebug)
                return;

            // Draw plane outline
            Gizmos.color = debugColor;
            Vector3[] corners = new Vector3[4];
            corners[0] = transform.TransformPoint(new Vector3(-width * 0.5f, 0, -height * 0.5f));
            corners[1] = transform.TransformPoint(new Vector3(width * 0.5f, 0, -height * 0.5f));
            corners[2] = transform.TransformPoint(new Vector3(width * 0.5f, 0, height * 0.5f));
            corners[3] = transform.TransformPoint(new Vector3(-width * 0.5f, 0, height * 0.5f));

            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);

            // Draw active cursors
            if (Application.isPlaying)
            {
                foreach (var cursor in activeCursors)
                {
                    Gizmos.DrawSphere(cursor.Value, debugSize);
                }
            }
        }
    }
}