using UnityEngine;

public class DeathMarker : MonoBehaviour
{
    [SerializeField] float lifetime = 300f;

    float _spawnTime;
    float _bobPhase;

    // Placeholder — will hold real inventory data once inventory system exists
    // For now, just stores that a death happened here
    public Vector3 DeathPosition { get; private set; }

    public void Init(Vector3 position)
    {
        DeathPosition = position;
        transform.position = position + Vector3.up * 0.5f;
        _spawnTime = Time.time;
        _bobPhase = Random.value * Mathf.PI * 2f;
        BuildVisual();
    }

    void Update()
    {
        if (Time.time - _spawnTime > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float bob = Mathf.Sin(Time.time * 2f + _bobPhase) * 0.15f;
        transform.position = DeathPosition + Vector3.up * (0.5f + bob);
        transform.Rotate(Vector3.up, 45f * Time.deltaTime);
    }

    void BuildVisual()
    {
        // Glowing cross marker — two intersecting quads
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", new Color(0.9f, 0.2f, 0.15f, 0.9f));
        mat.SetFloat("_Surface", 1f); // transparent
        mat.SetFloat("_Blend", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;

        // Vertical beam
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(beam.GetComponent<Collider>());
        beam.transform.SetParent(transform, false);
        beam.transform.localScale = new Vector3(0.15f, 1.5f, 0.15f);
        beam.transform.localPosition = Vector3.up * 0.5f;
        beam.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Cross arm
        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(arm.GetComponent<Collider>());
        arm.transform.SetParent(transform, false);
        arm.transform.localScale = new Vector3(0.8f, 0.12f, 0.12f);
        arm.transform.localPosition = Vector3.up * 1.1f;
        arm.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Base skull sphere
        var skull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(skull.GetComponent<Collider>());
        skull.transform.SetParent(transform, false);
        skull.transform.localScale = Vector3.one * 0.3f;
        skull.transform.localPosition = Vector3.zero;
        skull.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    public float TimeRemaining => Mathf.Max(0f, lifetime - (Time.time - _spawnTime));
}
