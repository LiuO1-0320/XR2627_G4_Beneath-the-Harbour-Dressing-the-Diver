using UnityEngine;
using UnityEngine.UI;

/// <summary>Non-interactive world-space cursor showing the actual head ray and dwell progress.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4GazeFeedback")]
public sealed class GazeFeedback : MonoBehaviour
{
    Canvas canvas;
    Image ring, dot;
    Texture2D texture;
    Sprite sprite;
    void Awake()
    {
        canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 300;
        var rect = GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(64,64);
        texture = new Texture2D(64,64,TextureFormat.RGBA32,false);
        for (int y=0;y<64;y++) for (int x=0;x<64;x++)
        {
            float radius = Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));
            texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(radius-22)*Mathf.Clamp01(30-radius)));
        }
        texture.Apply(); sprite = Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
        var track = MakeImage("Dwell track",64); track.sprite = sprite; track.color = new Color(1,1,1,.35f);
        ring = MakeImage("Dwell progress",64); ring.sprite = sprite; ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = 2; ring.color = new Color(.3f,1,.8f);
        dot = MakeImage("Head pointer",7); dot.color = Color.white;
    }
    Image MakeImage(string label,float size)
    {
        var obj = new GameObject(label,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        obj.transform.SetParent(transform,false); var image = obj.GetComponent<Image>();
        image.rectTransform.sizeDelta = Vector2.one*size; image.raycastTarget = false; return image;
    }
    public void Show(Camera camera, float distance, float progress, bool target)
    {
        canvas.enabled = true;
        distance = Mathf.Clamp(distance-.015f,.15f,8);
        transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*distance,camera.transform.rotation);
        transform.localScale = Vector3.one * distance * .00055f;
        ring.fillAmount = progress; dot.color = target ? new Color(.3f,1,.8f) : Color.white;
    }
    public void Hide() { if (canvas) canvas.enabled = false; }
    void OnDestroy() { if (sprite) Destroy(sprite); if (texture) Destroy(texture); }
}
