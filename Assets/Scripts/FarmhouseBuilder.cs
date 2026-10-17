using UnityEngine;

public class FarmhouseBuilder : MonoBehaviour
{
    [Header("Plot dimensions in metres")]
    public float plotWidth = 45.9f;
    public float plotDepth = 35.8f;
    public float groundThickness = 0.3f;

    [Header("Ground appearance")]
    public Color groundColor = new Color(0.30f, 0.38f, 0.22f);

    [ContextMenu("Build Farmhouse Plot")]
    public void BuildPlot()
    {
        GameObject existing = GameObject.Find("Ground_Base");

        if (existing != null)
        {
            DestroyImmediate(existing);
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ground.name = "Ground_Base";

        ground.transform.position = new Vector3(
            plotWidth / 2f,
            -groundThickness / 2f,
            plotDepth / 2f
        );

        ground.transform.localScale = new Vector3(
            plotWidth,
            groundThickness,
            plotDepth
        );

        Renderer renderer = ground.GetComponent<Renderer>();

        Material material = new Material(
            Shader.Find("Universal Render Pipeline/Lit")
        );

        material.color = groundColor;
        renderer.material = material;

        Debug.Log(
            "Farmhouse plot created: " +
            plotWidth + "m x " +
            plotDepth + "m"
        );
    }
}