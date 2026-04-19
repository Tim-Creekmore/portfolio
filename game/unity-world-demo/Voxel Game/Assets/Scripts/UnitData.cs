using UnityEngine;

[CreateAssetMenu(fileName = "NewUnit", menuName = "Voxel Kingdom/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string unitName = "Unnamed Unit";

    [Header("Stats")]
    public float maxHP = 60f;
    public float damage = 10f;
    public float attackInterval = 1.5f;
    public float attackRange = 1.8f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float followDistance = 2.5f;
    public float threatDetectionRange = 12f;

    [Header("Morale")]
    public float moraleThreshold = 30f;
}
