using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class UIManager : MonoBehaviour {
    [Header("Panels")]
    public GameObject adminPanel;
    public GameObject battlePanel;
    public GameObject winnerModal;

    [Header("Admin Form")]
    public TMP_InputField titleInput;
    public TMP_InputField coverInput;
    public TMP_InputField armyInput;
    public TMP_Dropdown formatDropdown;
    public TMP_InputField kInput, aInput, mInput;

    [Header("CSV Data")]
    public TMP_InputField csvInput;

    [Header("Book List Queue")]
    public GameObject bookRowPrefab;     // Assign your BookRowUI prefab here
    public Transform bookListContent;    // Assign the Content object of your Scroll View

    [Header("Winner Modal")]
    public TextMeshProUGUI winnerTitleText;
    public RawImage winnerCoverImage;
    public TextMeshProUGUI winnerStatsText;
    public TextMeshProUGUI loserStatsText;

    void Start() {
        // Initialize the table when the game starts
        SwitchToAdmin();
    }

    public void SwitchToBattle() {
        if (DataManager.Instance.db.books.Count < 2) {
            Debug.LogWarning("Need at least 2 books to battle!");
            return;
        }
        adminPanel.SetActive(false);
        battlePanel.SetActive(true);
        winnerModal.SetActive(false);
        BattleManager.Instance.StartBattle(DataManager.Instance.db.books);
    }

    public void SwitchToAdmin() {
        adminPanel.SetActive(true);
        battlePanel.SetActive(false);
        winnerModal.SetActive(false);
        RefreshTableUI();
    }

    // --- TABLE GENERATION ---
    public void RefreshTableUI() {
        // Clear existing rows
        foreach (Transform child in bookListContent) {
            Destroy(child.gameObject);
        }

        // Spawn a row for every book in the database
        foreach (BookData book in DataManager.Instance.db.books) {
            GameObject rowObj = Instantiate(bookRowPrefab, bookListContent);
            BookRowUI rowUI = rowObj.GetComponent<BookRowUI>();
            rowUI.Initialize(book, this);
        }
    }

    public void DeleteBook(BookData book) {
        DataManager.Instance.db.books.Remove(book);
        DataManager.Instance.SaveData();
        RefreshTableUI();
    }

    public void ClearAllBooks() {
        DataManager.Instance.db.books.Clear();
        DataManager.Instance.SaveData();
        RefreshTableUI();
    }

    // --- FORM & DATA IMPORTING ---
    public void AddBookFromForm() {
        BookData newBook = new BookData {
            id = System.Guid.NewGuid().ToString(),
            title = string.IsNullOrEmpty(titleInput.text) ? "Unknown Title" : titleInput.text,
            coverUrl = string.IsNullOrEmpty(coverInput.text) ? "https://via.placeholder.com/150" : coverInput.text,
            armySize = int.TryParse(armyInput.text, out int size) ? size : 15,
            format = formatDropdown.value == 0 ? BookFormat.Paperback : BookFormat.Digital,
            comp = new ArmyComp {
                k = float.TryParse(kInput.text, out float k) ? k : 1,
                a = float.TryParse(aInput.text, out float a) ? a : 1,
                m = float.TryParse(mInput.text, out float m) ? m : 1
            }
        };
        DataManager.Instance.db.books.Add(newBook);
        DataManager.Instance.SaveData();
        
        // Reset inputs to default
        titleInput.text = ""; coverInput.text = ""; armyInput.text = "15";
        kInput.text = "1"; aInput.text = "1"; mInput.text = "1";
        
        RefreshTableUI(); // Instantly update the list
    }

    public void ParseCSV() {
        string[] lines = csvInput.text.Split('\n');
        int added = 0;
        foreach(string line in lines) {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(',');
            if (parts.Length >= 3) {
                BookData b = new BookData {
                    id = System.Guid.NewGuid().ToString(),
                    title = parts[0].Trim(),
                    coverUrl = parts[1].Trim(),
                    armySize = int.Parse(parts[2].Trim()),
                    format = (parts.Length > 3 && parts[3].Trim().ToLower() == "digital") ? BookFormat.Digital : BookFormat.Paperback,
                    comp = new ArmyComp {
                        k = parts.Length > 4 ? float.Parse(parts[4].Trim()) : 1,
                        a = parts.Length > 5 ? float.Parse(parts[5].Trim()) : 1,
                        m = parts.Length > 6 ? float.Parse(parts[6].Trim()) : 1
                    }
                };
                DataManager.Instance.db.books.Add(b);
                added++;
            }
        }
        DataManager.Instance.SaveData();
        csvInput.text = "";
        RefreshTableUI(); // Instantly update the list
    }

    public void ExportCSV() {
        string csv = "Title,CoverURL,ArmySize,Format,Knights_Wt,Archers_Wt,Mages_Wt\n";
        foreach (var b in DataManager.Instance.db.books) {
            csv += $"{b.title},{b.coverUrl},{b.armySize},{b.format},{b.comp.k},{b.comp.a},{b.comp.m}\n";
        }
        string path = Application.persistentDataPath + "/tbr_export.csv";
        File.WriteAllText(path, csv);
        Application.OpenURL("file://" + Application.persistentDataPath); 
    }

    // --- BATTLE MODAL ---
    public void ShowWinnerModal(FactionManager winner) {
        winnerModal.SetActive(true);
        winnerTitleText.text = winner.book.title;
        StartCoroutine(LoadImage(winner.book.coverUrl, winnerCoverImage));
        
        winnerStatsText.text = $"Base Size: {winner.book.armySize} | Escaped Cowards: {winner.escapedCount}";

        string loserBreakdown = "Remaining Armies Update:\n";
        foreach(var f in BattleManager.Instance.factions) {
            if (f != winner) {
                int baseBonus = f.book.format == BookFormat.Digital ? 2 : 4;
                int totalChange = baseBonus + f.escapedCount;
                loserBreakdown += $"{f.book.title} - Pop: {f.book.armySize + totalChange} (+{totalChange}) [+{baseBonus} submission {f.book.format}, +{f.escapedCount} escaped]\n";
            }
        }
        loserStatsText.text = loserBreakdown;
    }

    public void AcceptVictory() {
        SwitchToAdmin();
    }

    private IEnumerator LoadImage(string url, RawImage targetImage) {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success) {
            targetImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        }
    }
}