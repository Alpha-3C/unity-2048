using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Places the board and controls inside the visible safe area on desktop and mobile.
/// The 4x4 grid keeps the same proportions as the 1024-unit source artwork.
/// </summary>
public class ResponsiveBoardLayout : MonoBehaviour
{
    private RectTransform board;
    private RectTransform score;
    private RectTransform restart;
    private RectTransform canvasRect;
    private GridLayoutGroup grid;

    private int previousWidth;
    private int previousHeight;
    private Rect previousSafeArea;

    public void Configure(RectTransform boardRect, RectTransform scoreRect, RectTransform restartRect)
    {
        board = boardRect;
        score = scoreRect;
        restart = restartRect;

        if (board == null || score == null || restart == null) return;

        Canvas canvas = board.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        canvasRect = canvas.transform as RectTransform;
        grid = board.GetComponent<GridLayoutGroup>();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        Canvas.ForceUpdateCanvases();
        ApplyLayout();
    }

    private void Update()
    {
        if (canvasRect == null) return;

        if (Screen.width != previousWidth || Screen.height != previousHeight ||
            Screen.safeArea != previousSafeArea)
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        if (canvasRect == null || grid == null || Screen.width == 0 || Screen.height == 0)
            return;

        Rect safeArea = Screen.safeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, safeArea.min, null, out Vector2 lowerLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, safeArea.max, null, out Vector2 upperRight);

        float safeWidth = upperRight.x - lowerLeft.x;
        float safeHeight = upperRight.y - lowerLeft.y;
        if (safeWidth <= 0f || safeHeight <= 0f) return;

        Vector2 safeCenter = (lowerLeft + upperRight) * 0.5f;
        bool portrait = safeHeight > safeWidth;

        float boardSize = portrait
            ? Mathf.Min(safeWidth * 0.9f, safeHeight - 260f)
            : Mathf.Min(safeHeight * 0.88f, safeWidth * 0.62f);
        boardSize = Mathf.Max(200f, boardSize);

        board.sizeDelta = new Vector2(boardSize, boardSize);
        float boardScale = boardSize / 1024f;
        int padding = Mathf.RoundToInt(32f * boardScale);
        grid.padding = new RectOffset(padding, padding, padding, padding);
        grid.cellSize = Vector2.one * (216f * boardScale);
        grid.spacing = Vector2.one * (32f * boardScale);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        if (portrait)
        {
            board.anchoredPosition = safeCenter + new Vector2(0f, -110f);

            float headerY = board.anchoredPosition.y + boardSize * 0.5f + 125f;
            float controlOffset = boardSize * 0.25f;
            float controlWidth = Mathf.Min(330f, (boardSize - 40f) * 0.5f);

            score.anchoredPosition = new Vector2(safeCenter.x - controlOffset, headerY);
            restart.anchoredPosition = new Vector2(safeCenter.x + controlOffset, headerY);
            score.sizeDelta = new Vector2(controlWidth, 150f);
            restart.sizeDelta = new Vector2(controlWidth, 100f);
        }
        else
        {
            float boardX = upperRight.x - 32f - boardSize * 0.5f;
            board.anchoredPosition = new Vector2(boardX, safeCenter.y);

            float controlsRight = boardX - boardSize * 0.5f - 32f;
            float controlsX = (lowerLeft.x + controlsRight) * 0.5f;
            float controlWidth = Mathf.Min(350f, (controlsRight - lowerLeft.x) * 0.8f);

            score.anchoredPosition = new Vector2(controlsX, safeCenter.y + 100f);
            restart.anchoredPosition = new Vector2(controlsX, safeCenter.y - 80f);
            score.sizeDelta = new Vector2(controlWidth, 160f);
            restart.sizeDelta = new Vector2(controlWidth, 100f);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(board);
        previousWidth = Screen.width;
        previousHeight = Screen.height;
        previousSafeArea = safeArea;
    }
}
