using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField] UnitData militiaData;
    [SerializeField] Transform playerTransform;
    [SerializeField] SquadManager squadManager;
    [SerializeField] int friendlyCount = 4;
    [SerializeField] int enemyCount = 8;

    public void RespawnAll()
    {
        DestroyAllUnits();
        SpawnFriendlies();
        SpawnEnemies();
    }

    void DestroyAllUnits()
    {
        if (squadManager != null)
            squadManager.ResetSquad();

        var allAI = Object.FindObjectsOfType<UnitAI>();
        foreach (var ai in allAI)
        {
            if (ai != null && ai.gameObject != null)
                Destroy(ai.gameObject);
        }
    }

    void SpawnFriendlies()
    {
        if (militiaData == null || playerTransform == null) return;

        Vector3 spawn = playerTransform.position;

        for (int i = 0; i < friendlyCount; i++)
        {
            float angle = i * (360f / friendlyCount) * Mathf.Deg2Rad;
            float radius = 3f;
            float ux = spawn.x + Mathf.Sin(angle) * radius;
            float uz = spawn.z + Mathf.Cos(angle) * radius;
            float uy = WorldData.HeightSmooth(ux, uz);

            var unitGO = CreateUnitBody($"FriendlyUnit_{i}", new Vector3(ux, uy, uz),
                new Color(0.25f, 0.45f, 0.7f), new Color(0.35f, 0.55f, 0.8f), "MILITIA");

            unitGO.AddComponent<UnitHealth>();
            var ai = unitGO.AddComponent<UnitAI>();
            SetPrivateField(ai, "unitData", militiaData);
            SetPrivateField(ai, "followTarget", playerTransform);
            SetPrivateField(ai, "formationIndex", i);

            if (squadManager != null)
                squadManager.AddUnit(ai);
        }
    }

    // Arena center at (60, 60), enemies in north half
    static readonly Vector2[] EnemyPositions = {
        new Vector2(60f, 78f),   // north center — behind big boulder
        new Vector2(54f, 75f),   // north-left
        new Vector2(66f, 76f),   // north-right
        new Vector2(48f, 68f),   // far left flank
        new Vector2(72f, 66f),   // far right flank
        new Vector2(58f, 70f),   // mid-left
        new Vector2(65f, 72f),   // mid-right
        new Vector2(60f, 82f),   // back row, near north fence
    };

    void SpawnEnemies()
    {
        if (militiaData == null) return;

        int count = Mathf.Min(enemyCount, EnemyPositions.Length);

        for (int i = 0; i < count; i++)
        {
            float ex = EnemyPositions[i].x;
            float ez = EnemyPositions[i].y;
            float ey = WorldData.HeightSmooth(ex, ez);

            var unitGO = CreateUnitBody($"EnemyUnit_{i}", new Vector3(ex, ey, ez),
                new Color(0.7f, 0.2f, 0.15f), new Color(0.8f, 0.3f, 0.2f), "ENEMY");

            unitGO.AddComponent<EnemyTag>();
            unitGO.AddComponent<UnitHealth>();
            var ai = unitGO.AddComponent<UnitAI>();
            SetPrivateField(ai, "unitData", militiaData);
            SetPrivateField(ai, "formationIndex", i);
            SetPrivateField(ai, "initialState", UnitAI.UnitState.HoldPosition);
        }
    }

    static GameObject CreateUnitBody(string name, Vector3 position,
        Color bodyColor, Color armorColor, string label)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");

        var bodyMat = new Material(shader);
        bodyMat.SetColor("_BaseColor", bodyColor);

        var armorMat = new Material(shader);
        armorMat.SetColor("_BaseColor", armorColor);

        var unitGO = new GameObject(name);
        unitGO.transform.position = position;

        var cc = unitGO.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.25f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.3f;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(unitGO.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.4f, 0.5f, 0.3f);
        body.GetComponent<MeshRenderer>().material = armorMat;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        Object.Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(unitGO.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        head.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
        head.GetComponent<MeshRenderer>().material = bodyMat;

        var leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLeg.name = "LeftLeg";
        Object.Destroy(leftLeg.GetComponent<Collider>());
        leftLeg.transform.SetParent(unitGO.transform, false);
        leftLeg.transform.localPosition = new Vector3(-0.1f, 0.25f, 0f);
        leftLeg.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
        leftLeg.GetComponent<MeshRenderer>().material = bodyMat;

        var rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLeg.name = "RightLeg";
        Object.Destroy(rightLeg.GetComponent<Collider>());
        rightLeg.transform.SetParent(unitGO.transform, false);
        rightLeg.transform.localPosition = new Vector3(0.1f, 0.25f, 0f);
        rightLeg.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
        rightLeg.GetComponent<MeshRenderer>().material = bodyMat;

        var swordMat = new Material(shader);
        swordMat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.65f));
        swordMat.SetFloat("_Metallic", 0.7f);

        var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sword.name = "Sword";
        Object.Destroy(sword.GetComponent<Collider>());
        sword.transform.SetParent(unitGO.transform, false);
        sword.transform.localPosition = new Vector3(0.3f, 0.9f, 0f);
        sword.transform.localScale = new Vector3(0.04f, 0.6f, 0.03f);
        sword.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        sword.GetComponent<MeshRenderer>().material = swordMat;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(unitGO.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        var tm = labelGO.AddComponent<TextMesh>();
        tm.text = label;
        tm.characterSize = 0.08f;
        tm.fontSize = 48;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = bodyColor;
        labelGO.AddComponent<Billboard>();

        return unitGO;
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
    }
}
