using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class BattleManager : MonoBehaviour {
    public static BattleManager Instance;
    
    [Range(0, 5)] public float simSpeed = 1f;
    public float escapeRadius = 120f; // Scaled up for Unity World Space

    [Header("Unit Prefabs")]
    public GameObject knightPrefab;
    public GameObject archerPrefab;
    public GameObject magePrefab;

    public List<FactionManager> factions = new List<FactionManager>();
    public List<Unit> allUnits = new List<Unit>();

    void Awake() { Instance = this; }

    public void StartBattle(List<BookData> competingBooks) {
        factions.Clear();
        allUnits.Clear();

        int totalFactions = competingBooks.Count;
        float radius = 30f; // Spawn circle radius

        for (int i = 0; i < totalFactions; i++) {
            BookData book = competingBooks[i];
            FactionManager fm = new FactionManager {
                book = book,
                factionColor = Color.HSVToRGB((float)i / totalFactions, 0.8f, 0.9f),
                aliveCount = book.armySize,
                escapedCount = 0
            };
            factions.Add(fm);

            float angle = i * Mathf.PI * 2f / totalFactions;
            Vector2 spawnCenter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            SpawnUnits(fm, spawnCenter);
        }
    }

    void SpawnUnits(FactionManager fm, Vector2 center) {
        float tw = fm.book.comp.k + fm.book.comp.a + fm.book.comp.m;
        if (tw <= 0) { fm.book.comp.k = 1; tw = 1; }

        int kCount = Mathf.RoundToInt(fm.book.armySize * (fm.book.comp.k / tw));
        int aCount = Mathf.RoundToInt(fm.book.armySize * (fm.book.comp.a / tw));
        int mCount = fm.book.armySize - kCount - aCount;

        SpawnSpecificType(knightPrefab, kCount, fm, center);
        SpawnSpecificType(archerPrefab, aCount, fm, center);
        SpawnSpecificType(magePrefab, mCount, fm, center);
    }

    void SpawnSpecificType(GameObject prefab, int count, FactionManager fm, Vector2 center) {
        for (int i = 0; i < count; i++) {
            Vector2 pos = center + Random.insideUnitCircle * 5f;
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            Unit unitScript = go.GetComponent<Unit>();
            unitScript.Setup(fm.book, fm);
            allUnits.Add(unitScript);
        }
    }

    public Unit GetClosestEnemy(Unit searcher) {
        Unit closest = null;
        float minDist = float.MaxValue;
        foreach (Unit u in allUnits) {
            if (u == searcher || u.hp <= 0 || u.isEscaped || u.faction == searcher.faction) continue;
            float dist = Vector2.Distance(searcher.transform.position, u.transform.position);
            if (dist < minDist) { minDist = dist; closest = u; }
        }
        return closest;
    }

    void Update() {
        // Clean up dead units from the list
        allUnits.RemoveAll(u => u == null || u.hp <= 0 || u.isEscaped);

        // Check Win Condition
        int aliveFactions = factions.Count(f => f.aliveCount > 0);
        if (aliveFactions == 1) {
            FactionManager winner = factions.First(f => f.aliveCount > 0);
            simSpeed = 0;
            ProcessVictory(winner);
        }
    }

    void ProcessVictory(FactionManager winner) {
        Debug.Log(winner.book.title + " WINS!");
        // Update data exactly like your HTML logic
        foreach (var f in factions) {
            if (f != winner) {
                int baseBonus = f.book.format == BookFormat.Digital ? 2 : 4;
                f.book.armySize += baseBonus + f.escapedCount;
            }
        }
        DataManager.Instance.SaveData();
        // Trigger UI Modal here...
    }
}