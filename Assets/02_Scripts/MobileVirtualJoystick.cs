using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VibeXR
{
    /// <summary>Creates a touch movement stick for handheld devices.</summary>
    public sealed class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float StickSize = 150f;
        private const float HandleSize = 68f;
        private const float DeadZone = 0.12f;

        private RectTransform baseRect;
        private RectTransform handleRect;
        private Vector2 input;
        private float movementRadius;

        public Vector2 Input => input;

        public static MobileVirtualJoystick Create()
        {
            var existing = FindAnyObjectByType<MobileVirtualJoystick>();
            if (existing != null) return existing;

            var root = new GameObject("Mobile Movement Joystick", typeof(RectTransform));
            return root.AddComponent<MobileVirtualJoystick>();
        }

        private void Awake()
        {
            BuildInterface();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Mobile Controls Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var baseObject = CreateImage("Joystick Base", canvasObject.transform, new Color(0.08f, 0.12f, 0.16f, 0.46f), StickSize);
            baseRect = baseObject.GetComponent<RectTransform>();
            baseRect.anchorMin = Vector2.zero;
            baseRect.anchorMax = Vector2.zero;
            baseRect.pivot = Vector2.zero;
            baseRect.anchoredPosition = new Vector2(52f, 52f);
            movementRadius = (StickSize - HandleSize) * 0.5f;

            var handleObject = CreateImage("Joystick Handle", baseObject.transform, new Color(0.82f, 0.91f, 0.96f, 0.78f), HandleSize);
            handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateImage(string objectName, Transform parent, Color color, float size)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            var rect = imageObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);

            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            image.sprite = CreateCircleSprite();
            image.type = Image.Type.Simple;
            return imageObject;
        }

        private static Sprite circleSprite;

        private static Sprite CreateCircleSprite()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Joystick Circle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center - 1f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius + 0.5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            circleSprite.name = "Runtime Joystick Circle";
            return circleSprite;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            input = Vector2.zero;
            if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;
        }

        private void UpdateInput(PointerEventData eventData)
        {
            if (baseRect == null || handleRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out var localPoint)) return;

            var centered = localPoint - baseRect.rect.center;
            var clamped = Vector2.ClampMagnitude(centered, movementRadius);
            handleRect.anchoredPosition = clamped;

            var normalized = clamped / movementRadius;
            input = normalized.magnitude < DeadZone ? Vector2.zero : normalized;
        }

        private void OnDisable()
        {
            input = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (circleSprite != null)
            {
                var texture = circleSprite.texture;
                Destroy(circleSprite);
                if (texture != null) Destroy(texture);
                circleSprite = null;
            }
        }
    }
}
