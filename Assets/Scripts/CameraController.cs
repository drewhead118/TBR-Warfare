using UnityEngine;

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
        HandleInput();

        if (!isAuto && Time.time - lastActiveTime > 3f && !Input.GetMouseButton(0)) {
            isAuto = true;
        }

        if (isAuto && BattleManager.Instance != null && BattleManager.Instance.allUnits.Count > 0) {
            AutoFollow();
        }
    }

    void HandleInput() {
        // Drag to Pan
        if (Input.GetMouseButtonDown(0)) {
            dragStartPos = cam.ScreenToWorldPoint(Input.mousePosition);
            camStartPos = transform.position;
            isAuto = false;
            lastActiveTime = Time.time;
        }
        if (Input.GetMouseButton(0)) {
            Vector3 diff = dragStartPos - cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = camStartPos + diff;
            lastActiveTime = Time.time;
        }

        // Scroll to Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f) {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * 5f, 5f, 150f);
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