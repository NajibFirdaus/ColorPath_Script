using Connect.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Connect.Core
{
    public class RuntimeLevelData
    {
        public int boardSize;
        public int pairCount;
        public float timeLimit;
        public List<Edge> edges;
        public List<Vector2Int> blockedCells;
    }
}
