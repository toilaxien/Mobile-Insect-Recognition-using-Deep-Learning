using System;
using System.Collections.Generic;

[Serializable]
public class SpeciesData
{
    // --- PHẦN KHỚP VỚI JSON (BẮT BUỘC) ---
    public string id;           // JSON là "id"
    public string displayName;
    public string intro;
    
    public string clipKey;      // JSON là "clipKey"
    public string audioKey;     // JSON là "audioKey"
    
    public List<StageData> stages;

    // --- PHẦN HỖ TRỢ CODE CŨ (Để Controller không bị lỗi) ---
    // Các dòng này giúp bạn vẫn dùng .label, .video như cũ mà code tự trỏ sang biến mới
    public string label => id;
    public string video => clipKey;
    public string audio => audioKey;
}

[Serializable]
public class StageData
{
    public string title;
    public string duration;
    public string environment;
    public string description;

    // --- PHẦN KHỚP VỚI JSON ---
    public string feature;      // JSON là "feature" (số ít)
    public string modelKey;     // JSON là "modelKey"

    // --- PHẦN HỖ TRỢ CODE CŨ ---
    public string features => feature;
    public string model3D => modelKey;
}