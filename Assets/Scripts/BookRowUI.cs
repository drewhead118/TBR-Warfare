using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class BookRowUI : MonoBehaviour
{
    private BookData boundBook;
    private UIManager uiManager;
    private Coroutine imageLoadCoroutine;

    [Header("UI References")]
    public RawImage coverImage;              // The visual image component
    public TMP_InputField coverUrlInput;     // The input field for the URL
    public TMP_InputField titleInput;
    public TMP_Dropdown formatDropdown;
    public TMP_InputField armySizeInput;
    public TMP_InputField kInput;
    public TMP_InputField aInput;
    public TMP_InputField mInput;
    public Button removeButton;

    public void Initialize(BookData book, UIManager manager)
    {
        boundBook = book;
        uiManager = manager;

        // Populate UI without triggering the onValueChanged events
        titleInput.SetTextWithoutNotify(book.title);
        coverUrlInput.SetTextWithoutNotify(book.coverUrl);
        formatDropdown.SetValueWithoutNotify(book.format == BookFormat.Paperback ? 0 : 1);
        armySizeInput.SetTextWithoutNotify(book.armySize.ToString());
        kInput.SetTextWithoutNotify(book.comp.k.ToString());
        aInput.SetTextWithoutNotify(book.comp.a.ToString());
        mInput.SetTextWithoutNotify(book.comp.m.ToString());

        // Load the image initially
        LoadCoverImage(book.coverUrl);

        // Hook up listeners to auto-save edits
        titleInput.onValueChanged.AddListener(OnTitleChanged);
        coverUrlInput.onValueChanged.AddListener(OnCoverUrlChanged);
        formatDropdown.onValueChanged.AddListener(OnFormatChanged);
        armySizeInput.onValueChanged.AddListener(OnArmySizeChanged);
        kInput.onValueChanged.AddListener(OnCompChanged);
        aInput.onValueChanged.AddListener(OnCompChanged);
        mInput.onValueChanged.AddListener(OnCompChanged);

        removeButton.onClick.AddListener(OnRemoveClicked);
    }

    private void OnTitleChanged(string newTitle)
    {
        boundBook.title = newTitle;
        DataManager.Instance.SaveData();
    }

    private void OnCoverUrlChanged(string newUrl)
    {
        boundBook.coverUrl = newUrl;
        DataManager.Instance.SaveData();
        
        // Refresh the image display whenever the URL is changed
        LoadCoverImage(newUrl);
    }

    private void OnFormatChanged(int index)
    {
        boundBook.format = index == 0 ? BookFormat.Paperback : BookFormat.Digital;
        DataManager.Instance.SaveData();
    }

    private void OnArmySizeChanged(string newSize)
    {
        if (int.TryParse(newSize, out int result)) {
            boundBook.armySize = result;
            DataManager.Instance.SaveData();
        }
    }

    private void OnCompChanged(string _)
    {
        float.TryParse(kInput.text, out boundBook.comp.k);
        float.TryParse(aInput.text, out boundBook.comp.a);
        float.TryParse(mInput.text, out boundBook.comp.m);
        
        DataManager.Instance.SaveData();
    }

    private void OnRemoveClicked()
    {
        uiManager.DeleteBook(boundBook);
    }

    // --- IMAGE LOADING LOGIC ---
    private void LoadCoverImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        
        // Stop any current loading process if the user types fast
        if (imageLoadCoroutine != null) StopCoroutine(imageLoadCoroutine);
        
        imageLoadCoroutine = StartCoroutine(DownloadImage(url));
    }

    private IEnumerator DownloadImage(string url)
    {
        // Using statement ensures the web request is properly disposed to prevent memory leaks
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Apply the downloaded texture to the RawImage
                coverImage.texture = DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                Debug.LogWarning($"Failed to load image from {url}: {request.error}");
                // Optional: You could assign a fallback/error texture here 
                // coverImage.texture = myErrorTexture;
            }
        }
    }
}