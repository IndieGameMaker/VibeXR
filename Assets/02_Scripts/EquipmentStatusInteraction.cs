using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VibeXR
{
    [DisallowMultipleComponent]
    public sealed class EquipmentStatusInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float interactionRange = 3f;
        [SerializeField] private TMP_FontAsset koreanFont;

        private GameObject promptCanvas;
        private GameObject statusCanvas;
        private Transform player;
        private Camera playerCamera;
        private bool isOpen;
        private Vector3 panelPosition;

        private static readonly Color Ink = new Color32(235, 243, 244, 255);
        private static readonly Color Muted = new Color32(153, 174, 180, 255);
        private static readonly Color Accent = new Color32(100, 226, 171, 255);
        private static readonly Color Panel = new Color32(13, 26, 34, 246);

        private void Awake()
        {
            playerCamera = Camera.main;
            if (playerCamera != null) player = playerCamera.transform.root;
            if (koreanFont == null) koreanFont = TMP_Settings.defaultFontAsset;

            panelPosition = transform.position + new Vector3(1.25f, 2.05f, 0f);
            BuildPrompt();
            BuildStatusPanel();
            SetOpen(false);
        }

        private void Update()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
                if (playerCamera == null) playerCamera = Camera.main;
            }

            bool inRange = player != null && Vector3.Distance(player.position, transform.position) <= interactionRange;
            if (promptCanvas != null) promptCanvas.SetActive(inRange && !isOpen);

            if (!inRange)
            {
                SetOpen(false);
                return;
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                SetOpen(!isOpen);
        }

        private void LateUpdate()
        {
            FacePlayer(promptCanvas != null ? promptCanvas.transform : null);
            FacePlayer(statusCanvas != null ? statusCanvas.transform : null);
        }

        private void OnDisable()
        {
            SetOpen(false);
            if (promptCanvas != null) promptCanvas.SetActive(false);
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            if (statusCanvas != null) statusCanvas.SetActive(open);
            if (promptCanvas != null) promptCanvas.SetActive(!open && player != null && Vector3.Distance(player.position, transform.position) <= interactionRange);
        }

        private void FacePlayer(Transform canvasTransform)
        {
            if (canvasTransform == null || playerCamera == null) return;
            Vector3 toCamera = playerCamera.transform.position - canvasTransform.position;
            if (toCamera.sqrMagnitude > 0.001f)
                canvasTransform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        private void BuildPrompt()
        {
            promptCanvas = CreateCanvas("상태 확인 안내", panelPosition + Vector3.up * 0.12f, new Vector2(520f, 88f));
            Image background = promptCanvas.GetComponentInChildren<Image>();
            background.color = new Color32(13, 26, 34, 235);
            AddText(promptCanvas.transform, "E 키를 눌러 설비 상태 확인", 27, FontStyles.Bold, Ink,
                new Vector2(0f, 0f), new Vector2(500f, 70f), TextAlignmentOptions.Center);
            promptCanvas.SetActive(false);
        }

        private void BuildStatusPanel()
        {
            statusCanvas = CreateCanvas("설비 상태 월드 UI", panelPosition, new Vector2(620f, 440f));
            Image background = statusCanvas.GetComponentInChildren<Image>();
            background.color = Panel;

            AddText(statusCanvas.transform, "SMART FACTORY   /   EQUIPMENT 01", 18, FontStyles.Bold, Accent,
                new Vector2(0f, 176f), new Vector2(570f, 32f), TextAlignmentOptions.MidlineLeft);
            AddText(statusCanvas.transform, "검사 설비 상태", 38, FontStyles.Bold, Ink,
                new Vector2(0f, 125f), new Vector2(570f, 58f), TextAlignmentOptions.MidlineLeft);
            AddText(statusCanvas.transform, "●  정상 운전 중", 25, FontStyles.Bold, Accent,
                new Vector2(0f, 73f), new Vector2(570f, 42f), TextAlignmentOptions.MidlineLeft);
            AddRule(statusCanvas.transform, new Vector2(0f, 42f), new Vector2(570f, 2f));

            AddRow(statusCanvas.transform, "운전 모드", "자동", 8f);
            AddRow(statusCanvas.transform, "컨베이어", "가동 중", -40f);
            AddRow(statusCanvas.transform, "검사 완료", "1,284 EA", -88f);
            AddRow(statusCanvas.transform, "양품률", "98.7 %", -136f);
            AddRow(statusCanvas.transform, "처리 속도", "24 EA / min", -184f);

            AddRule(statusCanvas.transform, new Vector2(0f, -224f), new Vector2(570f, 2f));
            AddText(statusCanvas.transform, "시뮬레이션 설비 정보", 17, FontStyles.Normal, Muted,
                new Vector2(0f, -255f), new Vector2(570f, 32f), TextAlignmentOptions.MidlineLeft);
            AddText(statusCanvas.transform, "E 키를 눌러 닫기", 17, FontStyles.Normal, Muted,
                new Vector2(0f, -278f), new Vector2(570f, 28f), TextAlignmentOptions.MidlineRight);
            statusCanvas.SetActive(false);
        }

        private GameObject CreateCanvas(string objectName, Vector3 position, Vector2 size)
        {
            GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, true);
            canvasObject.transform.position = position;
            canvasObject.transform.localScale = Vector3.one * 0.002f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0.5f, 0.5f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = Panel;
            return canvasObject;
        }

        private void AddRow(Transform parent, string label, string value, float y)
        {
            AddText(parent, label, 23, FontStyles.Normal, Muted, new Vector2(-20f, y),
                new Vector2(270f, 40f), TextAlignmentOptions.MidlineLeft);
            AddText(parent, value, 24, FontStyles.Bold, Ink, new Vector2(145f, y),
                new Vector2(300f, 40f), TextAlignmentOptions.MidlineRight);
        }

        private void AddRule(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject rule = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(parent, false);
            RectTransform rect = rule.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rule.GetComponent<Image>().color = new Color32(57, 78, 85, 255);
        }

        private void AddText(Transform parent, string value, float fontSize, FontStyles style, Color color,
            Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = koreanFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }
    }
}
