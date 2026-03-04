using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class FactionBanner : MonoBehaviour
{
    private FactionManager faction;
    private Camera mainCam;
    private Vector3 initialScale;

    [Header("UI References")]
    public RawImage coverImage;
    public TextMeshProUGUI titleAndCountText;
    public Image backgroundImage; // Optional: To color-code the banner

    [Header("Settings")]
    public float smoothSpeed = 5f;
    public float yOffset = 5f; // How high above the centroid the banner hovers
    
    // The camera orthographic size at which the banner stops shrinking visually on screen.
    // Increase this number if you want it to stop shrinking sooner.
    public float zoomThreshold = 30f; 

    private Vector2 currentVelocity; // Used if we want to use SmoothDamp instead of Lerp for even smoother motion
    private bool isInitialized = false;

    public void Setup(FactionManager factionManager)
    {
        faction = factionManager;
        mainCam = Camera.main;
        initialScale = transform.localScale;

        if (backgroundImage != null) {
            backgroundImage.color = faction.factionColor;
        }

        StartCoroutine(LoadCoverImage(faction.book.coverUrl));
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || faction == null) return;

        UpdateBannerState();
    }

    private void UpdateBannerState()
    {
        // 1. Calculate Centroid
        Vector2 centroidSum = Vector2.zero;
        int aliveCount = 0;

        foreach (Unit u in BattleManager.Instance.allUnits)
        {
            if (u.faction == faction.book && u.hp > 0 && !u.isEscaped)
            {
                centroidSum += (Vector2)u.transform.position;
                aliveCount++;
            }
        }

        // Hide banner if faction is wiped out
        if (aliveCount == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        // Update Text
        titleAndCountText.text = $"{faction.book.title}\n({aliveCount})";

        // 2. Handle Scale Threshold (Prevent shrinking too much)
        float currentZoom = mainCam.orthographicSize;
        float scaleMultiplier = 1f;

        if (currentZoom > zoomThreshold)
        {
            // By multiplying the world scale by this ratio, the object grows in world space 
            // exactly proportional to how far the camera is zooming out, keeping screen-size constant.
            scaleMultiplier = currentZoom / zoomThreshold;
        }
        
        transform.localScale = initialScale * scaleMultiplier;

        // 3. Smooth Tweening to Centroid
        Vector2 targetPosition = (centroidSum / aliveCount);
        
        // Add vertical offset so it floats above the army (scaled by our multiplier so it stays proportionally above)
        targetPosition.y += (yOffset * scaleMultiplier);

        // Smoothly interpolate current position to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }

    // --- IMAGE LOADING LOGIC ---
    private IEnumerator LoadCoverImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) yield break;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                coverImage.texture = DownloadHandlerTexture.GetContent(request);
            }
        }
    }
}