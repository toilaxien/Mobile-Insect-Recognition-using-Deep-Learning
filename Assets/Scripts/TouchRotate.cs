// using UnityEngine;

// public class TouchRotate : MonoBehaviour
// {
//     public float rotateSpeed = 0.2f;
//     public bool onlyRotateWhenTouchingModel = true;

//     Camera cam;
//     bool dragging;
//     Vector2 lastPos;

//     void Awake()
//     {
//         cam = Camera.main;
//     }

//     void Update()
//     {
//         // Touch (Android)
//         if (Input.touchCount == 1)
//         {
//             var t = Input.GetTouch(0);

//             if (t.phase == TouchPhase.Began)
//             {
//                 dragging = CanDrag(t.position);
//                 lastPos = t.position;
//             }
//             else if (t.phase == TouchPhase.Moved && dragging)
//             {
//                 Vector2 delta = t.position - lastPos;
//                 Rotate(delta);
//                 lastPos = t.position;
//             }
//             else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
//             {
//                 dragging = false;
//             }
//             return;
//         }

//         // Chuột (test trong Editor)
//         if (Input.GetMouseButtonDown(0))
//         {
//             dragging = CanDrag(Input.mousePosition);
//             lastPos = Input.mousePosition;
//         }
//         else if (Input.GetMouseButton(0) && dragging)
//         {
//             Vector2 delta = (Vector2)Input.mousePosition - lastPos;
//             Rotate(delta);
//             lastPos = Input.mousePosition;
//         }
//         else if (Input.GetMouseButtonUp(0))
//         {
//             dragging = false;
//         }
//     }

//     bool CanDrag(Vector2 screenPos)
//     {
//         if (!onlyRotateWhenTouchingModel) return true;
//         if (!cam) return true;

//         Ray ray = cam.ScreenPointToRay(screenPos);
//         if (Physics.Raycast(ray, out RaycastHit hit, 500f))
//         {
//             return hit.transform == transform || hit.transform.IsChildOf(transform);
//         }
//         return false;
//     }

//     void Rotate(Vector2 delta)
//     {
//         float yaw = -delta.x * rotateSpeed;
//         transform.Rotate(0f, yaw, 0f, Space.World);
//     }
// }




// using UnityEngine;

// public class TouchRotate : MonoBehaviour
// {
//     [Header("Rotate")]
//     public float rotateSpeed = 0.2f;
//     public bool onlyRotateWhenTouchingModel = true;

//     [Header("Pinch Zoom (scale model)")]
//     public float zoomSpeed = 0.005f;   // độ nhạy pinch
//     public float minScale = 0.05f;     // scale nhỏ nhất
//     public float maxScale = 3.0f;      // scale lớn nhất

//     Camera cam;
//     bool dragging;
//     Vector2 lastPos;

//     float initialDistance;
//     Vector3 initialScale;

//     void Awake()
//     {
//         cam = Camera.main;
//     }

//     void Update()
//     {
//         // ===== 2 ngón: pinch để zoom (scale) =====
//         if (Input.touchCount == 2)
//         {
//             Touch t0 = Input.GetTouch(0);
//             Touch t1 = Input.GetTouch(1);

//             // khi bắt đầu pinch
//             if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
//             {
//                 initialDistance = Vector2.Distance(t0.position, t1.position);
//                 initialScale = transform.localScale;
//                 dragging = false; // đang pinch thì không xoay
//             }
//             else
//             {
//                 float currentDistance = Vector2.Distance(t0.position, t1.position);
//                 float delta = currentDistance - initialDistance;

//                 // scale theo delta
//                 float scaleFactor = 1f + delta * zoomSpeed;

//                 Vector3 targetScale = initialScale * scaleFactor;
//                 float clamped = Mathf.Clamp(targetScale.x, minScale, maxScale);

//                 // giữ uniform scale
//                 transform.localScale = new Vector3(clamped, clamped, clamped);
//             }
//             return;
//         }

//         // ===== 1 ngón: xoay =====
//         if (Input.touchCount == 1)
//         {
//             Touch t = Input.GetTouch(0);

//             if (t.phase == TouchPhase.Began)
//             {
//                 dragging = CanDrag(t.position);
//                 lastPos = t.position;
//             }
//             else if (t.phase == TouchPhase.Moved && dragging)
//             {
//                 Vector2 delta = t.position - lastPos;
//                 Rotate(delta);
//                 lastPos = t.position;
//             }
//             else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
//             {
//                 dragging = false;
//             }
//             return;
//         }

//         // ===== Editor / PC: chuột xoay + wheel zoom =====
//         if (Input.GetMouseButtonDown(0))
//         {
//             dragging = CanDrag(Input.mousePosition);
//             lastPos = Input.mousePosition;
//         }
//         else if (Input.GetMouseButton(0) && dragging)
//         {
//             Vector2 delta = (Vector2)Input.mousePosition - lastPos;
//             Rotate(delta);
//             lastPos = Input.mousePosition;
//         }
//         else if (Input.GetMouseButtonUp(0))
//         {
//             dragging = false;
//         }

//         float wheel = Input.mouseScrollDelta.y;
//         if (Mathf.Abs(wheel) > 0.0001f)
//         {
//             float factor = 1f + wheel * 0.1f;
//             Vector3 targetScale = transform.localScale * factor;
//             float clamped = Mathf.Clamp(targetScale.x, minScale, maxScale);
//             transform.localScale = new Vector3(clamped, clamped, clamped);
//         }
//     }

//     bool CanDrag(Vector2 screenPos)
//     {
//         if (!onlyRotateWhenTouchingModel) return true;
//         if (!cam) return true;

//         Ray ray = cam.ScreenPointToRay(screenPos);
//         if (Physics.Raycast(ray, out RaycastHit hit, 500f))
//         {
//             return hit.transform == transform || hit.transform.IsChildOf(transform);
//         }
//         return false;
//     }

//     void Rotate(Vector2 delta)
//     {
//         float yaw = -delta.x * rotateSpeed;
//         transform.Rotate(0f, yaw, 0f, Space.World);
//     }
// }




using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    [Header("Rotate")]
    public float rotateSpeed = 0.25f;
    public float rotationSmooth = 12f;

    [Header("Zoom (scale)")]
    public float pinchSpeed = 0.004f;
    public float zoomSmooth = 12f;
    public float minScale = 0.05f;
    public float maxScale = 3.0f;

    [Header("Interaction")]
    public bool onlyRotateWhenTouchingModel = true;

    [Header("Raycast Camera (QUAN TRỌNG)")]
    public Camera inputCamera; // Kéo ModelCamera (hoặc camera đang nhìn model) vào đây

    float targetYawDelta, currentYawDelta;
    float targetScale, currentScale;

    Vector2 lastPos;
    bool dragging;

    float startPinchDist;
    float startPinchScale;

    void Awake()
    {
        if (inputCamera == null) inputCamera = Camera.main;
        currentScale = targetScale = transform.localScale.x;
    }

    void Update()
    {
        // Pinch zoom
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                startPinchDist = Vector2.Distance(t0.position, t1.position);
                startPinchScale = targetScale;
                dragging = false;
            }
            else
            {
                float dist = Vector2.Distance(t0.position, t1.position);

                // ✅ dùng ratio để không bị đảo (phóng to thành nhỏ)
                if (startPinchDist > 0.0001f)
                {
                    float ratio = dist / startPinchDist;
                    float scale = startPinchScale * ratio;
                    targetScale = Mathf.Clamp(scale, minScale, maxScale);
                }
            }
        }
        // Rotate
        else if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                dragging = CanDrag(t.position);
                lastPos = t.position;
                targetYawDelta = 0f;
            }
            else if (t.phase == TouchPhase.Moved && dragging)
            {
                Vector2 delta = t.position - lastPos;
                targetYawDelta = -delta.x * rotateSpeed;
                lastPos = t.position;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                dragging = false;
                targetYawDelta = 0f;
            }
        }
        else
        {
            targetYawDelta = 0f;
        }

        currentYawDelta = Mathf.Lerp(currentYawDelta, targetYawDelta, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
        if (Mathf.Abs(currentYawDelta) > 0.00001f)
            transform.Rotate(0f, currentYawDelta, 0f, Space.World);

        currentScale = Mathf.Lerp(currentScale, targetScale, 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime));
        transform.localScale = new Vector3(currentScale, currentScale, currentScale);
    }

    bool CanDrag(Vector2 screenPos)
    {
        if (!onlyRotateWhenTouchingModel) return true;
        if (inputCamera == null) return true;

        Ray ray = inputCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return hit.transform == transform || hit.transform.IsChildOf(transform);

        return false;
    }
}


// using UnityEngine;

// public class TouchRotate : MonoBehaviour
// {
//     [Header("Rotate")]
//     public float rotateSpeed = 0.25f;            // độ nhạy kéo
//     public float rotationSmooth = 12f;           // càng cao càng mượt

//     [Header("Zoom (scale)")]
//     public float pinchSpeed = 0.004f;            // độ nhạy pinch
//     public float zoomSmooth = 12f;               // mượt zoom
//     public float minScale = 0.05f;
//     public float maxScale = 3.0f;

//     [Header("Interaction")]
//     public bool onlyRotateWhenTouchingModel = true;

//     Camera cam;

//     // mục tiêu (target) và giá trị hiện tại (current) để smooth
//     float targetYawDelta;
//     float currentYawDelta;

//     float targetScale;
//     float currentScale;

//     Vector2 lastPos;
//     bool dragging;

//     float startPinchDist;
//     float startPinchScale;

//     void Awake()
//     {
//         cam = Camera.main;
//         currentScale = targetScale = transform.localScale.x;
//     }

//     void Update()
//     {
//         // ===== pinch zoom 2 ngón =====
//         if (Input.touchCount == 2)
//         {
//             Touch t0 = Input.GetTouch(0);
//             Touch t1 = Input.GetTouch(1);

//             if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
//             {
//                 startPinchDist = Vector2.Distance(t0.position, t1.position);
//                 startPinchScale = targetScale;
//                 dragging = false;
//             }
//             else
//             {
//                 float dist = Vector2.Distance(t0.position, t1.position);
//                 float delta = dist - startPinchDist;

//                 float scale = startPinchScale * (1f + delta * pinchSpeed);
//                 targetScale = Mathf.Clamp(scale, minScale, maxScale);
//             }
//         }
//         // ===== xoay 1 ngón =====
//         else if (Input.touchCount == 1)
//         {
//             Touch t = Input.GetTouch(0);

//             if (t.phase == TouchPhase.Began)
//             {
//                 dragging = CanDrag(t.position);
//                 lastPos = t.position;
//                 targetYawDelta = 0f;
//             }
//             else if (t.phase == TouchPhase.Moved && dragging)
//             {
//                 Vector2 delta = t.position - lastPos;
//                 targetYawDelta = -delta.x * rotateSpeed;   // cập nhật mục tiêu
//                 lastPos = t.position;
//             }
//             else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
//             {
//                 dragging = false;
//                 targetYawDelta = 0f;
//             }
//         }
//         else
//         {
//             targetYawDelta = 0f;
//         }

//         // ===== smooth xoay =====
//         currentYawDelta = Mathf.Lerp(currentYawDelta, targetYawDelta, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
//         if (Mathf.Abs(currentYawDelta) > 0.00001f)
//             transform.Rotate(0f, currentYawDelta, 0f, Space.World);

//         // ===== smooth zoom =====
//         currentScale = Mathf.Lerp(currentScale, targetScale, 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime));
//         transform.localScale = new Vector3(currentScale, currentScale, currentScale);
//     }

//     bool CanDrag(Vector2 screenPos)
//     {
//         if (!onlyRotateWhenTouchingModel) return true;
//         if (!cam) return true;

//         Ray ray = cam.ScreenPointToRay(screenPos);
//         if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
//             return hit.transform == transform || hit.transform.IsChildOf(transform);

//         return false;
//     }
// }
