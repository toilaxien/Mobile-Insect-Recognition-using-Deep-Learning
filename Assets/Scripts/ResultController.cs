// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.Video;

// public class ResultController : MonoBehaviour
// {
//     #region --- CONFIGURATION ---
//     [Header("Scene Navigation")]
//     [Tooltip("Tên scene dùng để quét lại")]
//     public string scanSceneName = "ScanScene";
//     [Tooltip("Tên scene hiển thị mô hình 3D")]
//     public string scene3DName = "3DScene";

//     [Header("UI Components")]
//     public TMP_Text speciesTitleText;
    
//     [Header("Video Player")]
//     public RawImage videoRawImage;
//     public VideoPlayer videoPlayer;
//     public RenderTexture videoRT;

//     [Header("Scroll View Content")]
//     public Transform contentParent;     // Nơi chứa các StageItem
//     public StageItemUI stageItemPrefab; // Prefab UI cho từng giai đoạn

//     [Header("Buttons")]
//     public Button btnSave;
//     public Button btnBack;

//     [Header("Audio")]
//     public AudioSource narrationSource;
//     #endregion

//     private void Start()
//     {
//         // 1. Kiểm tra lỗi từ lần quét trước
//         if (!string.IsNullOrEmpty(PredictStore.errorMessage))
//         {
//             Debug.LogError($"Predict Error: {PredictStore.errorMessage}");
//             ReturnToScan();
//             return;
//         }

//         // 2. Lấy label dự đoán
//         string label = PredictStore.predictedLabel;
//         if (string.IsNullOrEmpty(label))
//         {
//             Debug.LogError("Missing predictedLabel in PredictStore!");
//             ReturnToScan();
//             return;
//         }

//         // 3. Load dữ liệu JSON
//         SpeciesData data = LoadSpeciesData(label);
//         if (data == null)
//         {
//             Debug.LogError($"Cannot find JSON for species: {label}");
//             ReturnToScan();
//             return;
//         }

//         // 4. Cập nhật UI và Logic
//         UpdateUI(data);
//         SetupVideo(data);
//         SpawnStages(data);
//         SetupButtons();

//         // 5. Phát giọng đọc
//         StartCoroutine(PlayNarrationRoutine(data));
//     }

//     #region --- DATA LOADING ---
//     SpeciesData LoadSpeciesData(string label)
//     {
//         // Đường dẫn: Resources/Species/{label}
//         TextAsset ta = Resources.Load<TextAsset>($"Species/{label}");
//         if (ta == null) return null;
//         return JsonUtility.FromJson<SpeciesData>(ta.text);
//     }
//     #endregion

//     #region --- UI & MEDIA SETUP ---
//     void UpdateUI(SpeciesData data)
//     {
//         if (speciesTitleText != null)
//             speciesTitleText.text = data.displayName;
//     }

//     void SetupVideo(SpeciesData data)
//     {
//         if (videoPlayer == null || videoRawImage == null || string.IsNullOrEmpty(data.video)) return;

//         // Gán Render Texture nếu chưa có
//         if (videoRT != null)
//         {
//             videoPlayer.targetTexture = videoRT;
//             videoRawImage.texture = videoRT;
//         }

//         // Load Video Clip từ Resources
//         VideoClip clip = Resources.Load<VideoClip>($"Species/{data.label}/Video/{data.video}");
//         if (clip != null)
//         {
//             videoPlayer.clip = clip;
//             videoPlayer.isLooping = true;
//             videoPlayer.Play();
//         }
//         else
//         {
//             Debug.LogWarning($"Video clip not found: {data.video}");
//         }
//     }

//     void SpawnStages(SpeciesData data)
//     {
//         // Xóa các item cũ nếu có
//         foreach (Transform child in contentParent)
//         {
//             Destroy(child.gameObject);
//         }

//         if (data.stages == null) return;

//         // Tạo item mới
//         foreach (var st in data.stages)
//         {
//             StageItemUI item = Instantiate(stageItemPrefab, contentParent);
//             item.Bind(st, OnClickView3D);
//         }
//     }

//     IEnumerator PlayNarrationRoutine(SpeciesData data)
//     {
//         // Đợi 1 frame để đảm bảo UI load xong mượt mà
//         yield return null; 

//         if (narrationSource != null && !string.IsNullOrEmpty(data.audio))
//         {
//             AudioClip clip = Resources.Load<AudioClip>($"Species/{data.label}/Audio/{data.audio}");
//             if (clip != null)
//             {
//                 narrationSource.clip = clip;
//                 narrationSource.Play();
//             }
//             else
//             {
//                 Debug.LogWarning($"Audio clip not found: {data.audio}");
//             }
//         }
//     }
//     #endregion

//     #region --- INTERACTION ---
//     void SetupButtons()
//     {
//         if (btnBack != null)
//         {
//             btnBack.onClick.RemoveAllListeners();
//             btnBack.onClick.AddListener(OnBackButtonClicked);
//         }

//         if (btnSave != null)
//         {
//             btnSave.onClick.RemoveAllListeners();
//             btnSave.onClick.AddListener(OnSaveButtonClicked);
//         }
//     }

//     void OnBackButtonClicked()
//     {
//         // Clear dữ liệu cũ để tránh lỗi cho lần quét sau
//         PredictStore.Clear(); 
//         ReturnToScan();
//     }

//     void OnSaveButtonClicked()
//     {
//         // savedImagePath đã được lưu từ bước chụp ảnh (PredictController)
//         string path = PredictStore.savedImagePath;
//         Debug.Log($"[Save] Image saved at: {path}");
        
//         // TODO: Thêm logic hiện thông báo (Toast/Popup) "Đã lưu vào bộ sưu tập" tại đây
//     }

//     void OnClickView3D(StageData st)
//     {
//         if (string.IsNullOrEmpty(st.model3D))
//         {
//             Debug.LogWarning("This stage has no 3D model data.");
//             return;
//         }

//         // LƯU Ý: Ở đây bạn đang dùng lại biến savedImagePath để truyền tên model 3D.
//         // Điều này có thể làm mất đường dẫn ảnh chụp nếu bạn muốn quay lại màn hình này.
//         // Tốt nhất nên tạo thêm biến 'public static string selectedModel3D' trong PredictStore.
        
//         PredictStore.savedImagePath = st.model3D; // Tạm thời giữ nguyên logic của bạn
//         SceneManager.LoadScene(scene3DName);
//     }

//     void ReturnToScan()
//     {
//         SceneManager.LoadScene(scanSceneName);
//     }
//     #endregion
// }


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using System.IO;

public class ResultController : MonoBehaviour
{
    #region --- CONFIGURATION ---
    [Header("Scene Navigation")]
    public string scanSceneName = "ScanScene";
    public string scene3DName = "3DScene";

    [Header("UI Components")]
    public TMP_Text speciesTitleText;
    public TMP_Text introText; // Thêm text Intro nếu cần

    [Header("Video Player")]
    public RawImage videoRawImage;
    public VideoPlayer videoPlayer;
    public RenderTexture videoRT;

    [Header("Scroll View Content")]
    public Transform contentParent;     
    public StageItemUI stageItemPrefab; 

    [Header("Buttons")]
    public Button btnSave;
    public Button btnBack;

    [Header("Audio")]
    public AudioSource narrationSource;
    
    [Header("Button SFX / Voice")]
    public AudioSource uiVoiceSource;     // AudioSource để phát âm khi bấm nút (tách khỏi narrationSource càng tốt)
    public AudioClip backClip;            // clip đọc khi bấm Return
    public AudioClip saveClip;            // clip đọc khi bấm Save
    public bool waitForButtonClip = true; // chờ clip xong rồi mới làm

    #endregion


    private void Start()
    {
        // 1. Kiểm tra lỗi
        if (!string.IsNullOrEmpty(PredictStore.errorMessage))
        {
            Debug.LogError($"Lỗi từ Scan: {PredictStore.errorMessage}");
            ReturnToScan();
            return;
        }

        // 2. Lấy label
        string label = PredictStore.predictedLabel;
        if (string.IsNullOrEmpty(label))
        {
            Debug.LogError("Chưa có label (predictedLabel is null)!");
            ReturnToScan();
            return;
        }

        // 3. Load dữ liệu JSON
        SpeciesData data = LoadSpeciesData(label);
        if (data == null)
        {
            Debug.LogError($"Không đọc được JSON cho loài: {label}");
            ReturnToScan();
            return;
        }

        // 4. Cập nhật UI
        UpdateUI(data);
        SetupVideo(data);
        SpawnStages(data);
        SetupButtons();

        // 5. Phát giọng đọc
        StartCoroutine(PlayNarrationRoutine(data));
    }

    #region --- DATA LOADING ---
    SpeciesData LoadSpeciesData(string label)
    {
        // SỬA: Thêm "/species" vào cuối vì file tên là species.json nằm trong folder label
        string path = $"Species/{label}/species";
        
        TextAsset ta = Resources.Load<TextAsset>(path);
        if (ta == null) 
        {
            Debug.LogError($"Không tìm thấy file tại: Resources/{path}");
            return null;
        }

        try 
        {
            return JsonUtility.FromJson<SpeciesData>(ta.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON sai cú pháp: {e.Message}");
            return null;
        }
    }
    #endregion

    #region --- UI & MEDIA SETUP ---
    void UpdateUI(SpeciesData data)
    {
        if (speciesTitleText != null)
            speciesTitleText.text = data.displayName;
            
        // Nếu bạn có Text Intro
        if (introText != null)
            introText.text = data.intro;
    }

    void SetupVideo(SpeciesData data)
    {
        // SỬA: Dùng clipKey thay vì video
        if (videoPlayer == null || string.IsNullOrEmpty(data.clipKey)) return;

        if (videoRT != null)
        {
            videoPlayer.targetTexture = videoRT;
            if(videoRawImage) videoRawImage.texture = videoRT;
        }

        // SỬA: JSON đã có đường dẫn đầy đủ, load trực tiếp luôn
        VideoClip clip = Resources.Load<VideoClip>(data.clipKey);
        
        if (clip != null)
        {
            videoPlayer.clip = clip;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy Video tại: {data.clipKey}");
        }
    }

    void SpawnStages(SpeciesData data)
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (data.stages == null) return;

        foreach (var st in data.stages)
        {
            StageItemUI item = Instantiate(stageItemPrefab, contentParent);
            item.Bind(st, OnClickView3D);
        }
    }

    IEnumerator PlayNarrationRoutine(SpeciesData data)
    {
        yield return null; 

        // SỬA: Dùng audioKey thay vì audio
        if (narrationSource != null && !string.IsNullOrEmpty(data.audioKey))
        {
            // Load trực tiếp từ đường dẫn trong JSON
            AudioClip clip = Resources.Load<AudioClip>(data.audioKey);
            
            if (clip != null)
            {
                narrationSource.clip = clip;
                narrationSource.Play();
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy Audio tại: {data.audioKey}");
            }
        }
    }
    #endregion

    #region --- INTERACTION ---
    void SetupButtons()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(() => StartCoroutine(BackRoutine()));
        }

        if (btnSave != null)
        {
            btnSave.onClick.RemoveAllListeners();
            btnSave.onClick.AddListener(() => StartCoroutine(SaveRoutine()));
        }
    }

    bool isBusy = false;

    class AudioState
    {
        public AudioSource src;
        public AudioClip clip;
        public float time;
        public bool loop;
        public float volume;
    }
    List<AudioState> stoppedStates = new List<AudioState>();

    bool wasVideoPlaying = false;


    void StopAllOtherAudio()
    {
        stoppedStates.Clear();

        var allSources = FindObjectsOfType<AudioSource>(true);
        foreach (var src in allSources)
        {
            if (src == null) continue;
            if (src == uiVoiceSource) continue;

            if (src.isPlaying)
            {
                // Lưu trạng thái để phát lại
                stoppedStates.Add(new AudioState
                {
                    src = src,
                    clip = src.clip,           // chỉ khôi phục được nếu có clip
                    time = src.time,
                    loop = src.loop,
                    volume = src.volume
                });

                src.Stop();
            }
        }

        // Video
        if (videoPlayer != null)
        {
            wasVideoPlaying = videoPlayer.isPlaying;
            if (wasVideoPlaying) videoPlayer.Pause(); // dùng Pause để resume mượt hơn
        }
    }


    void RestartStoppedAudio()
    {
        foreach (var st in stoppedStates)
        {
            if (st == null || st.src == null) continue;

            // Nếu clip null (đã PlayOneShot) thì không thể tự khôi phục bằng Play()
            if (st.clip == null) continue;

            st.src.clip = st.clip;
            st.src.loop = st.loop;
            st.src.volume = st.volume;

            // Phục hồi gần đúng vị trí
            st.src.time = Mathf.Clamp(st.time, 0f, st.clip.length - 0.01f);
            st.src.Play();
        }
        stoppedStates.Clear();

        if (videoPlayer != null && wasVideoPlaying)
            videoPlayer.Play();

        wasVideoPlaying = false;
    }



    IEnumerator PlayButtonClip(AudioClip clip)
    {
        if (clip == null) yield break;

        // Chặn click + stop hết âm khác
        StopAllOtherAudio();

        // Stop chính uiVoiceSource để không bị OneShot chồng lên lần trước
        if (uiVoiceSource != null)
        {
            uiVoiceSource.Stop();
            uiVoiceSource.PlayOneShot(clip);

            if (waitForButtonClip)
                yield return new WaitForSecondsRealtime(clip.length);
        }
    }



    IEnumerator BackRoutine()
    {
        if (isBusy) yield break;
        isBusy = true;
        SetButtonsInteractable(false);

        yield return PlayButtonClip(backClip);

        PredictStore.Clear();
        ReturnToScan();

        // không cần bật lại vì đổi scene
    }

    IEnumerator SaveRoutine()
    {
        if (isBusy) yield break;
        isBusy = true;
        SetButtonsInteractable(false);

        yield return PlayButtonClip(saveClip);

        OnSaveButtonClicked();

        // bật lại âm thanh (phát lại từ đầu)
        RestartStoppedAudio();

        SetButtonsInteractable(true);
        isBusy = false;
    }



    void OnBackButtonClicked()
    {
        PredictStore.Clear(); 
        ReturnToScan();
    }

    void SetButtonsInteractable(bool on)
    {
        if (btnBack != null) btnBack.interactable = on;
        if (btnSave != null) btnSave.interactable = on;
    }

    void OnSaveButtonClicked()
    {
        string label = PredictStore.predictedLabel;
        string srcPath = PredictStore.capturedImagePath;

        if (string.IsNullOrEmpty(label))
        {
            Debug.LogError("[Save] predictedLabel rỗng => không biết lưu vào thư mục nào.");
            return;
        }

        if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath))
        {
            Debug.LogError("[Save] capturedImagePath rỗng hoặc file không tồn tại: " + srcPath);
            return;
        }

        // Thư mục lưu theo loài: persistentDataPath/SavedImages/<label>/
        string folder = Path.Combine(Application.persistentDataPath, "SavedImages", label);
        Directory.CreateDirectory(folder);

        // ✅ Luôn chỉ 1 ảnh cho mỗi loài: tên file cố định theo label
        // (ép luôn .png để chắc chắn không tạo 2 file .jpg/.png khác nhau)
        string dstPath = Path.Combine(folder, $"{label}.png");

        try
        {
            // Nếu ảnh nguồn không phải PNG thì vẫn copy byte; để chắc chắn đúng PNG, bạn cần EncodeToPNG từ Texture2D.
            // Ở đây: đơn giản nhất là ghi đè file đích bằng byte của file nguồn.
            byte[] bytes = File.ReadAllBytes(srcPath);
            File.WriteAllBytes(dstPath, bytes);

            Debug.Log($"[Save] Đã lưu/ghi đè ảnh vào: {dstPath}");
            // TODO: Toast / popup
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Save] Lưu ảnh thất bại: " + e.Message);
        }
    }



    void OnClickView3D(StageData st)
    {
        // SỬA: Dùng modelKey thay vì model3D
        if (string.IsNullOrEmpty(st.modelKey))
        {
            Debug.LogWarning("Stage này không có model 3D (modelKey rỗng)");
            return;
        }

        // Lưu đường dẫn model để Scene 3D load
        PredictStore.savedImagePath = st.modelKey; 
        
        SceneManager.LoadScene(scene3DName);
    }

    void ReturnToScan()
    {
        SceneManager.LoadScene(scanSceneName);
    }
    #endregion
}


// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.Video;

// public class ResultController : MonoBehaviour
// {
//     #region --- CẤU HÌNH (CONFIGURATION) ---
//     [Header("Debug Mode")]
//     [Tooltip("Tích vào để test trực tiếp mà không cần quét")]
//     public bool enableDebug = true; 
//     [Tooltip("Tên folder loài vật trong Resources/Species (VD: ladybug)")]
//     public string debugLabel = "ladybug";

//     [Header("Điều hướng Scene")]
//     public string scanSceneName = "ScanScene";
//     public string scene3DName = "3DScene";

//     [Header("UI Chính")]
//     public TMP_Text speciesTitleText;   // Tên loài
//     public TMP_Text introText;          // Giới thiệu (nếu có)
    
//     [Header("Video Player")]
//     public RawImage videoRawImage;
//     public VideoPlayer videoPlayer;
//     public RenderTexture videoRT;

//     [Header("Danh Sách Giai Đoạn (Scroll View)")]
//     public Transform contentParent;     
//     public StageItemUI stageItemPrefab; 

//     [Header("Nút Bấm")]
//     public Button btnSave;
//     public Button btnBack;

//     [Header("Âm Thanh")]
//     public AudioSource narrationSource;
//     #endregion

//     private void Start()
//     {
//         string labelToLoad = "";

//         // --- 1. XÁC ĐỊNH LABEL CẦN LOAD ---
//         if (enableDebug)
//         {
//             // Chế độ Test: Lấy tên từ ô Debug Label
//             labelToLoad = debugLabel;
//             Debug.LogWarning($"[DEBUG] Đang load dữ liệu loài: {labelToLoad}");
//         }
//         else
//         {
//             // Chế độ Thật: Lấy từ kết quả AI quét được
//             if (!string.IsNullOrEmpty(PredictStore.errorMessage))
//             {
//                 Debug.LogError($"Lỗi Scan: {PredictStore.errorMessage}");
//                 ReturnToScan();
//                 return;
//             }

//             labelToLoad = PredictStore.predictedLabel;
//             if (string.IsNullOrEmpty(labelToLoad))
//             {
//                 Debug.LogError("Không tìm thấy label từ PredictStore!");
//                 ReturnToScan();
//                 return;
//             }
//         }

//         // --- 2. LOAD DATA TỪ FILE JSON ---
//         SpeciesData data = LoadSpeciesData(labelToLoad);
        
//         // Nếu không load được data
//         if (data == null)
//         {
//             Debug.LogError($"LỖI: Không đọc được file JSON của loài '{labelToLoad}'. Kiểm tra lại xem đã sửa file SpeciesData.cs chưa?");
//             if (!enableDebug) ReturnToScan(); 
//             return;
//         }

//         // --- 3. HIỂN THỊ DỮ LIỆU ---
//         UpdateUI(data);
//         SetupVideo(data);
//         SpawnStages(data);
//         SetupButtons();

//         // --- 4. PHÁT AUDIO ---
//         StartCoroutine(PlayNarrationRoutine(data));
//     }

//     #region --- XỬ LÝ DỮ LIỆU (DATA LOADING) ---
//     SpeciesData LoadSpeciesData(string label)
//     {
//         // Đường dẫn file JSON: Resources/Species/{label}/species
//         // (Dựa trên cấu trúc folder bạn đã chụp ảnh)
//         string path = $"Species/{label}/species";
        
//         TextAsset ta = Resources.Load<TextAsset>(path);
        
//         if (ta == null) 
//         {
//             Debug.LogError($"Không tìm thấy file JSON tại đường dẫn: Resources/{path}");
//             return null;
//         }

//         try 
//         {
//             return JsonUtility.FromJson<SpeciesData>(ta.text);
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"File JSON bị lỗi cú pháp: {e.Message}");
//             return null;
//         }
//     }
//     #endregion

//     #region --- XỬ LÝ MEDIA (VIDEO & AUDIO) ---
//     void UpdateUI(SpeciesData data)
//     {
//         if (speciesTitleText != null)
//             speciesTitleText.text = data.displayName;
            
//         // Nếu bạn có gắn Text giới thiệu thì hiện nó ra
//         if (introText != null && !string.IsNullOrEmpty(data.intro))
//             introText.text = data.intro;
//     }

//     void SetupVideo(SpeciesData data)
//     {
//         // Lưu ý: Dùng biến 'clipKey' (đã sửa trong Data Class)
//         if (videoPlayer == null || string.IsNullOrEmpty(data.clipKey)) return;

//         // Cấu hình Render Texture để video hiện lên UI
//         if (videoRT != null)
//         {
//             videoPlayer.targetTexture = videoRT;
//             if (videoRawImage != null) videoRawImage.texture = videoRT;
//         }

//         // Load Video từ đường dẫn trong JSON
//         // JSON của bạn ghi: "Species/ladybug/clip" -> Đã đủ đường dẫn, load luôn
//         VideoClip clip = Resources.Load<VideoClip>(data.clipKey);
        
//         if (clip != null)
//         {
//             videoPlayer.clip = clip;
//             videoPlayer.isLooping = true;
//             videoPlayer.Play();
//         }
//         else
//         {
//             Debug.LogWarning($"Không tìm thấy Video clip tại: {data.clipKey}");
//         }
//     }

//     IEnumerator PlayNarrationRoutine(SpeciesData data)
//     {
//         yield return null; // Đợi 1 frame cho máy đỡ lag

//         // Lưu ý: Dùng biến 'audioKey' (đã sửa trong Data Class)
//         if (narrationSource != null && !string.IsNullOrEmpty(data.audioKey))
//         {
//             // JSON ghi: "Species/ladybug/audio" -> Load luôn
//             AudioClip clip = Resources.Load<AudioClip>(data.audioKey);
            
//             if (clip != null)
//             {
//                 narrationSource.clip = clip;
//                 narrationSource.Play();
//             }
//             else
//             {
//                 Debug.LogWarning($"Không tìm thấy Audio tại: {data.audioKey}");
//             }
//         }
//     }
//     #endregion

//     #region --- XỬ LÝ DANH SÁCH GIAI ĐOẠN (STAGES) ---
//     void SpawnStages(SpeciesData data)
//     {
//         // Xóa danh sách cũ trước khi tạo mới
//         foreach (Transform child in contentParent)
//         {
//             Destroy(child.gameObject);
//         }

//         if (data.stages == null) return;

//         foreach (var st in data.stages)
//         {
//             StageItemUI item = Instantiate(stageItemPrefab, contentParent);
//             // Truyền hàm OnClickView3D vào để nút bấm hoạt động
//             item.Bind(st, OnClickView3D);
//         }
//     }

//     void OnClickView3D(StageData st)
//     {
//         // Lưu ý: Dùng biến 'modelKey' (đã sửa trong Data Class)
//         if (string.IsNullOrEmpty(st.modelKey))
//         {
//             Debug.LogWarning("Giai đoạn này không có model 3D (modelKey rỗng)");
//             return;
//         }

//         // Lưu đường dẫn model vào biến chung để Scene 3D đọc được
//         PredictStore.savedImagePath = st.modelKey; 
        
//         // Chuyển sang Scene 3D
//         SceneManager.LoadScene(scene3DName);
//     }
//     #endregion

//     #region --- CÁC NÚT BẤM (BUTTONS) ---
//     void SetupButtons()
//     {
//         if (btnBack != null)
//         {
//             btnBack.onClick.RemoveAllListeners();
//             btnBack.onClick.AddListener(OnBackButtonClicked);
//         }

//         if (btnSave != null)
//         {
//             btnSave.onClick.RemoveAllListeners();
//             btnSave.onClick.AddListener(OnSaveButtonClicked);
//         }
//     }

//     void OnBackButtonClicked()
//     {
//         PredictStore.Clear(); 
//         ReturnToScan();
//     }

//     void OnSaveButtonClicked()
//     {
//         Debug.Log($"[Save] Đã lưu ảnh: {PredictStore.savedImagePath}");
//         // TODO: Thêm code hiện thông báo "Đã lưu" ở đây
//     }

//     void ReturnToScan()
//     {
//         SceneManager.LoadScene(scanSceneName);
//     }
//     #endregion
// }