using UnityEngine;

namespace ScrapDash
{
    public static class ProceduralVisuals
    {
        private static Sprite _whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null) return _whiteSprite;

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "SCRAP_DASH_WhitePixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);

                _whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0.5f),
                    1f
                );
                _whiteSprite.name = "SCRAP_DASH_WhiteSprite";
                _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
                return _whiteSprite;
            }
        }

        public static GameObject Rect(
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            Transform parent = null,
            int sortingOrder = 0)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
            }
            else
            {
                go.transform.position = position;
            }

            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color)) return color;
            return Color.magenta;
        }
    }
}
