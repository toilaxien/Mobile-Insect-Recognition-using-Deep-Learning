


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource bgmSource;          // AudioSource nhạc nền
    public AudioClip bgmClip;              // clip nhạc nền

    public AudioSource sfxSource;          // AudioSource phát thông báo
    public AudioClip winClip;              // clip "Ghép thành công"

    [Range(0f, 1f)] public float bgmNormalVolume = 0.8f;
    [Range(0f, 1f)] public float bgmDuckedVolume = 0.2f;
    public float duckFadeTime = 0.15f;     // thời gian hạ/tăng volume

    [Header("Scene")]
    public string collectionSceneName = "CollectionScene";

    [Header("UI References")]
    public RectTransform board;          // vùng ghép
    public RectTransform piecesArea;     // vùng chứa mảnh (xấp bài)

    [Header("Ghost (faded full image)")]
    public Image boardGhostImage;
    [Range(0f, 1f)] public float ghostAlpha = 0.3f;

    [Header("Slots (grid)")]
    public RectTransform slotsParent;    // con của Board
    public Image slotPrefab;             // Image prefab dùng làm ô
    public bool showSlotGrid = true;
    [Range(0f, 1f)] public float slotAlpha = 0.25f;

    [Header("Piece Prefab")]
    public Image piecePrefab;            // Image prefab dùng làm mảnh

    [Header("Grid Config")]
    [Min(2)] public int columns = 3;
    [Min(2)] public int rows = 4;

    [Header("Snap Config")]
    [Tooltip("Khoảng cách snap tính theo pixel UI (càng lớn càng dễ ăn)")]
    public float snapDistance = 80f;

    [Header("Deck (stack pieces at top-right)")]
    public bool stackAsDeck = true;

    [Tooltip("Cách mép phải (x) và mép trên (y). Tăng Y để xích xuống.")]
    public Vector2 deckPadding = new Vector2(40f, 160f);

    [Tooltip("Độ lệch mỗi lá (tạo cảm giác xếp bài).")]
    public Vector2 deckOffsetPerPiece = new Vector2(-6f, -6f);

    [Tooltip("Chỉ lệch tối đa bao nhiêu lá để khỏi trôi quá xa.")]
    public int deckMaxVisibleOffset = 12;

    [Tooltip("Độ lộn xộn thêm vào mỗi lá (random lệch nhẹ).")]
    public float deckJitter = 3f;

    [Header("Images (Resources)")]
    public string resourcesFolder = "PuzzleImages"; // Assets/Resources/PuzzleImages
    public bool loopImages = true;

    [Header("Buttons")]
    public Button btnNext;
    public Button btnPrev;
    public Button btnBackToCollection;

    [Header("Win UI")]
    public GameObject winPanel;
    public TMP_Text winText;
    public float winDelay = 1.2f;

    private Sprite[] sprites;
    private int index = 0;
    private bool isBusy = false;

    private readonly List<PuzzlePieceDrag> pieces = new();
    private readonly List<RectTransform> slots = new();

    void Start()
    {
        sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"❌ Không tìm thấy ảnh trong Resources/{resourcesFolder}. " +
                           $"Hãy để ảnh ở Assets/Resources/{resourcesFolder}/ và import dạng Sprite(2D and UI).");
            return;
        }
        PlayBGM();
        if (btnNext) btnNext.onClick.AddListener(NextImage);
        if (btnPrev) btnPrev.onClick.AddListener(PrevImage);

        if (btnBackToCollection)
            btnBackToCollection.onClick.AddListener(() => SceneManager.LoadScene(collectionSceneName));

        if (winPanel) winPanel.SetActive(false);

        SpawnCurrent();
        RefreshNavButtons();
    }

    void RefreshNavButtons()
    {
        if (!loopImages)
        {
            if (btnPrev) btnPrev.interactable = index > 0;
            if (btnNext) btnNext.interactable = index < sprites.Length - 1;
        }
    }

    public void NextImage()
    {
        if (isBusy) return;

        index++;
        if (index >= sprites.Length) index = loopImages ? 0 : sprites.Length - 1;

        SpawnCurrent();
        RefreshNavButtons();
    }

    public void PrevImage()
    {
        if (isBusy) return;

        index--;
        if (index < 0) index = loopImages ? sprites.Length - 1 : 0;

        SpawnCurrent();
        RefreshNavButtons();
    }

    void SpawnCurrent()
    {
        StartCoroutine(SpawnRoutine(sprites[index]));
    }

    IEnumerator SpawnRoutine(Sprite fullSprite)
    {
        isBusy = true;

        // Clear
        ClearChildren(piecesArea);
        ClearChildren(slotsParent);
        pieces.Clear();
        slots.Clear();
        if (winPanel) winPanel.SetActive(false);

        // Ghost
        if (boardGhostImage != null)
        {
            boardGhostImage.sprite = fullSprite;
            boardGhostImage.color = new Color(1f, 1f, 1f, ghostAlpha);
            boardGhostImage.preserveAspect = false;
            boardGhostImage.raycastTarget = false;
            boardGhostImage.enabled = true;
        }

        // Slot size
        float slotW = board.rect.width / columns;
        float slotH = board.rect.height / rows;

        // Create slots (top-down)
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < columns; c++)
        {
            var slotImg = Instantiate(slotPrefab, slotsParent);
            slotImg.raycastTarget = false;

            var rt = slotImg.rectTransform;
            rt.sizeDelta = new Vector2(slotW, slotH);
            rt.anchoredPosition = CellToBoardLocalPos_TopDown(c, r, slotW, slotH);

            if (showSlotGrid)
                slotImg.color = new Color(1f, 1f, 1f, slotAlpha);
            else
                slotImg.color = new Color(1f, 1f, 1f, 0f);

            slots.Add(rt);
        }

        // Build random order for pieces each spawn
        int total = rows * columns;
        List<int> order = new List<int>(total);
        for (int i = 0; i < total; i++) order.Add(i);
        Shuffle(order);

        // Slice texture
        Texture2D tex = fullSprite.texture;
        int pieceW = tex.width / columns;
        int pieceH = tex.height / rows;

        Rect areaRect = piecesArea.rect;

        // Spawn pieces in random order but keep correct slot mapping
        foreach (int slotIndex in order)
        {
            int r = slotIndex / columns;  // top-down row
            int c = slotIndex % columns;

            // Unity texture origin = bottom-left => invert row to cut from top
            int rr = rows - 1 - r;

            Rect cut = new Rect(c * pieceW, rr * pieceH, pieceW, pieceH);
            Sprite pieceSprite = Sprite.Create(tex, cut, new Vector2(0.5f, 0.5f), 100f);

            var img = Instantiate(piecePrefab, piecesArea);
            img.sprite = pieceSprite;
            img.preserveAspect = false;
            img.raycastTarget = true;

            var prt = img.rectTransform;
            prt.sizeDelta = new Vector2(slotW, slotH);

            // Deck position (stack)
            if (stackAsDeck)
                prt.anchoredPosition = GetDeckPosition(areaRect, pieces.Count);
            else
                prt.anchoredPosition = RandomInside(areaRect);

            // Drag
            var drag = img.GetComponent<PuzzlePieceDrag>();
            if (drag == null) drag = img.gameObject.AddComponent<PuzzlePieceDrag>();

            // yêu cầu: kéo gần đúng là ăn → snapDistance đưa vào đây
            drag.Init(piecesArea, slots[slotIndex], snapDistance);

            pieces.Add(drag);
        }

        yield return null;
        isBusy = false;
    }

    void Update()
    {
        if (isBusy) return;
        if (pieces.Count == 0) return;

        for (int i = 0; i < pieces.Count; i++)
            if (!pieces[i].IsPlaced) return;

        StartCoroutine(WinRoutine());
        pieces.Clear(); // tránh gọi nhiều lần
    }

    IEnumerator WinRoutine()
    {
        isBusy = true;

        // show UI
        if (winPanel) winPanel.SetActive(true);
        if (winText) winText.text = "Ghép thành công!";

        // hạ nhạc nền
        yield return StartCoroutine(DuckBGM(true));

        // phát âm thanh win
        float wait = winDelay;

        if (sfxSource != null && winClip != null)
        {
            sfxSource.Stop();
            sfxSource.PlayOneShot(winClip);
            wait = Mathf.Max(winDelay, winClip.length); // chờ ít nhất bằng độ dài clip
        }

        yield return new WaitForSecondsRealtime(wait);

        // tăng nhạc nền lại
        yield return StartCoroutine(DuckBGM(false));

        isBusy = false;
        NextImage();
    }


    // ---------- Helpers ----------

    void ClearChildren(RectTransform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    Vector2 CellToBoardLocalPos_TopDown(int c, int r, float w, float h)
    {
        float xLeft = -board.rect.width / 2f + w / 2f;
        float yTop  =  board.rect.height / 2f - h / 2f;
        return new Vector2(xLeft + c * w, yTop - r * h);
    }

    Vector2 GetDeckPosition(Rect areaRect, int pieceIndex)
    {
        // góc phải trên của piecesArea, nhưng xích xuống bằng deckPadding.y
        Vector2 basePos = new Vector2(
            areaRect.xMax - deckPadding.x -120f,
            areaRect.yMax - deckPadding.y -120f
        );

        int k = Mathf.Min(pieceIndex, deckMaxVisibleOffset);

        Vector2 offset = deckOffsetPerPiece * k;

        // random lộn xộn nhẹ
        Vector2 jitter = new Vector2(
            Random.Range(-deckJitter, deckJitter),
            Random.Range(-deckJitter, deckJitter)
        );

        return basePos + offset + jitter;
    }

    Vector2 RandomInside(Rect rect)
    {
        return new Vector2(
            Random.Range(rect.xMin, rect.xMax),
            Random.Range(rect.yMin, rect.yMax)
        );
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
        void PlayBGM()
    {
        if (bgmSource == null || bgmClip == null) return;

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmNormalVolume;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    IEnumerator DuckBGM(bool duck)
    {
        if (bgmSource == null) yield break;

        float start = bgmSource.volume;
        float target = duck ? bgmDuckedVolume : bgmNormalVolume;

        float t = 0f;
        while (t < duckFadeTime)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(start, target, t / duckFadeTime);
            yield return null;
        }
        bgmSource.volume = target;
    }

}
