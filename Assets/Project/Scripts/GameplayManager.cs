using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Connect.Common;

namespace Connect.Core
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance;

        [Header("UI")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private GameObject _winText;
        [SerializeField] private SpriteRenderer _clickHighlight;

        [Header("Prefabs")]
        [SerializeField] private SpriteRenderer _boardPrefab;
        [SerializeField] private SpriteRenderer _bgCellPrefab;
        [SerializeField] private Node _nodePrefab;

        [Header("Colors")]
        public List<Color> NodeColors;

        private RuntimeLevelData CurrentLevelData;
        private List<Node> _nodes;
        public Dictionary<Vector2Int, Node> _nodeGrid;

        private Node startNode;
        private float timeLeft;
        public bool hasGameFinished;

        private Dictionary<int, bool> activeColors = new();

        private Dictionary<Vector2Int, SpriteRenderer> _bgGrid;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            hasGameFinished = false;
            _winText.SetActive(false);
            activeColors.Clear();

            _titleText.text = $"Level - {GameManager.Instance.CurrentLevel}";

            CurrentLevelData = GameManager.Instance.GetGeneratedLevel();

            InitTimer(CurrentLevelData.timeLimit);
            SpawnBoard(CurrentLevelData.boardSize);
            SpawnNodes();
        }

        #region TIMER
        private void InitTimer(float time)
        {
            timeLeft = time;
            UpdateTimerUI();
        }

        private void UpdateTimerUI()
        {
            _timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        }

        private void LoseGame()
        {
            hasGameFinished = true;
            _titleText.text = "TIME UP!";
            _clickHighlight.gameObject.SetActive(false);
        }
        #endregion

        private void Update()
        {
            if (hasGameFinished) return;

            timeLeft -= Time.deltaTime;
            UpdateTimerUI();

            if (timeLeft <= 0)
            {
                LoseGame();
                return;
            }

            HandleInput();
        }

        #region BOARD
        private void SpawnBoard(int size)
        {
            var board = Instantiate(
                _boardPrefab,
                new Vector3(size / 2f, size / 2f, 0f),
                Quaternion.identity);

            board.size = new Vector2(size + 0.1f, size + 0.1f);

            _bgGrid = new Dictionary<Vector2Int, SpriteRenderer>();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    SpriteRenderer bg = Instantiate(
                        _bgCellPrefab,
                        new Vector3(i + 0.5f, j + 0.5f, 0f),
                        Quaternion.identity);

                    _bgGrid.Add(new Vector2Int(i, j), bg);
                }
            }

            Camera.main.orthographicSize = size + 2f;
            Camera.main.transform.position =
                new Vector3(size / 2f, size / 2f, -10f);

            _clickHighlight.gameObject.SetActive(false);
        }

        private void SpawnNodes()
        {
            _nodes = new List<Node>();
            _nodeGrid = new Dictionary<Vector2Int, Node>();

            int size = CurrentLevelData.boardSize;

            HashSet<Vector2Int> blocked =
                CurrentLevelData.blockedCells != null
                ? new HashSet<Vector2Int>(CurrentLevelData.blockedCells)
                : new HashSet<Vector2Int>();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Vector2Int pos2D = new Vector2Int(i, j);
                    Vector3 pos = new Vector3(i + 0.5f, j + 0.5f, 0f);

                    Node node = Instantiate(_nodePrefab, pos, Quaternion.identity);
                    node.Init();
                    node.Pos2D = pos2D;

                    if (blocked.Contains(pos2D))
                    {
                        node.gameObject.SetActive(false);
  
                        if (_bgGrid != null && _bgGrid.TryGetValue(pos2D, out var bg))
                        {
                            bg.color = new Color(0.15f, 0.15f, 0.15f);
                        }

                        continue;
                    }

                    int colorId = GetColorId(i, j);
                    if (colorId != -1)
                        node.SetColorForPoint(colorId);

                    _nodes.Add(node);
                    _nodeGrid.Add(pos2D, node);
                }
            }

            Vector2Int[] dirs =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (var kv in _nodeGrid)
            {
                foreach (var d in dirs)
                {
                    Vector2Int target = kv.Key + d;

                    // 🔥 obstacle otomatis ter-skip karena tidak ada di grid
                    if (_nodeGrid.ContainsKey(target))
                        kv.Value.SetEdge(d, _nodeGrid[target]);
                }
            }
        }

        private int GetColorId(int x, int y)
        {
            Vector2Int p = new Vector2Int(x, y);

            for (int i = 0; i < CurrentLevelData.edges.Count; i++)
            {
                if (CurrentLevelData.edges[i].StartPoint == p ||
                    CurrentLevelData.edges[i].EndPoint == p)
                    return i;
            }
            return -1;
        }
        #endregion

        #region INPUT
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
                startNode = null;

            if (Input.GetMouseButton(0))
            {
                Vector3 mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mPos, Vector2.zero);

                if (startNode == null)
                {
                    if (hit && hit.collider.TryGetComponent(out Node n) && n.IsClickable)
                    {
                        startNode = n;
                        _clickHighlight.gameObject.SetActive(true);
                        _clickHighlight.color = GetHighLightColor(n.colorId);
                    }
                    return;
                }

                _clickHighlight.transform.position = mPos;

                if (hit && hit.collider.TryGetComponent(out Node t) && t != startNode)
                {
                    startNode.UpdateInput(t);
                    CheckWin();
                    startNode = null;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                startNode = null;
                _clickHighlight.gameObject.SetActive(false);
            }
        }
        #endregion

        #region COLOR LOCK
        public bool IsColorActive(int colorId)
        {
            return activeColors.ContainsKey(colorId) && activeColors[colorId];
        }

        public void SetColorActive(int colorId, bool active)
        {
            activeColors[colorId] = active;
        }
        #endregion

        public Color GetHighLightColor(int id)
        {
            Color c = NodeColors[id % NodeColors.Count];
            c.a = 0.4f;
            return c;
        }

        public void ShowHint()
        {
            if (hasGameFinished) return;

            Node hint = HintSolver.GetHintNode(_nodeGrid, _nodes);
            if (hint == null) return;

            _clickHighlight.gameObject.SetActive(true);
            _clickHighlight.transform.position = hint.transform.position;
            _clickHighlight.color = GetHighLightColor(hint.colorId);

            CancelInvoke(nameof(HideHint));
            Invoke(nameof(HideHint), 1.2f);
        }
        
        private void HideHint()
        {
            _clickHighlight.gameObject.SetActive(false);
        }

        private void CheckWin()
        {
            foreach (var n in _nodes)
                n.SolveHighlight();

            foreach (var n in _nodes)
                if (!n.IsWin) return;

            hasGameFinished = true;
            _winText.SetActive(true);
        }


    }
}
