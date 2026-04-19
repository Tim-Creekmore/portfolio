using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerStamina playerStamina;
    [SerializeField] SquadManager squadManager;

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

        data.units = new List<UnitSaveData>();
        if (squadManager != null)
        {
            foreach (var unit in squadManager.Units)
            {
                if (unit == null || !unit.IsAlive) continue;
                var ud = new UnitSaveData();
                ud.x = unit.transform.position.x;
                ud.y = unit.transform.position.y;
                ud.z = unit.transform.position.z;
                ud.hp = unit.Health.CurrentHP;
                ud.maxHP = unit.Health.MaxHP;
                ud.formationIndex = unit.FormationIndex;
                data.units.Add(ud);
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Game saved to {SavePath}");
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

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
        if (cc != null) cc.enabled = true;

        if (playerHealth != null)
            playerHealth.SetHP(data.playerHP);
        if (playerStamina != null)
            playerStamina.SetStamina(data.playerStamina);

        if (squadManager != null && data.units != null)
        {
            var units = squadManager.Units;
            for (int i = 0; i < data.units.Count && i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.IsAlive) continue;

                var ucc = unit.GetComponent<CharacterController>();
                if (ucc != null) ucc.enabled = false;
                unit.transform.position = new Vector3(
                    data.units[i].x, data.units[i].y, data.units[i].z);
                if (ucc != null) ucc.enabled = true;

                unit.Health.SetHP(data.units[i].hp);
            }
        }

        Debug.Log("Game loaded.");
    }
}

[System.Serializable]
public class SaveData
{
    public float playerX, playerY, playerZ;
    public float playerHP, playerMaxHP;
    public float playerStamina, playerMaxStamina;
    public List<UnitSaveData> units;
}

[System.Serializable]
public class UnitSaveData
{
    public float x, y, z;
    public float hp, maxHP;
    public int formationIndex;
}
