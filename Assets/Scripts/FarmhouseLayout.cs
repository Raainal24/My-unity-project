using System.Collections.Generic;
using UnityEngine;

public class LayoutNameTags : MonoBehaviour
{
    [Header("Text size")]
    [Tooltip("Height of one line of text in metres. Smaller = smaller tags.")]
    public float textSize = 0.28f;
    [Tooltip("Width of one character relative to textSize. Raise it if text spills out of the panel.")]
    public float charWidthFactor = 0.55f;

    [Header("Placement")]
    [Tooltip("Lowest a tag may float (keeps tags above eye level in first person).")]
    public float minHeightAboveGround = 2.4f;
    public float gapAboveObject = 0.8f;
    [Tooltip("Extra vertical gap when two tags have to be stacked.")]
    public float stackSpacing = 0.15f;
    public bool showLeaderLines = true;

    [Header("Visibility in play mode")]
    [Tooltip("Tags farther than this from the camera are hidden. 0 = always show.")]
    public float maxVisibleDistance = 22f;

    // Children of Layout_Placeholders that never get a tag
    static readonly HashSet<string> SkipNames = new HashSet<string>
    {
        "Trees_Group",
        "Plants_Group",
        "West_Road"
    };

    class Entry
    {
        public Transform item;
        public string label;
        public float x, z;       // tag centre on the ground plane
        public float topY;       // top of the object
        public float y;          // final tag centre height
        public float panelW, panelH;
    }

    [ContextMenu("Create Name Tags")]
    public void CreateNameTags()
    {
        Transform layout = transform.Find("Layout_Placeholders");

        if (layout == null)
        {
            Debug.LogError("Layout_Placeholders not found!");
            return;
        }

        // Remove previously generated tags and leader lines.
        for (int i = layout.childCount - 1; i >= 0; i--)
        {
            Transform child = layout.GetChild(i);

            if (child.name.StartsWith("NameTag"))
                DestroyImmediate(child.gameObject);
        }

        // ---------- 1. Measure every object ----------
        List<Entry> entries = new List<Entry>();
        int count = layout.childCount;

        for (int i = 0; i < count; i++)
        {
            Transform item = layout.GetChild(i);

            if (SkipNames.Contains(item.name) || item.name.StartsWith("NameTag"))
                continue;

            Renderer[] allRenderers = item.GetComponentsInChildren<Renderer>();
            if (allRenderers.Length == 0)
                continue;

            // Everything (so tags sit above roofs, beams, etc.)
            Bounds fullBounds = allRenderers[0].bounds;
            foreach (Renderer r in allRenderers)
                fullBounds.Encapsulate(r.bounds);

            // Only the zone itself (root renderer or "Visual" child) for dimensions
            bool haveFootprint = false;
            Bounds footprint = new Bounds();

            foreach (Renderer r in allRenderers)
            {
                if (r.gameObject != item.gameObject && r.name != "Visual")
                    continue;

                if (!haveFootprint)
                {
                    footprint = r.bounds;
                    haveFootprint = true;
                }
                else
                {
                    footprint.Encapsulate(r.bounds);
                }
            }

            if (!haveFootprint)
                footprint = fullBounds;   // e.g. Front_Gate

            float longSide = Mathf.Max(footprint.size.x, footprint.size.z);
            float shortSide = Mathf.Min(footprint.size.x, footprint.size.z);

            // Curved paths: true centre-line length and width from the mesh
            MeshFilter mf = item.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Vector3[] v = mf.sharedMesh.vertices;

                if (v.Length >= 4 && v.Length % 2 == 0)
                {
                    float pathLength = 0f;
                    Vector3 prev = (v[0] + v[1]) * 0.5f;

                    for (int k = 2; k < v.Length; k += 2)
                    {
                        Vector3 c = (v[k] + v[k + 1]) * 0.5f;
                        pathLength += Vector3.Distance(prev, c);
                        prev = c;
                    }

                    longSide = pathLength;
                    shortSide = Vector3.Distance(v[0], v[1]);
                }
            }

            string title = item.name.Replace("_", " ");
            string dims = longSide.ToString("0.0") + " × " + shortSide.ToString("0.0") + " m";

            int longestLine = Mathf.Max(title.Length, dims.Length);

            Entry e = new Entry();
            e.item = item;
            e.label = title + "\n" + dims;
            e.x = fullBounds.center.x;
            e.z = fullBounds.center.z;
            e.topY = fullBounds.max.y;
            e.panelW = longestLine * textSize * charWidthFactor + textSize * 1.2f;
            e.panelH = 2f * textSize * 1.15f + textSize * 0.6f;

            // Lowest allowed centre: above the object AND above eye level
            float aboveObject = e.topY + gapAboveObject + e.panelH * 0.5f;
            float aboveEyes = minHeightAboveGround + e.panelH * 0.5f;
            e.y = Mathf.Max(aboveObject, aboveEyes);

            entries.Add(e);
        }

        // ---------- 2. Resolve clashes by stacking ----------
        List<Entry> placed = new List<Entry>();

        foreach (Entry e in entries)
        {
            for (int guard = 0; guard < 60; guard++)
            {
                bool clash = false;

                foreach (Entry p in placed)
                {
                    float dx = e.x - p.x;
                    float dz = e.z - p.z;
                    float horizontal = Mathf.Sqrt(dx * dx + dz * dz);
                    float needHorizontal = (e.panelW + p.panelW) * 0.5f;

                    float needVertical = (e.panelH + p.panelH) * 0.5f + stackSpacing;

                    if (horizontal < needHorizontal && Mathf.Abs(e.y - p.y) < needVertical)
                    {
                        // Push this tag up so it sits just above the one it hits
                        e.y = p.y + needVertical;
                        clash = true;
                        break;
                    }
                }

                if (!clash)
                    break;
            }

            placed.Add(e);
        }

        // ---------- 3. Build the tags ----------
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null)
            unlit = Shader.Find("Unlit/Color");

        Material panelMaterial = new Material(unlit);
        panelMaterial.color = new Color(0.04f, 0.07f, 0.09f, 0.95f);

        Material lineMaterial = new Material(unlit);
        lineMaterial.color = new Color(1f, 1f, 1f, 1f);

        foreach (Entry e in entries)
        {
            GameObject tag = new GameObject("NameTag_" + e.item.name);
            tag.transform.SetParent(layout, true);
            tag.transform.position = new Vector3(e.x, e.y, e.z);

            // Background panel.
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "Background";
            panel.transform.SetParent(tag.transform, false);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = new Vector3(e.panelW, e.panelH, 1f);

            Collider panelCollider = panel.GetComponent<Collider>();
            if (panelCollider != null)
                DestroyImmediate(panelCollider);

            panel.GetComponent<Renderer>().sharedMaterial = panelMaterial;

            // Text. With fontSize 100, one line is about characterSize * 10 metres tall.
            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(tag.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = e.label;
            text.fontSize = 100;
            text.characterSize = textSize * 0.1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;

            // Thin vertical line from the object up to its tag
            if (showLeaderLines)
            {
                float bottom = e.y - e.panelH * 0.5f;
                float length = bottom - e.topY;

                if (length > 0.05f)
                {
                    GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    line.name = "NameTagLine_" + e.item.name;
                    line.transform.SetParent(layout, true);
                    line.transform.position = new Vector3(e.x, e.topY + length * 0.5f, e.z);
                    line.transform.localScale = new Vector3(0.03f, length, 0.03f);

                    Collider lineCollider = line.GetComponent<Collider>();
                    if (lineCollider != null)
                        DestroyImmediate(lineCollider);

                    line.GetComponent<Renderer>().sharedMaterial = lineMaterial;
                }
            }
        }

        Debug.Log("Created " + entries.Count + " name tags (stacked to avoid clashes).");
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;

        if (cam == null)
            cam = FindFirstObjectByType<Camera>();

        if (cam == null)
            return;

        Transform layout = transform.Find("Layout_Placeholders");

        if (layout == null)
            return;

        const string linePrefix = "NameTagLine_";

        foreach (Transform child in layout)
        {
            if (child.name.StartsWith("NameTag_"))
            {
                Vector3 dir = child.position - cam.transform.position;

                bool visible = maxVisibleDistance <= 0f || dir.magnitude <= maxVisibleDistance;

                if (child.gameObject.activeSelf != visible)
                    child.gameObject.SetActive(visible);

                if (visible && dir.sqrMagnitude > 0.001f)
                    child.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            else if (child.name.StartsWith(linePrefix))
            {
                // Leader line follows the visibility of its tag
                Transform tag = layout.Find("NameTag_" + child.name.Substring(linePrefix.Length));
                bool visible = tag == null || tag.gameObject.activeSelf;

                if (child.gameObject.activeSelf != visible)
                    child.gameObject.SetActive(visible);
            }
        }
    }
}