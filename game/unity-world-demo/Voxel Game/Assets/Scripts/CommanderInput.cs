using UnityEngine;

public class CommanderInput : MonoBehaviour
{
    [SerializeField] CameraStateMachine cameraStateMachine;
    [SerializeField] SquadManager squadManager;
    [SerializeField] Camera commanderCam;

    GameObject _activeRallyMarker;

    void Start()
    {
        if (cameraStateMachine == null)
            cameraStateMachine = GetComponent<CameraStateMachine>();
        if (squadManager == null)
            squadManager = GetComponent<SquadManager>();
    }

    void Update()
    {
        if (cameraStateMachine == null || !cameraStateMachine.IsCommanderMode) return;
        if (squadManager == null) return;

        if (Input.GetMouseButtonDown(1))
            HandleRightClick();

        if (Input.GetKeyDown(KeyCode.H))
        {
            squadManager.SetAllState(UnitAI.UnitState.HoldPosition);
            Debug.Log($"[Commander] HOLD — squad size: {squadManager.AliveCount}");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            squadManager.SetAllState(UnitAI.UnitState.Following);
            Debug.Log($"[Commander] FOLLOW — squad size: {squadManager.AliveCount}");
        }
    }

    void HandleRightClick()
    {
        Camera cam = commanderCam != null ? commanderCam : Camera.main;
        if (cam == null)
        {
            // Fallback: find whatever camera is active
            cam = Object.FindObjectOfType<Camera>();
        }
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var enemy = hit.collider.GetComponentInParent<EnemyTag>();
            if (enemy != null)
            {
                squadManager.SetAttackTarget(enemy.transform);
                Debug.Log($"[Commander] ATTACK TARGET: {enemy.name}");
                return;
            }

            squadManager.SetRallyPoint(hit.point);
            ShowRallyMarker(hit.point);
            Debug.Log($"[Commander] RALLY to {hit.point}");
        }
    }

    void ShowRallyMarker(Vector3 position)
    {
        if (_activeRallyMarker != null)
            Destroy(_activeRallyMarker);

        _activeRallyMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _activeRallyMarker.name = "RallyMarker";
        Destroy(_activeRallyMarker.GetComponent<Collider>());
        _activeRallyMarker.transform.position = position + Vector3.up * 0.05f;
        _activeRallyMarker.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);

        var mr = _activeRallyMarker.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.2f, 0.6f, 1f, 0.7f));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mr.sharedMaterial = mat;

        Destroy(_activeRallyMarker, 8f);
    }
}
