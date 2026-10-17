
using UnityEngine;

public class LayoutNameTags : MonoBehaviour
{
    [ContextMenu("Create Name Tags")]
    public void CreateNameTags()
    {
        Transform layout = transform.Find("Layout_Placeholders");

        if (layout == null)
        {
            Debug.LogError("Layout_Placeholders not found!");
            return;
        }

        // Remove previously generated name tags.
        for (int i = layout.childCount - 1; i >= 0; i--)
        {
            Transform child = layout.GetChild(i);

            if (child.name.StartsWith("NameTag_"))
                DestroyImmediate(child.gameObject);
        }

        // Snapshot the layout objects before creating new tags.
        int count = layout.childCount;

        for (int i = 0; i < count; i++)
        {
            Transform item = layout.GetChild(i);

            // Skip all individual trees, trunks, and crowns.
            if (item.name.StartsWith("Fruit_Tree_") ||
                item.name.StartsWith("Boundary_Tree_"))
                continue;

            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                continue;

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);

            // Use the object's horizontal footprint for dimensions.
            float length = bounds.size.x;
            float width = bounds.size.z;

            string label = item.name.Replace("_", " ");

            // Display the longer horizontal dimension first.
            float longSide = Mathf.Max(length, width);
            float shortSide = Mathf.Min(length, width);

            label += "\n(" + longSide.ToString("0.0") +
                     " m × " + shortSide.ToString("0.0") + " m)";

            GameObject tag = new GameObject("NameTag_" + item.name);
            tag.transform.SetParent(layout, true);

            tag.transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + 1.2f,
                bounds.center.z
            );

            // Background panel.
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "Background";
            panel.transform.SetParent(tag.transform, false);
            panel.transform.localPosition = Vector3.zero;

            float panelWidth = Mathf.Max(3.0f, label.Length * 0.075f);
            panel.transform.localScale = new Vector3(panelWidth, 1.15f, 1f);

            Collider panelCollider = panel.GetComponent<Collider>();
            if (panelCollider != null)
                DestroyImmediate(panelCollider);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            Material bgMaterial = new Material(shader);
            bgMaterial.color = new Color(0.04f, 0.07f, 0.09f, 0.95f);
            panel.GetComponent<Renderer>().material = bgMaterial;

            // Text.
            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(tag.transform, false);
            textObject.transform.localPosition = new Vector3(0, 0, -0.02f);

            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = label;
            text.fontSize = 96;
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
        }

        Debug.Log("Updated name tags with dimensions; tree tags skipped.");
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

        foreach (Transform child in layout)
        {
            if (child.name.StartsWith("NameTag_"))
            {
                child.rotation = Quaternion.LookRotation(
                    child.position - cam.transform.position,
                    Vector3.up
                );
            }
        }
    }
}
