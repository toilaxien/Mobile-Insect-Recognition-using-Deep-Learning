using UnityEngine;

public class TimedTextHighlighter : MonoBehaviour
{
    [System.Serializable]
    public class TimedTarget
    {
        public KidTextEffect effect;   // gắn script KidTextEffect của chữ nút
        public float startTime;        // THỜI GIAN BẮT ĐẦU (giây)
        public float endTime;          // THỜI GIAN KẾT THÚC (giây)

        [HideInInspector] public bool isOn;
    }

    public TimedTarget[] targets;

    public bool playOnStart = true;    
    private float timer = 0f;
    private bool running = false;

    void Start()
    {
        if (playOnStart)
            StartSequence();
    }

    void Update()
    {
        if (!running || targets == null || targets.Length == 0) return;

        timer += Time.deltaTime;

        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (t.effect == null) continue;

            bool inWindow = timer >= t.startTime && timer <= t.endTime;

            if (inWindow && !t.isOn) TurnOn(t);
            if (!inWindow && t.isOn) TurnOff(t);
        }
    }

    public void StartSequence()
    {
        timer = 0f;
        running = true;

        for (int i = 0; i < targets.Length; i++)
            TurnOff(targets[i]);
    }

    public void StopSequence()
    {
        running = false;
        for (int i = 0; i < targets.Length; i++)
            TurnOff(targets[i]);
    }

    void TurnOn(TimedTarget t)
    {
        t.isOn = true;
        t.effect.StartHighlight();
    }

    void TurnOff(TimedTarget t)
    {
        if (t.effect == null) return;
        t.isOn = false;
        t.effect.StopHighlight();
    }
}
