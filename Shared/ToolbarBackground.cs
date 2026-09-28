using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Shared by every plugin that clones a Studio toolbar button (linked into each .csproj, compiled per assembly, so
// it is internal and namespace-less on purpose). Vanilla toolbar icons have their dark patterned square baked into
// the sprite; ours are transparent and looked like they floated in nothing. The button's own Image
// now shows that square (toolbar_bg.png, 32x32, embedded with LogicalName "ToolbarBackground.png") and the icon moves
// to a child Image on top - callers keep swapping / tinting the returned icon Image exactly as before.
internal static class ToolbarBackground
{
    private static Sprite _sprite;

    /// <summary>Puts the background on the cloned button and returns the Image the icon should use from now on.</summary>
    public static Image Apply(GameObject button)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null)
            return null;
        Sprite bg = BgSprite;
        if (bg == null)
            return buttonImage; // no background available: behave exactly as before

        Transform old = button.transform.Find("Icon");
        GameObject iconGo = old != null ? old.gameObject : new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(button.transform, false);
        RectTransform rt = (RectTransform)iconGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image icon = iconGo.GetComponent<Image>();
        if (icon == null)
            icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false; // clicks stay on the button
        icon.preserveAspect = true;
        icon.sprite = buttonImage.sprite;

        buttonImage.sprite = bg;
        buttonImage.type = Image.Type.Simple;
        buttonImage.color = Color.white;
        return icon;
    }

    private static Sprite BgSprite
    {
        get
        {
            if (_sprite != null)
                return _sprite;
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ToolbarBackground.png"))
                {
                    if (s == null)
                        return null;
                    byte[] bytes = new byte[s.Length];
                    s.Read(bytes, 0, bytes.Length);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes) == false)
                        return null;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (Exception)
            {
                return null;
            }
            return _sprite;
        }
    }
}
