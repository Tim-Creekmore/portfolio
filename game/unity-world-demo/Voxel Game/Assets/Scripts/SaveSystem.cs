using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerStamina playerStamina;
    [SerializeField] SquadManager squadManager;
    [SerializeField] UnitSpawner unitSpawner;

    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            Save();
        else if (Input.GetKeyDown(KeyCode.F9))
            Load();
    }

    public void Save()
    {
        var data = new SaveData();

        var pos = transform.position;
        data.playerX = pos.x;
        data.playerY = pos.y;
        data.playerZ = pos.z;
        data.playerHP = playerHealth != null ? playerHealth.CurrentHP : 100f;
        data.playerMaxHP = playerHealth != null ? playerHealth.MaxHP : 100f;
        data.playerStamina = playerStamina != null ? playerStamina.Current : 100f;
        data.playerMaxStamina = playerStamina != null ? playerStamina.Max : 100f;

        // Friendly squad units
        data.units = new List<UnitSaveData>();
        if (squadManager != null)
        {
            foreach (var unit in squadManager.Units)
            {
                if (unit == null || !unit.IsAlive) continue;
                data.units.Add(MakeSnapshot(unit));
            }
        }

        // All enemy units (have EnemyTag)
        data.enemies = new List<UnitSaveData>();
        var enemyTags = Object.FindObjectsOfType<EnemyTag>();
        foreach (var tag in enemyTags)
        {
            if (tag == null) continue;
            var ai = tag.GetComponent<UnitAI>();
            if (ai == null || !ai.IsAlive) continue;
            data.enemies.Add(MakeSnapshot(ai));
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Game saved: {data.units.Count} squad, {data.enemies.Count} enemies → {SavePath}");
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);

        // Teleport player first so freshly-spawned friendlies form around the saved location
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
        if (cc != null) cc.enabled = true;

        if (playerHealth != null)
            playerHealth.SetHP(data.playerHP);
        if (playerStamina != null)
            playerStamina.SetStamina(data.playerStamina);

        // Wipe all current units and respawn fresh — this resets aggro AND brings back
        // any units that died between save and load
        if (unitSpawner != null)
            unitSpawner.RespawnAll();

        // Now overlay the saved snapshots onto the freshly-spawned units
        if (squadManager != null && data.units != null)
        {
            foreach (var snap in data.units)
            {
                UnitAI match = FindFriendlyByFormation(snap.formationIndex);
                if (match != null) ApplySnapshot(match, snap);
            }
        }

        if (data.enemies != null)
        {
            foreach (var snap in data.enemies)
            {
                UnitAI match = FindEnemyByFormation(snap.formationIndex);
                if (match != null) ApplySnapshot(match, snap);
            }
        }

        Debug.Log($"Game loaded: {(data.units?.Count ?? 0)} squad, {(data.enemies?.Count ?? 0)} enemies");
    }

    UnitAI FindFriendlyByFormation(int formationIndex)
    {
        if (squadManager == null) return null;
        foreach (var unit in squadManager.Units)
        {
            if (unit != null && unit.IsAlive && unit.FormationIndex == formationIndex)
                return unit;
        }
        return null;
    }

    UnitAI FindEnemyByFormation(int formationIndex)
    {
        var tags = Object.FindObjectsOfType<EnemyTag>();
        foreach (var tag in tags)
        {
            if (tag == null) continue;
            var ai = tag.GetComponent<UnitAI>();
            if (ai != null && ai.IsAlive && ai.FormationIndex == formationIndex)
                return ai;
        }
        return null;
    }

    static UnitSaveData MakeSnapshot(UnitAI unit)
    {
        return new UnitSaveData
        {
            x = unit.transform.position.x,
            y = unit.transform.position.y,
            z = unit.transform.position.z,
            hp = unit.Health.CurrentHP,
            maxHP = unit.Health.MaxHP,
            formationIndex = unit.FormationIndex,
        };
    }

    static void ApplySnapshot(UnitAI unit, UnitSaveData snap)
    {
        var ucc = unit.GetComponent<CharacterController>();
        if (ucc != null) ucc.enabled = false;
        unit.transform.position = new Vector3(snap.x, snap.y, snap.z);
        if (ucc != null) ucc.enabled = true;
        unit.Health.SetHP(snap.hp);
    }
}

[System.Serializable]
public class SaveData
{
    public float playerX, playerY, playerZ;
    public float playerHP, playerMaxHP;
    public float playerStamina, playerMaxStamina;
    public List<UnitSaveData> units;
    public List<UnitSaveData> enemies;
}

[System.Serializable]
public class UnitSaveData
{
    public float x, y, z;
    public float hp, maxHP;
    public int formationIndex;
}
