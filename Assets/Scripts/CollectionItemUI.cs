using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionItemUI : MonoBehaviour
{
    public Image thumbnail;
    public TMP_Text labelText;
    public Button button;

    string _label;

    public void Bind(string label, Sprite sprite, System.Action<string> onClick)
    {
        _label = label;

        if (labelText) labelText.text = label;

        if (thumbnail)
        {
            thumbnail.sprite = sprite;
            thumbnail.preserveAspect = true;
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(_label));
        }
    }
}
