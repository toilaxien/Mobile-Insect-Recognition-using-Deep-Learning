

// using UnityEngine;
// using UnityEngine.EventSystems;

// [RequireComponent(typeof(RectTransform))]
// public class PuzzlePieceDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
// {
//     public bool IsPlaced { get; private set; }

//     RectTransform rt;
//     RectTransform piecesArea;
//     RectTransform correctSlot;

//     Vector2 startAnchoredPos;
//     Vector2 dragOffset;
//     bool dragEnabled = true;

//     Transform startParent;

//     public void Init(RectTransform piecesArea, RectTransform correctSlot)
//     {
//         this.piecesArea = piecesArea;
//         this.correctSlot = correctSlot;

//         rt = GetComponent<RectTransform>();
//         IsPlaced = false;
//         dragEnabled = true;

//         startParent = transform.parent;

//         var cg = GetComponent<CanvasGroup>();
//         if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
//         cg.blocksRaycasts = true;
//     }

//     public void DisableDrag()
//     {
//         dragEnabled = false;
//         var cg = GetComponent<CanvasGroup>();
//         if (cg != null) cg.blocksRaycasts = false;
//     }

//     public void OnBeginDrag(PointerEventData eventData)
//     {
//         if (!dragEnabled || IsPlaced) return;

//         // lưu lại vị trí cũ để thả sai quay về
//         startAnchoredPos = rt.anchoredPosition;

//         // đưa về piecesArea khi kéo (để kéo đúng hệ)
//         if (transform.parent != piecesArea)
//             transform.SetParent(piecesArea, true);

//         transform.SetAsLastSibling();

//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             piecesArea, eventData.position, eventData.pressEventCamera, out var localPoint);

//         dragOffset = rt.anchoredPosition - localPoint;
//     }

//     public void OnDrag(PointerEventData eventData)
//     {
//         if (!dragEnabled || IsPlaced) return;

//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             piecesArea, eventData.position, eventData.pressEventCamera, out var localPoint);

//         rt.anchoredPosition = localPoint + dragOffset;
//     }

//     public void OnEndDrag(PointerEventData eventData)
//     {
//         if (!dragEnabled || IsPlaced) return;

//         // kiểm tra thả có nằm trong vùng ô đúng không (theo SCREEN point)
//         bool inside = RectTransformUtility.RectangleContainsScreenPoint(
//             correctSlot, eventData.position, eventData.pressEventCamera
//         );

//         if (inside)
//         {
//             // ✅ ĐÚNG: gắn mảnh vào slot -> (0,0) luôn đúng tuyệt đối
//             transform.SetParent(correctSlot, false);
//             rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
//             rt.pivot = new Vector2(0.5f, 0.5f);
//             rt.anchoredPosition = Vector2.zero;

//             IsPlaced = true;
//             DisableDrag();
//         }
//         else
//         {
//             // ❌ SAI: quay về vị trí cũ và parent cũ
//             transform.SetParent(piecesArea, true);
//             rt.anchoredPosition = startAnchoredPos;
//         }
//     }
// }


using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PuzzlePieceDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool IsPlaced { get; private set; }

    RectTransform self;
    RectTransform dragRoot;      // thường là piecesArea
    RectTransform correctSlot;   // slot đúng của mảnh

    float snapDistance = 80f;

    Canvas rootCanvas;
    CanvasGroup canvasGroup;

    Vector2 startAnchoredPos;    // vị trí ban đầu (xấp bài)
    Transform startParent;       // parent ban đầu (piecesArea)
    int startSiblingIndex;

    public void Init(RectTransform dragRoot, RectTransform correctSlot, float snapDistance)
    {
        self = GetComponent<RectTransform>();
        this.dragRoot = dragRoot;
        this.correctSlot = correctSlot;
        this.snapDistance = Mathf.Max(5f, snapDistance);

        // tìm canvas cha để tính đúng scale drag
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            Debug.LogWarning("PuzzlePieceDrag: Không tìm thấy Canvas cha.");

        // để kéo mượt + không chặn raycast khi đang drag
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // lưu vị trí ban đầu
        startParent = transform.parent;
        startSiblingIndex = transform.GetSiblingIndex();
        startAnchoredPos = self.anchoredPosition;

        IsPlaced = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsPlaced) return;

        // lưu lại vị trí trước khi kéo (để thả sai thì về lại)
        startParent = transform.parent;
        startSiblingIndex = transform.GetSiblingIndex();
        startAnchoredPos = self.anchoredPosition;

        // cho nổi lên trên cùng
        transform.SetAsLastSibling();

        // đang kéo thì không chặn raycast (để UI khác bắt được nếu cần)
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsPlaced) return;
        if (dragRoot == null) return;

        // convert screen position -> local position trong dragRoot (piecesArea)
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        self.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsPlaced) return;

        canvasGroup.blocksRaycasts = true;

        if (correctSlot == null)
        {
            ReturnToStart();
            return;
        }

        // tính khoảng cách giữa mảnh và slot đúng
        Vector2 slotPosInDragRoot = WorldToLocalIn(dragRoot, correctSlot.position);
        float dist = Vector2.Distance(self.anchoredPosition, slotPosInDragRoot);

        // nếu đủ gần -> snap và khóa
        if (dist <= snapDistance)
        {
            SnapToSlot();
        }
        else
        {
            ReturnToStart();
        }
    }

    void SnapToSlot()
    {
        // đưa mảnh về đúng slot (cùng parent với slotsParent)
        // để mảnh nằm đúng vị trí và không bị lệch bởi scale/anchor
        Transform slotParent = correctSlot.parent;

        // giữ world position để khỏi nhảy sai khi đổi parent
        transform.SetParent(slotParent, worldPositionStays: true);

        // đặt đúng vị trí slot
        self.position = correctSlot.position;

        // khóa kéo
        IsPlaced = true;

        // đảm bảo mảnh nằm trên ghost/slot (tuỳ bạn)
        transform.SetAsLastSibling();
    }

    void ReturnToStart()
    {
        // quay lại đúng parent & vị trí ban đầu
        transform.SetParent(startParent, worldPositionStays: false);
        self.anchoredPosition = startAnchoredPos;
        transform.SetSiblingIndex(startSiblingIndex);
    }

    Vector2 WorldToLocalIn(RectTransform target, Vector3 worldPos)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out local
        );
        return local;
    }
}
