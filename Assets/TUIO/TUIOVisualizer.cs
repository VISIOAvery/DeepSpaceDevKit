using System.Collections.Generic;
using UnityEngine;
using TUIO;

/// <summary>
/// Visualizes TUIO cursors and objects in a Unity scene.
/// Can be configured to visualize from either TUIOManager directly or via TUIOBroker.
/// </summary>
public class TUIOVisualizer : MonoBehaviour, ITUIOReceiver
{
    [Header("Connection Settings")]
    [Tooltip("Whether to use TUIOBroker for visualization instead of direct TUIOManager connection")]
    public bool usebroker = true;

    [Tooltip("Reference to TUIOManager (only needed if not using broker)")]
    public TUIOManager tuioManager;

    [Header("Visualization Settings")]
    [Tooltip("Whether to visualize cursors")]
    public bool showCursors = true;

    [Tooltip("Whether to visualize objects")]
    public bool showObjects = false;

    [Tooltip("Whether to visualize blobs")]
    public bool showBlobs = false;

    [Tooltip("Whether to draw cursor trail paths")]
    public bool showCursorTrails = true;

    [Tooltip("Whether to draw fullscreen ImGUI cursors")]
    public bool showFullscreenCursors = true;

    [Tooltip("Prefab to instantiate for each TUIO cursor")]
    public GameObject cursorPrefab;

    [Tooltip("Prefab to instantiate for each TUIO object")]
    public GameObject objectPrefab;

    [Tooltip("Prefab to instantiate for each TUIO blob")]
    public GameObject blobPrefab;

    [Tooltip("Size of the cursor visualization")]
    public float cursorSize = 0.05f;

    [Tooltip("Size of the object visualization")]
    public float objectSize = 0.1f;

    [Tooltip("Size of the blob visualization")]
    public float blobSize = 0.15f;

    [Tooltip("Whether to show debug information (labels, paths)")]
    public bool showDebugInfo = true;

    [Tooltip("Whether to show IMGUI overlay")]
    public bool showImGuiOverlay = true;

    [Tooltip("Color for cursors")]
    public Color cursorColor = new Color(1f, 1f, 1f, 0.7f);

    [Tooltip("Color for objects")]
    public Color objectColor = new Color(0f, 1f, 0.5f, 0.7f);

    [Tooltip("Color for blobs")]
    public Color blobColor = new Color(0.5f, 0.5f, 1f, 0.7f);

    // Dictionary to store visualization GameObjects for cursors
    private Dictionary<long, GameObject> cursorObjects = new Dictionary<long, GameObject>();

    // Dictionary to store visualization GameObjects for objects
    private Dictionary<long, GameObject> tuioObjects = new Dictionary<long, GameObject>();

    // Dictionary to store visualization GameObjects for blobs
    private Dictionary<long, GameObject> blobObjects = new Dictionary<long, GameObject>();

    private void Start()
    {
        if (!usebroker && tuioManager == null)
        {
            Debug.LogWarning("No TUIOManager assigned. TUIOVisualizer will not work in direct mode.");
        }

        // Create default prefabs if none provided
        if (cursorPrefab == null)
        {
            cursorPrefab = CreateDefaultPrefab("DefaultCursor", cursorColor);
        }

        if (objectPrefab == null && showObjects)
        {
            objectPrefab = CreateDefaultPrefab("DefaultObject", objectColor);
        }

        if (blobPrefab == null && showBlobs)
        {
            blobPrefab = CreateDefaultPrefab("DefaultBlob", blobColor);
        }

        // Connect to events
        if (usebroker)
        {
            // Register with TUIOBroker
            TUIOBroker.RegisterTUIOReceiver(this);
        }
        else if (tuioManager != null)
        {
            // Register with direct events from TUIOManager
            tuioManager.OnNewContainer += OnNewTUIOContainer;
            tuioManager.OnUpdateContainer += OnUpdateTUIOContainer;
            tuioManager.OnRemoveContainer += OnRemoveTUIOContainer;
        }
    }

    private void Update()
    {
        // Update all visualizations from current TUIO state
        if (this.tuioManager != null)
        {
            // Update cursor positions
            foreach (var cursor in this.tuioManager.GetAllCursors())
            {
                if (cursorObjects.TryGetValue(cursor.SessionID, out GameObject cursorObject) && cursorObject != null)
                {
                    // Update position
                    Vector3 screenPos = ConvertToScreenCoordinates(cursor.X, cursor.Y);
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                    worldPos.z = 0; // Ensure z position is 0
                    cursorObject.transform.position = worldPos;

                    // Update debug visualization if enabled
                    if (showDebugInfo)
                    {
                        UpdateDebugVisualization(cursorObject, cursor);
                    }
                }
            }

            // Update object positions (similar to cursors)
            foreach (var tobj in this.tuioManager.GetAllObjects())
            {
                if (tuioObjects.TryGetValue(tobj.SessionID, out GameObject objObject) && objObject != null)
                {
                    // Update position
                    Vector3 screenPos = ConvertToScreenCoordinates(tobj.X, tobj.Y);
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                    worldPos.z = 0;
                    objObject.transform.position = worldPos;
                    objObject.transform.rotation = Quaternion.Euler(0, 0, tobj.Angle * Mathf.Rad2Deg);

                    // Update debug visualization if enabled
                    if (showDebugInfo)
                    {
                        UpdateDebugVisualization(objObject, tobj);
                    }
                }
            }

            // Update blob positions
            foreach (var tblb in this.tuioManager.GetAllBlobs())
            {
                if (blobObjects.TryGetValue(tblb.SessionID, out GameObject blobObject) && blobObject != null)
                {
                    // Update position
                    Vector3 screenPos = ConvertToScreenCoordinates(tblb.X, tblb.Y);
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                    worldPos.z = 0;
                    blobObject.transform.position = worldPos;
                    blobObject.transform.rotation = Quaternion.Euler(0, 0, tblb.Angle * Mathf.Rad2Deg);

                    // Update scale
                    float scaleX = tblb.Width * blobSize;
                    float scaleY = tblb.Height * blobSize;
                    blobObject.transform.localScale = new Vector3(scaleX, scaleY, 1);

                    // Update debug visualization if enabled
                    if (showDebugInfo)
                    {
                        UpdateDebugVisualization(blobObject, tblb);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Disconnect from events
        if (usebroker)
        {
            TUIOBroker.UnregisterTUIOReceiver(this);
        }
        else if (tuioManager != null)
        {
            tuioManager.OnNewContainer -= OnNewTUIOContainer;
            tuioManager.OnUpdateContainer -= OnUpdateTUIOContainer;
            tuioManager.OnRemoveContainer -= OnRemoveTUIOContainer;
        }

        // Clean up cursor visualizations
        foreach (var cursor in cursorObjects)
        {
            if (cursor.Value != null)
            {
                Destroy(cursor.Value);
            }
        }
        cursorObjects.Clear();

        // Clean up object visualizations
        foreach (var obj in tuioObjects)
        {
            if (obj.Value != null)
            {
                Destroy(obj.Value);
            }
        }
        tuioObjects.Clear();

        // Clean up blob visualizations
        foreach (var blob in blobObjects)
        {
            if (blob.Value != null)
            {
                Destroy(blob.Value);
            }
        }
        blobObjects.Clear();
    }

    #region ITUIOReceiver Implementation
    public void OnNewTUIOContainer(TuioContainer container)
    {
        if (container is TuioCursor && showCursors)
        {
            VisualizeCursor(container as TuioCursor, true);
        }
        else if (container is TuioObject && showObjects)
        {
            VisualizeObject(container as TuioObject, true);
        }
        else if (container is TuioBlob && showBlobs)
        {
            VisualizeBlob(container as TuioBlob, true);
        }
    }

    public void OnUpdateTUIOContainer(TuioContainer container)
    {
        // Log for debugging update events (uncomment if needed)
        // Debug.Log($"TUIOVisualizer received update event for container ID: {container.SessionID}");

        if (container is TuioCursor && showCursors)
        {
            // Force visualization update
            TuioCursor tcur = container as TuioCursor;

            // Check if this cursor exists in our dictionary
            if (cursorObjects.TryGetValue(tcur.SessionID, out GameObject cursorObject) && cursorObject != null)
            {
                // Update position directly
                Vector3 screenPos = ConvertToScreenCoordinates(tcur.X, tcur.Y);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                worldPos.z = 0; // Ensure z position is 0
                cursorObject.transform.position = worldPos;

                // Update debug visualization if enabled
                if (showDebugInfo)
                {
                    UpdateDebugVisualization(cursorObject, tcur);
                }
            }
            else
            {
                // If not found in dictionary, create new visualization
                VisualizeCursor(tcur, true);
            }
        }
        else if (container is TuioObject && showObjects)
        {
            VisualizeObject(container as TuioObject, false);
        }
        else if (container is TuioBlob && showBlobs)
        {
            VisualizeBlob(container as TuioBlob, false);
        }
    }

    public void OnRemoveTUIOContainer(TuioContainer container)
    {
        if (container is TuioCursor && showCursors)
        {
            RemoveCursorVisualization(container as TuioCursor);
        }
        else if (container is TuioObject && showObjects)
        {
            RemoveObjectVisualization(container as TuioObject);
        }
        else if (container is TuioBlob && showBlobs)
        {
            RemoveBlobVisualization(container as TuioBlob);
        }
    }
    #endregion

    #region Visualization Methods

    private void VisualizeCursor(TuioCursor tcur, bool isNew)
    {
        GameObject cursorObject;

        // Create new cursor visualization if it's new
        if (isNew)
        {
            cursorObject = Instantiate(cursorPrefab);
            cursorObject.name = $"Cursor_{tcur.SessionID}";
            cursorObject.transform.localScale = new Vector3(cursorSize, cursorSize, cursorSize);
            cursorObject.SetActive(true);

            // Store the cursor object
            cursorObjects[tcur.SessionID] = cursorObject;

            // Add debug visualization if enabled
            if (showDebugInfo)
            {
                AddDebugVisualization(cursorObject, tcur);
            }

            // Debug.Log($"Visualizer: Cursor added: ID={tcur.SessionID}, Position=({tcur.X}, {tcur.Y})");
        }
        else
        {
            // Get existing cursor visualization
            if (!cursorObjects.TryGetValue(tcur.SessionID, out cursorObject) || cursorObject == null)
            {
                return;
            }
        }

        // Position the cursor at screen coordinates
        Vector3 screenPos = ConvertToScreenCoordinates(tcur.X, tcur.Y);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0; // Ensure z position is 0
        cursorObject.transform.position = worldPos;

        // Update debug visualization if enabled
        if (showDebugInfo)
        {
            UpdateDebugVisualization(cursorObject, tcur);
        }

        // Debug log update for troubleshooting (uncomment if needed)
        // Debug.Log($"Visualizer: Cursor updated: ID={tcur.SessionID}, Position=({tcur.X:F2}, {tcur.Y:F2}) at {Time.frameCount}");
    }

    private void VisualizeObject(TuioObject tobj, bool isNew)
    {
        GameObject objObject;

        // Create new object visualization if it's new
        if (isNew)
        {
            objObject = Instantiate(objectPrefab);
            objObject.name = $"Object_{tobj.SessionID}";
            objObject.transform.localScale = new Vector3(objectSize, objectSize, objectSize);
            objObject.SetActive(true);

            // Store the object 
            tuioObjects[tobj.SessionID] = objObject;

            // Add debug visualization if enabled
            if (showDebugInfo)
            {
                AddDebugVisualization(objObject, tobj);
            }

            Debug.Log($"Visualizer: Object added: ID={tobj.SessionID}, SymbolID={tobj.SymbolID}, Position=({tobj.X}, {tobj.Y})");
        }
        else
        {
            // Get existing object visualization
            if (!tuioObjects.TryGetValue(tobj.SessionID, out objObject) || objObject == null)
            {
                return;
            }
        }

        // Position the object at screen coordinates
        Vector3 screenPos = ConvertToScreenCoordinates(tobj.X, tobj.Y);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        objObject.transform.position = worldPos;

        // Rotate the object
        objObject.transform.rotation = Quaternion.Euler(0, 0, tobj.Angle * Mathf.Rad2Deg);

        // Update debug visualization if enabled
        if (showDebugInfo)
        {
            UpdateDebugVisualization(objObject, tobj);
        }
    }

    private void VisualizeBlob(TuioBlob tblb, bool isNew)
    {
        GameObject blobObject;

        // Create new blob visualization if it's new
        if (isNew)
        {
            blobObject = Instantiate(blobPrefab);
            blobObject.name = $"Blob_{tblb.SessionID}";

            // Scale based on blob dimensions
            float scaleX = tblb.Width * blobSize;
            float scaleY = tblb.Height * blobSize;
            blobObject.transform.localScale = new Vector3(scaleX, scaleY, 1);
            blobObject.SetActive(true);

            // Store the blob object
            blobObjects[tblb.SessionID] = blobObject;

            // Add debug visualization if enabled
            if (showDebugInfo)
            {
                AddDebugVisualization(blobObject, tblb);
            }

            Debug.Log($"Visualizer: Blob added: ID={tblb.SessionID}, Position=({tblb.X}, {tblb.Y}), Size=({tblb.Width}, {tblb.Height})");
        }
        else
        {
            // Get existing blob visualization
            if (!blobObjects.TryGetValue(tblb.SessionID, out blobObject) || blobObject == null)
            {
                return;
            }

            // Update scale based on blob dimensions
            float scaleX = tblb.Width * blobSize;
            float scaleY = tblb.Height * blobSize;
            blobObject.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }

        // Position the blob at screen coordinates
        Vector3 screenPos = ConvertToScreenCoordinates(tblb.X, tblb.Y);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        blobObject.transform.position = worldPos;

        // Rotate the blob
        blobObject.transform.rotation = Quaternion.Euler(0, 0, tblb.Angle * Mathf.Rad2Deg);

        // Update debug visualization if enabled
        if (showDebugInfo)
        {
            UpdateDebugVisualization(blobObject, tblb);
        }
    }

    private void RemoveCursorVisualization(TuioCursor tcur)
    {
        if (cursorObjects.TryGetValue(tcur.SessionID, out GameObject cursorObject))
        {
            if (cursorObject != null)
            {
                Destroy(cursorObject);
            }
            cursorObjects.Remove(tcur.SessionID);

            Debug.Log($"Visualizer: Cursor removed: ID={tcur.SessionID}");
        }
    }

    private void RemoveObjectVisualization(TuioObject tobj)
    {
        if (tuioObjects.TryGetValue(tobj.SessionID, out GameObject objObject))
        {
            if (objObject != null)
            {
                Destroy(objObject);
            }
            tuioObjects.Remove(tobj.SessionID);

            Debug.Log($"Visualizer: Object removed: ID={tobj.SessionID}, SymbolID={tobj.SymbolID}");
        }
    }

    private void RemoveBlobVisualization(TuioBlob tblb)
    {
        if (blobObjects.TryGetValue(tblb.SessionID, out GameObject blobObject))
        {
            if (blobObject != null)
            {
                Destroy(blobObject);
            }
            blobObjects.Remove(tblb.SessionID);

            Debug.Log($"Visualizer: Blob removed: ID={tblb.SessionID}");
        }
    }

    #endregion

    #region ImGUI Visualization

    /// <summary>
    /// Draw TUIO cursors on the screen using immediate mode GUI
    /// </summary>
    private void DrawTUIOCursors()
    {
        if (this.tuioManager == null || !showFullscreenCursors)
            return;

        // Draw all cursors at their current screen positions
        foreach (var cursor in this.tuioManager.GetAllCursors())
        {
            // Get screen position from TUIO coordinates
            Vector2 screenPos = ConvertToScreenCoordinates(cursor.X, cursor.Y);

            // Draw cursor as a circle
            if (Event.current.type == EventType.Repaint)
            {
                // Create special texture for the cursor if needed (better than using a white texture)
                Texture2D cursorTex = GetCursorTexture();

                // Draw a circle for the cursor using the circular texture
                float size = 150f;
                GUI.color = cursorColor;
                GUI.DrawTexture(new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size), cursorTex);

                // Draw cursor ID next to the circle
                GUI.color = Color.red;
                int oldSize = GUI.skin.label.fontSize;
                GUI.skin.label.fontSize = 80;  // doppelt so groß z. B.
                GUI.Label(new Rect(screenPos.x + size / 2, screenPos.y - size / 2, 200, 100), cursor.SessionID.ToString());
                GUI.skin.label.fontSize = oldSize; // wieder zurücksetzen!

                // Draw speed indicator
                if (cursor.MotionSpeed > 0.01f)
                {
                    float speedSize = Mathf.Clamp(cursor.MotionSpeed * 200, 5, 50);
                    GUI.color = new Color(1f, 0.5f, 0f, 0.7f); // Orange
                    GUI.DrawTexture(new Rect(screenPos.x - speedSize / 2, screenPos.y - speedSize / 2, speedSize, speedSize), cursorTex);
                }

                GUI.color = Color.white;
            }
        }
    }

    // Cache for cursor texture
    private Texture2D cursorTexture;

    /// <summary>
    /// Get a circular texture for cursor drawing
    /// </summary>
    private Texture2D GetCursorTexture()
    {
        if (cursorTexture == null)
        {
            cursorTexture = CreateCircleTexture(150);
        }
        return cursorTexture;
    }

    /// <summary>
    /// Create a circular texture for cursor drawing
    /// </summary>
    private Texture2D CreateCircleTexture(int size)
    {
        // Kein MipChain -> vermeidet zusätzliches Weichzeichnen
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, /*mipChain:*/ false);
        float radius = size / 2f;
        float r2     = radius * radius;
        Vector2 c    = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d2 = (new Vector2(x, y) - c).sqrMagnitude;
            if (d2 <= r2)
            {
                // kleiner, dezenter Rand-Falloff (1-2 Pixel), damit es nicht pixelig wirkt, obwohl FilterMode.Point aktiv ist
                float d       = Mathf.Sqrt(d2);
                float edgePx  = 1.5f;                           // Randbreite in Pixeln
                float alpha   = Mathf.Clamp01((radius - d) / edgePx);
                if (d < radius - edgePx) alpha = 1f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            else
            {
                texture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }
        texture.Apply(/*updateMipmaps:*/ false, /*makeNoLongerReadable:*/ false);
        return texture;
    }

    private void OnGUI()
    {
        if (!showImGuiOverlay)
            return;

        // First, draw all cursor dots (outside of the BeginArea to allow full screen drawing)
        DrawTUIOCursors();

        // Draw ImGUI window with cursor information
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        GUILayout.BeginVertical("TUIO Input Tracker", GUI.skin.window);

        // Add toggle for visualization modes
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Visualization Options:");

        bool newShowImGuiOverlay = GUILayout.Toggle(showImGuiOverlay, "Show ImGUI Overlay");
        if (newShowImGuiOverlay != showImGuiOverlay)
        {
            showImGuiOverlay = newShowImGuiOverlay;
        }

        bool newShowFullscreenCursors = GUILayout.Toggle(showFullscreenCursors, "Show Fullscreen Cursors");
        if (newShowFullscreenCursors != showFullscreenCursors)
        {
            showFullscreenCursors = newShowFullscreenCursors;
        }

        bool newShowDebugInfo = GUILayout.Toggle(showDebugInfo, "Show 3D Debug Info");
        if (newShowDebugInfo != showDebugInfo)
        {
            showDebugInfo = newShowDebugInfo;

            // Update all visualizations if debug info changed
            foreach (var cursor in cursorObjects)
            {
                if (cursor.Value != null)
                {
                    Transform textTransform = cursor.Value.transform.Find($"DebugText_{cursor.Key}");
                    if (textTransform != null)
                    {
                        textTransform.gameObject.SetActive(showDebugInfo);
                    }

                    LineRenderer lineRenderer = cursor.Value.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = showDebugInfo && showCursorTrails;
                    }
                }
            }
        }

        bool newShowCursorTrails = GUILayout.Toggle(showCursorTrails, "Show Cursor Trails");
        if (newShowCursorTrails != showCursorTrails)
        {
            showCursorTrails = newShowCursorTrails;

            // Update all line renderers if cursor trails changed
            foreach (var cursor in cursorObjects)
            {
                if (cursor.Value != null)
                {
                    LineRenderer lineRenderer = cursor.Value.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = showDebugInfo && showCursorTrails;
                    }
                }
            }
        }

        GUILayout.EndVertical();

        GUILayout.Label($"Active Cursors: {cursorObjects.Count}");
        GUILayout.Label($"Active Objects: {tuioObjects.Count}");
        GUILayout.Label($"Active Blobs: {blobObjects.Count}");

        GUILayout.Space(10);

        // Display cursor details
        if (cursorObjects.Count > 0 && showCursors)
        {
            GUILayout.Label("Cursors:", GUI.skin.box);

            foreach (var cursorPair in cursorObjects)
            {
                if (cursorPair.Value == null) continue;

                // Get cursor data from TUIOManager if available
                TuioCursor tcur = null;
                if (this.tuioManager != null)
                {
                    foreach (var cursor in this.tuioManager.GetAllCursors())
                    {
                        if (cursor.SessionID == cursorPair.Key)
                        {
                            tcur = cursor;
                            break;
                        }
                    }
                }

                // Get screen position either from cursor data or from GameObject
                Vector2 screenPos;
                if (tcur != null)
                {
                    // Use actual cursor data for position (more accurate)
                    screenPos = ConvertToScreenCoordinates(tcur.X, tcur.Y);
                }
                else
                {
                    // Fallback to GameObject position
                    Vector3 worldScreenPos = Camera.main.WorldToScreenPoint(cursorPair.Value.transform.position);
                    screenPos = new Vector2(worldScreenPos.x, worldScreenPos.y);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"ID: {cursorPair.Key}");
                GUILayout.Label($"Pos: {screenPos.x:F0}, {screenPos.y:F0}");
                GUILayout.EndHorizontal();

                // Cursor dots are now drawn in the DrawTUIOCursors method
                // No need to draw them here
            }

            GUILayout.Space(10);
        }

        // Display object details
        if (tuioObjects.Count > 0 && showObjects)
        {
            GUILayout.Label("Objects:", GUI.skin.box);

            foreach (var objPair in tuioObjects)
            {
                if (objPair.Value == null) continue;

                TuioObject tobj = null;
                if (this.tuioManager != null && this.tuioManager.client != null)
                {
                    foreach (var obj in this.tuioManager.GetAllObjects())
                    {
                        if (obj.SessionID == objPair.Key)
                        {
                            tobj = obj;
                            break;
                        }
                    }
                }

                if (tobj != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"ID: {tobj.SessionID}");
                    GUILayout.Label($"Symbol: {tobj.SymbolID}");
                    GUILayout.Label($"Angle: {tobj.Angle * Mathf.Rad2Deg:F0}°");
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
        }

        // Display blob details
        if (blobObjects.Count > 0 && showBlobs)
        {
            GUILayout.Label("Blobs:", GUI.skin.box);

            foreach (var blobPair in blobObjects)
            {
                if (blobPair.Value == null) continue;

                TuioBlob tblb = null;
                if (this.tuioManager != null && this.tuioManager.client != null)
                {
                    foreach (var blob in this.tuioManager.GetAllBlobs())
                    {
                        if (blob.SessionID == blobPair.Key)
                        {
                            tblb = blob;
                            break;
                        }
                    }
                }

                if (tblb != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"ID: {tblb.SessionID}");
                    GUILayout.Label($"Size: {tblb.Width:F2} x {tblb.Height:F2}");
                    GUILayout.Label($"Area: {tblb.Area:F2}");
                    GUILayout.EndHorizontal();
                }
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a default prefab for visualization
    /// </summary>
    private GameObject CreateDefaultPrefab(string name, Color color)
    {
        GameObject prefab = new GameObject(name);
        SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = color;
        prefab.SetActive(false);
        return prefab;
    }

    /// <summary>
    /// Creates a simple circle sprite for visualization
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        // Create a texture for the sprite
        int size = 64;
        Texture2D texture = new Texture2D(size, size);

        // Fill the texture with a circle
        Color fillColor = Color.white;
        Color clearColor = new Color(0f, 0f, 0f, 0f);

        float radius = size / 2f;
        float radiusSquared = radius * radius;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distanceSquared = (new Vector2(x, y) - center).sqrMagnitude;
                if (distanceSquared <= radiusSquared)
                {
                    texture.SetPixel(x, y, fillColor);
                }
                else
                {
                    texture.SetPixel(x, y, clearColor);
                }
            }
        }

        texture.Apply();

        // Create a sprite from the texture
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.Tight
        );
    }

    /// <summary>
    /// Converts TUIO coordinates (0.0-1.0) to screen coordinates
    /// </summary>
    private Vector2 ConvertToScreenCoordinates(float x, float y)
    {
        // TUIO standard coordinates: 
        // - x is 0.0-1.0 (left to right)
        // - y is 0.0-1.0 (bottom to top)

        // Use X directly (no mirroring)
        float screenX = Mathf.Clamp01(x) * Screen.width;

        // In Unity screen coordinates, y=0 is top, y=Screen.height is bottom,
        // so we need to flip the Y-axis from the TUIO coordinate system
        // float screenY = (1.0f - Mathf.Clamp01(y)) * Screen.height;
        float screenY = Mathf.Clamp01(y) * Screen.height;

        // Debug to check coordinate mapping (uncomment for detailed logging)
        // Debug.Log($"TUIO ({x:F3}, {y:F3}) -> Screen ({screenX:F0}, {screenY:F0})");

        return new Vector2(screenX, screenY);
    }

    /// <summary>
    /// Adds debug visualization (text label, line renderer) to a container
    /// </summary>
    private void AddDebugVisualization(GameObject containerObject, TuioContainer container)
    {
        // 1. Add a line renderer to visualize path
        LineRenderer lineRenderer = containerObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 1;
        lineRenderer.enabled = showDebugInfo && showCursorTrails;

        // Get current position
        Vector3 screenPos = ConvertToScreenCoordinates(container.X, container.Y);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0; // Ensure it's visible

        lineRenderer.SetPosition(0, worldPos);
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Different colors for different container types
        if (container is TuioCursor)
        {
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.yellow;
        }
        else if (container is TuioObject)
        {
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.cyan;
        }
        else if (container is TuioBlob)
        {
            lineRenderer.startColor = Color.blue;
            lineRenderer.endColor = Color.magenta;
        }

        lineRenderer.alignment = LineAlignment.View; // Make line face the camera

        // 2. Add a text label with position info
        GameObject textObj = new GameObject($"DebugText_{container.SessionID}");
        textObj.transform.SetParent(containerObject.transform);
        textObj.transform.localPosition = Vector3.up * 0.1f;

        // Create a TextMesh for displaying position
        TextMesh textMesh = textObj.AddComponent<TextMesh>();

        // Different info depending on the container type
        if (container is TuioCursor)
        {
            TuioCursor cursor = container as TuioCursor;
            textMesh.text = $"Cursor ID: {cursor.SessionID}\n" +
                          $"Pos: ({cursor.X:F2}, {cursor.Y:F2})";
        }
        else if (container is TuioObject)
        {
            TuioObject obj = container as TuioObject;
            textMesh.text = $"Object ID: {obj.SessionID}\n" +
                          $"Symbol: {obj.SymbolID}\n" +
                          $"Pos: ({obj.X:F2}, {obj.Y:F2})";
        }
        else if (container is TuioBlob)
        {
            TuioBlob blob = container as TuioBlob;
            textMesh.text = $"Blob ID: {blob.SessionID}\n" +
                          $"Size: ({blob.Width:F2}, {blob.Height:F2})\n" +
                          $"Pos: ({blob.X:F2}, {blob.Y:F2})";
        }

        textMesh.fontSize = 24;
        textMesh.characterSize = 0.025f;
        textMesh.anchor = TextAnchor.LowerCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // Make text face camera
        textObj.AddComponent<Billboard>();
    }

    /// <summary>
    /// Updates debug visualization for a container
    /// </summary>
    private void UpdateDebugVisualization(GameObject containerObject, TuioContainer container)
    {
        // Get current position
        Vector3 screenPos = ConvertToScreenCoordinates(container.X, container.Y);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0; // Ensure it's visible

        // Update line renderer if it exists
        LineRenderer lineRenderer = containerObject.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            // Add new point to line renderer (limit to 100 points max)
            int count = Mathf.Min(lineRenderer.positionCount + 1, 100);
            Vector3[] positions = new Vector3[count];

            // Shift existing positions
            if (lineRenderer.positionCount > 0)
            {
                lineRenderer.GetPositions(positions);
                for (int i = count - 1; i > 0; i--)
                {
                    positions[i] = positions[i - 1];
                }
            }

            // Add current position at start
            positions[0] = worldPos;

            // Update line renderer
            lineRenderer.positionCount = count;
            lineRenderer.SetPositions(positions);
        }

        // Update text information
        Transform textObjTransform = containerObject.transform.Find($"DebugText_{container.SessionID}");
        if (textObjTransform != null)
        {
            TextMesh textMesh = textObjTransform.GetComponent<TextMesh>();
            if (textMesh != null)
            {
                // Different info depending on the container type
                if (container is TuioCursor)
                {
                    TuioCursor cursor = container as TuioCursor;
                    textMesh.text = $"Cursor ID: {cursor.SessionID}\n" +
                                  $"Pos: ({cursor.X:F2}, {cursor.Y:F2})\n" +
                                  $"Speed: {cursor.MotionSpeed:F3}";
                }
                else if (container is TuioObject)
                {
                    TuioObject obj = container as TuioObject;
                    textMesh.text = $"Object ID: {obj.SessionID}\n" +
                                  $"Symbol: {obj.SymbolID}\n" +
                                  $"Pos: ({obj.X:F2}, {obj.Y:F2})\n" +
                                  $"Angle: {obj.Angle:F2}";
                }
                else if (container is TuioBlob)
                {
                    TuioBlob blob = container as TuioBlob;
                    textMesh.text = $"Blob ID: {blob.SessionID}\n" +
                                  $"Size: ({blob.Width:F2}, {blob.Height:F2})\n" +
                                  $"Pos: ({blob.X:F2}, {blob.Y:F2})\n" +
                                  $"Area: {blob.Area:F2}";
                }
            }
        }
    }

    #endregion
}