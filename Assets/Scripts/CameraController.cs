using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class CameraController : MonoBehaviour {
    private Camera cam;
    private bool isAuto = true;
    private float lastActiveTime;
    
    private Vector3 dragStartPos;
    private Vector3 camStartPos;

    void Start() {
        cam = GetComponent<Camera>();
        lastActiveTime = Time.time;
    }

    void Update() {
        // Guard check to ensure a mouse is connected
        if (Mouse.current != null) {
            HandleInput();
        }

        // Resume auto-cam after 3 seconds of inactivity
        if (!isAuto && Time.time - lastActiveTime > 3f && (Mouse.current == null || !Mouse.current.leftButton.isPressed)) {
            isAuto = true;
        }

        if (isAuto && BattleManager.Instance != null && BattleManager.Instance.allUnits.Count > 0) {
            AutoFollow();
        }
    }

    void HandleInput() {
        Mouse mouse = Mouse.current;

        // Drag to Pan
        if (mouse.leftButton.wasPressedThisFrame) {
            dragStartPos = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            camStartPos = transform.position;
            isAuto = false;
            lastActiveTime = Time.time;
        }
        
        if (mouse.leftButton.isPressed) {
            Vector3 diff = dragStartPos - cam.ScreenToWorldPoint(mouse.position.ReadValue());
            transform.position = camStartPos + diff;
            lastActiveTime = Time.time;
        }

        // Scroll to Zoom
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f) {
            // The new input system often returns large scroll values (like 120 or -120), so we normalize it to 1 or -1
            float scrollDir = Mathf.Sign(scroll); 
            
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scrollDir * 5f, 5f, 150f);
            isAuto = false;
            lastActiveTime = Time.time;
        }
    }

    void AutoFollow() {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var u in BattleManager.Instance.allUnits) {
            if (u.transform.position.x < minX) minX = u.transform.position.x;
            if (u.transform.position.x > maxX) maxX = u.transform.position.x;
            if (u.transform.position.y < minY) minY = u.transform.position.y;
            if (u.transform.position.y > maxY) maxY = u.transform.position.y;
        }

        // Failsafe in case units haven't spawned yet
        if (minX == float.MaxValue) return;

        Vector3 targetPos = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, -10f);
        float targetW = Mathf.Max(maxX - minX + 20f, 40f);
        float targetH = Mathf.Max(maxY - minY + 20f, 30f);
        
        float screenRatio = (float)Screen.width / Screen.height;
        float targetSize = Mathf.Max(targetH / 2f, (targetW / screenRatio) / 2f);
        targetSize = Mathf.Clamp(targetSize, 10f, 150f);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * 2f);
    }
}