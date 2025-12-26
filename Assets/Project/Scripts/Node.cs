using System.Collections.Generic;
using UnityEngine;

namespace Connect.Core
{
    public class Node : MonoBehaviour
    {
        [SerializeField] private GameObject _point;
        [SerializeField] private GameObject _topEdge;
        [SerializeField] private GameObject _bottomEdge;
        [SerializeField] private GameObject _leftEdge;
        [SerializeField] private GameObject _rightEdge;
        [SerializeField] private GameObject _highLight;

        private Dictionary<Node, GameObject> ConnectedEdges;
        public List<Node> ConnectedNodes = new();

        public int colorId;
        public Vector2Int Pos2D { get; set; }

        public bool IsEndNode => _point.activeSelf;
        public bool IsClickable => IsEndNode || ConnectedNodes.Count > 0;

        public bool IsWin =>
            IsEndNode ? ConnectedNodes.Count == 1 : ConnectedNodes.Count == 2;

        public void Init()
        {
            _point.SetActive(false);
            _topEdge.SetActive(false);
            _bottomEdge.SetActive(false);
            _leftEdge.SetActive(false);
            _rightEdge.SetActive(false);
            _highLight.SetActive(false);

            ConnectedEdges = new Dictionary<Node, GameObject>();
            ConnectedNodes.Clear();
        }

        public void SetColorForPoint(int id)
        {
            colorId = id;
            _point.SetActive(true);
            _point.GetComponent<SpriteRenderer>().color =
                GameplayManager.Instance.NodeColors[id % GameplayManager.Instance.NodeColors.Count];
        }

        public void SetEdge(Vector2Int offset, Node node)
        {
            if (offset == Vector2Int.up) ConnectedEdges[node] = _topEdge;
            else if (offset == Vector2Int.down) ConnectedEdges[node] = _bottomEdge;
            else if (offset == Vector2Int.left) ConnectedEdges[node] = _leftEdge;
            else if (offset == Vector2Int.right) ConnectedEdges[node] = _rightEdge;
        }

        public void UpdateInput(Node target)
        {
            if (!ConnectedEdges.ContainsKey(target)) return;

            // ❌ Cegah double path warna sama
            if (IsEndNode && ConnectedNodes.Count == 0 &&
                GameplayManager.Instance.IsColorActive(colorId))
                return;

            // ❌ Cegah nabrak warna lain
            if (target.ConnectedNodes.Count > 0 &&
                target.colorId != colorId)
                return;

            // Toggle off
            if (ConnectedNodes.Contains(target))
            {
                RemoveConnection(target);
                GameplayManager.Instance.SetColorActive(colorId, false);
                return;
            }

            // Maks 2 koneksi
            if (ConnectedNodes.Count == 2)
                RemoveConnection(ConnectedNodes[0]);

            if (target.ConnectedNodes.Count == 2)
                target.RemoveConnection(target.ConnectedNodes[0]);

            GameplayManager.Instance.SetColorActive(colorId, true);
            AddEdge(target);
        }

        private void AddEdge(Node node)
        {
            node.colorId = colorId;

            ConnectedNodes.Add(node);
            node.ConnectedNodes.Add(this);

            var edge = ConnectedEdges[node];
            edge.SetActive(true);
            edge.GetComponent<SpriteRenderer>().color =
                GameplayManager.Instance.NodeColors[colorId % GameplayManager.Instance.NodeColors.Count];
        }

        private void RemoveConnection(Node node)
        {
            ConnectedNodes.Remove(node);
            node.ConnectedNodes.Remove(this);

            ConnectedEdges[node].SetActive(false);
            node.ConnectedEdges[this].SetActive(false);
        }

        public void SolveHighlight()
        {
            _highLight.SetActive(IsWin);
            if (IsWin)
                _highLight.GetComponent<SpriteRenderer>().color =
                    GameplayManager.Instance.GetHighLightColor(colorId);
        }
    }
}
