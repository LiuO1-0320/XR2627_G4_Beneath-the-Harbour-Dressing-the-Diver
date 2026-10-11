using UnityEngine;
using UnityEngine.UI;

/// <summary>Restores the shared CJK font for saved, independently editable exhibit captions.</summary>
[ExecuteAlways, RequireComponent(typeof(Text))]
public class HarbourStaticCaption : MonoBehaviour
{
    static Font sharedFont;
    void OnEnable()
    {
        if (!sharedFont)
            sharedFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 48);
        GetComponent<Text>().font = sharedFont;
    }
}
