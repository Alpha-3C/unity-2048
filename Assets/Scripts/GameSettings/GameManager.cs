using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    Playing,
    Victory,
    Defeat
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("棋盘中的 16 个格子，按行从左到右排列")]
    [SerializeField] private GameObject[] GOcells;

    [Header("各数值的图片模板")]
    [SerializeField] private GameObject BlankGO;
    [SerializeField] private GameObject GO2, GO4, GO8, GO16, GO32, GO64;
    [SerializeField] private GameObject GO128, GO256, GO512, GO1024, GO2048;

    [Header("输入与分数")]
    [SerializeField] private TMP_Text ScoreText;
    [SerializeField] private Button newGameButton;

    [SerializeField, Min(0.01f)]
    private float slideDuration = 0.12f;

    private bool isAnimating;
    private Coroutine slideCoroutine;


    private readonly List<MovingTileImage> movingImages = new List<MovingTileImage>();

    private IInputDetector keyboardDetector;
    private IInputDetector swipeDetector;
    private Image[] cellImages;
    private ItemMatrix matrix;
    private GameState gameState;
    private int score;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheCellImages();

        if (cellImages != null && ScoreText != null && newGameButton != null)
        {
            ResponsiveBoardLayout layout = GetComponent<ResponsiveBoardLayout>();
            if (layout == null) layout = gameObject.AddComponent<ResponsiveBoardLayout>();
            layout.Configure(cellImages[0].transform.parent as RectTransform,
                ScoreText.rectTransform, newGameButton.transform as RectTransform);
        }

        if (newGameButton != null)
            newGameButton.onClick.AddListener(Initialize);

        // 两种输入都启用；已有组件则复用，没有则在运行时添加。
        keyboardDetector = GetComponent<ArrowKeysDetector>();
        if (keyboardDetector == null)
            keyboardDetector = gameObject.AddComponent<ArrowKeysDetector>();

        swipeDetector = GetComponent<SwipeDetector>();
        if (swipeDetector == null)
            swipeDetector = gameObject.AddComponent<SwipeDetector>();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        CancelSlide();

        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(Initialize);

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (gameState != GameState.Playing) return;

        // 每帧都轮询两个检测器，让鼠标按下/松开的状态连续更新。
        SwipeDirection? keyboardDirection = keyboardDetector.DetectInputDirection();
        SwipeDirection? swipeDirection = swipeDetector.DetectInputDirection();
        SwipeDirection? direction = keyboardDirection ?? swipeDirection;
        if (direction.HasValue) TryMove(direction.Value);
    }

    // 开始新的一局；以后可以直接绑到“重新开始”按钮。
    public void Initialize()
    {
        CancelSlide();

        matrix = new ItemMatrix();
        score = 0;
        gameState = GameState.Playing;

        SpawnRandomItem();   //开局生成两个块
        SpawnRandomItem();
        RefreshUI();
    }

    private class MovingTileImage
    {
        public Image _image;
        public Vector3 Start;
        public Vector3 End;
    }

    // 键盘、滑动和按钮最终都可以调用这个入口。
    public void TryMove(SwipeDirection direction)
    {
        if (gameState != GameState.Playing || 
            matrix == null || 
            cellImages == null || 
            isAnimating)     return;
        SwipeResult result = matrix.OperateSwipe(direction);
        if (!result.Changed) return;

        isAnimating = true;
        slideCoroutine = StartCoroutine(PlaySwipe(result));
        /*// 只有棋盘实际变化，才生成新砖块。
        score += result.ScoreGained;
        SpawnRandomItem();
        UpdateGameState();
        RefreshUI();*/
    }

    private void UpdateGameState()
    {
        bool canMove = false;

        for (int row = 0; row < Globals.Rows; row++)
        {
            for (int column = 0; column < Globals.Columns; column++)
            {
                Item item = matrix[row, column];
                if (item == null)
                {
                    canMove = true;
                    continue;
                }

                if (item.Value >= 2048)
                {
                    gameState = GameState.Victory;
                    return;
                }

                // 只看右、下邻格，每一对相邻格检查一次即可。
                if (column + 1 < Globals.Columns && matrix[row, column + 1]?.Value == item.Value)
                    canMove = true;
                if (row + 1 < Globals.Rows && matrix[row + 1, column]?.Value == item.Value)
                    canMove = true;
            }
        }

        gameState = canMove ? GameState.Playing : GameState.Defeat;
    }

    private void SpawnRandomItem()
    {
        matrix.GetEmptyPosition(out int row, out int column);
        if (row < 0) return;

        matrix[row, column] = new Item
        {
            Value = Random.value < 0.9f ? 2 : 4,
            Row = row,
            Column = column
        };
    }

    private void RefreshUI()
    {
        UpdateMatrixUI();
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (ScoreText == null) return;

        string result = gameState == GameState.Victory ? "\nYou win!" :
                        gameState == GameState.Defeat ? "\nGame over" : "";
        ScoreText.text = "Score: " + score + result;
    }

    private void UpdateMatrixUI()
    {
        if (cellImages == null) return;

        for (int row = 0; row < Globals.Rows; row++)
        {
            for (int column = 0; column < Globals.Columns; column++)
            {
                UpdateCellUI(row, column);
            }
        }
    }

    private void UpdateCellUI(int row, int column)
    {
        int index = row * Globals.Columns + column;
        int value = matrix[row, column]?.Value ?? 0;
        cellImages[index].sprite = GetSpriteForValue(value);
    }

    private Sprite GetSpriteForValue(int value)
    {
        GameObject template;
        switch (value)
        {
            case 0: template = BlankGO; break;
            case 2: template = GO2; break;
            case 4: template = GO4; break;
            case 8: template = GO8; break;
            case 16: template = GO16; break;
            case 32: template = GO32; break;
            case 64: template = GO64; break;
            case 128: template = GO128; break;
            case 256: template = GO256; break;
            case 512: template = GO512; break;
            case 1024: template = GO1024; break;
            case 2048: template = GO2048; break;
            default:
                Debug.LogError("没有配置数值 " + value + " 的方块图片。", this);
                return null;
        }

        if (template != null && template.TryGetComponent(out Image image))
            return image.sprite;

        Debug.LogError("方块模板缺失或没有 Image 组件：" + value, this);
        return null;
    }

    private void CacheCellImages()
    {
        int expectedCount = Globals.Rows * Globals.Columns;
        if (GOcells == null || GOcells.Length != expectedCount)
        {
            Debug.LogError("GOcells 需要按行排列并配置 " + expectedCount + " 个格子。", this);
            return;
        }

        cellImages = new Image[expectedCount];
        for (int index = 0; index < expectedCount; index++)
        {
            if (GOcells[index] == null || !GOcells[index].TryGetComponent(out cellImages[index]))
            {
                Debug.LogError("GOcells[" + index + "] 未配置或缺少 Image 组件。", this);
                cellImages = null;
                return;
            }
        }
    }

    private IEnumerator PlaySwipe(SwipeResult result)
    {
        CreateMovingImages(result);

        Sprite blankSprite = GetSpriteForValue(0);
        foreach(Image cell in cellImages)
        {
            cell.sprite = blankSprite;
        }

        float duration = Mathf.Max(0.01f, slideDuration);
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
            float progress = elapsed / duration;

            foreach(MovingTileImage tile in movingImages)
            {
                tile._image.rectTransform.position = Vector3.Lerp(tile.Start, tile.End, progress);
            }

            yield return null;
        }

        ClearMovingImages();

        score += result.ScoreGained;
        SpawnRandomItem();
        UpdateGameState();
        RefreshUI();

        isAnimating = false;
        slideCoroutine = null;

    }

    private void CreateMovingImages(SwipeResult result)
    {
        // 临时图片必须放到 Canvas 下，避免影响 GridLayoutGroup。
        Canvas canvas = cellImages[0].GetComponentInParent<Canvas>();

        foreach(TileMove move in result.Moves)
        {
            int fromIndex = move.From.Row * Globals.Columns + move.From.Column;

            int toIndex = move.To.Row * Globals.Columns + move.To.Column;

            RectTransform fromRect = cellImages[fromIndex].rectTransform;
            RectTransform toRect = cellImages[toIndex].rectTransform;

            GameObject temporary = new GameObject("MovingTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            temporary.transform.SetParent(canvas.transform, false);
            Image image = temporary.GetComponent<Image>();
            image.sprite = GetSpriteForValue(move.Value);
            image.preserveAspect = cellImages[fromIndex].preserveAspect;
            image.raycastTarget = false;

            RectTransform movingRect = image.rectTransform;
            movingRect.pivot = fromRect.pivot;
            movingRect.sizeDelta = fromRect.rect.size;
            movingRect.position = fromRect.position;

            movingImages.Add(new MovingTileImage
            {
                _image = image,
                Start = fromRect.position,
                End = toRect.position
            });


        }
    }

    private void ClearMovingImages()
    {
        foreach(var tile in movingImages)
        {
            if (tile._image == null) continue;

            tile._image.gameObject.SetActive(false);
            Destroy(tile._image.gameObject);
        }

        movingImages.Clear();
    }

    private void CancelSlide()
    {
        if(slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }

        ClearMovingImages();
        isAnimating = false;
    }
}
