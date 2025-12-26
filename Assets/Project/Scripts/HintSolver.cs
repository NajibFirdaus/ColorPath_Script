using System.Collections.Generic;
using UnityEngine;

namespace Connect.Core
{
    public static class HintSolver
    {
        public static Node GetHintNode(
            Dictionary<Vector2Int, Node> grid,
            List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (!node.IsEndNode || node.IsWin)
                    continue;

                foreach (var kv in grid)
                {
                    Node n = kv.Value;

                    if (IsValidHint(node, n))
                        return n;
                }
            }

            return null;
        }

        private static bool IsValidHint(Node start, Node target)
        {
            if (start == target) return false;

            if (Vector2Int.Distance(start.Pos2D, target.Pos2D) != 1)
                return false;

            if (target.ConnectedNodes.Count > 0 &&
                target.colorId != start.colorId)
                return false;

            return true;
        }
    }
}
