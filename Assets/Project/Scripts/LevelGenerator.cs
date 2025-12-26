using Connect.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Connect.Core
{
    public static class LevelGenerator
    {

        /// <summary>
        /// generate Level and stage
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static RuntimeLevelData Generate(int stage, int level)
        {
            RuntimeLevelData data = new RuntimeLevelData();

            int tier = (level - 1) / 10;

            data.boardSize = Mathf.Clamp(5 + stage + tier / 2, 5, 8);
            data.pairCount = Mathf.Clamp(3 + tier / 3, 3, 6);
            data.timeLimit = Mathf.Clamp(65f - tier * 3f, 30f, 65f);

            var result = GenerateSolvableEdgesWithGrid(
                data.boardSize,
                data.pairCount
            );

            data.edges = result.edges;

            //data.blockedCells = GenerateObstacles(
            //    result.solutionGrid,
            //    data.boardSize,
            //    tier,
            //    data.edges
            //);

            return data;
        }

        private static (List<Edge> edges, int[,] solutionGrid)
            GenerateSolvableEdgesWithGrid(int size, int pairCount)
        {
            const int MAX_ATTEMPT = 50;

            for (int attempt = 0; attempt < MAX_ATTEMPT; attempt++)
            {
                int[,] grid = new int[size, size];
                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        grid[x, y] = -1;

                List<List<Vector2Int>> solutions = new();
                bool failed = false;

                for (int color = 0; color < pairCount; color++)
                {
                    var path = GenerateRandomPath(grid, size, color);
                    if (path == null)
                    {
                        failed = true;
                        break;
                    }

                    solutions.Add(path);
                    foreach (var p in path)
                        grid[p.x, p.y] = color;
                }

                if (failed) continue;
                if (!IsGridFull(grid, size)) continue;

                List<Edge> edges = new();
                foreach (var path in solutions)
                {
                    edges.Add(new Edge
                    {
                        Points = new List<Vector2Int>
                        {
                            path[0],
                            path[^1]
                        }
                    });
                }

                return (edges, grid);
            }

            return GenerateSimpleFallback(size, pairCount);
        }

        private static (List<Edge>, int[,])
            GenerateSimpleFallback(int size, int pairCount)
        {
            int[,] grid = new int[size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    grid[x, y] = -1;

            List<Edge> edges = new();
            int yRow = 0;

            for (int c = 0; c < pairCount; c++)
            {
                Vector2Int a = new(0, yRow);
                Vector2Int b = new(size - 1, yRow);

                grid[a.x, a.y] = c;
                grid[b.x, b.y] = c;

                edges.Add(new Edge
                {
                    Points = new List<Vector2Int> { a, b }
                });

                yRow = (yRow + 2) % size;
            }

            return (edges, grid);
        }

        //private static List<Vector2Int> GenerateObstacles(
        //    int[,] solutionGrid,
        //    int size,
        //    int tier,
        //    List<Edge> edges
        //)
        //{
        //    List<Vector2Int> obstacles = new();
        //    List<Vector2Int> candidates = new();

        //    HashSet<Vector2Int> pathCells = new();
        //    for (int x = 0; x < size; x++)
        //        for (int y = 0; y < size; y++)
        //            if (solutionGrid[x, y] != -1)
        //                pathCells.Add(new Vector2Int(x, y));

        //    HashSet<Vector2Int> endpoints = new();
        //    foreach (var e in edges)
        //    {
        //        endpoints.Add(e.StartPoint);
        //        endpoints.Add(e.EndPoint);
        //    }

        //    for (int x = 1; x < size - 1; x++)
        //    {
        //        for (int y = 1; y < size - 1; y++)
        //        {
        //            Vector2Int p = new(x, y);

        //            if (pathCells.Contains(p))
        //                continue;

        //            bool nearEndpoint = false;
        //            foreach (var ep in endpoints)
        //            {
        //                if (Vector2Int.Distance(p, ep) <= 1)
        //                {
        //                    nearEndpoint = true;
        //                    break;
        //                }
        //            }

        //            if (!nearEndpoint)
        //                candidates.Add(p);
        //        }
        //    }

        //    int obstacleCount = Mathf.Clamp(
        //        tier,
        //        0,
        //        candidates.Count / 2
        //    );

        //    Shuffle(candidates);

        //    foreach (var c in candidates)
        //    {
        //        if (obstacles.Count >= obstacleCount)
        //            break;

        //        if (NearPath(c, pathCells))
        //            continue;

        //        if (!IsSafeObstaclePlacement(c, obstacles, size))
        //            continue;

        //        obstacles.Add(c);
        //    }

        //    return obstacles;
        //}

        private static bool NearPath(
            Vector2Int cell,
            HashSet<Vector2Int> pathCells
        )
        {
            Vector2Int[] dirs =
            {
                Vector2Int.zero,
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (var d in dirs)
                if (pathCells.Contains(cell + d))
                    return true;

            return false;
        }

        private static List<Vector2Int> GenerateRandomPath(
            int[,] grid,
            int size,
            int colorId
        )
        {
            List<Vector2Int> path = new();

            Vector2Int start = GetRandomEmpty(grid, size);
            if (start.x == -1) return null;

            path.Add(start);
            grid[start.x, start.y] = colorId;

            int filled = CountFilled(grid, size);
            float fillRatio = (float)filled / (size * size);

            int minLength = Mathf.RoundToInt(size * (1.0f + fillRatio));
            int maxLength = Mathf.RoundToInt(size * (2.0f + fillRatio));
            int targetLength = Random.Range(minLength, maxLength);

            Vector2Int cur = start;
            Vector2Int lastDir = Vector2Int.zero;

            Vector2Int[] dirs =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            for (int i = 0; i < targetLength; i++)
            {
                List<Vector2Int> options = new();

                foreach (var d in dirs)
                {
                    Vector2Int n = cur + d;
                    if (n.x < 0 || n.y < 0 || n.x >= size || n.y >= size)
                        continue;
                    if (grid[n.x, n.y] != -1)
                        continue;

                    options.Add(n);
                }

                if (options.Count == 0)
                    break;

                cur = options[Random.Range(0, options.Count)];
                grid[cur.x, cur.y] = colorId;
                path.Add(cur);
            }

            return path.Count >= 3 ? path : null;
        }

        private static bool IsSafeObstaclePlacement(
            Vector2Int cell,
            List<Vector2Int> obstacles,
            int size
        )
        {
            int row = 0, col = 0;
            foreach (var o in obstacles)
            {
                if (o.y == cell.y) row++;
                if (o.x == cell.x) col++;
            }

            return row < size / 2 && col < size / 2;
        }

        private static Vector2Int GetRandomEmpty(int[,] grid, int size)
        {
            List<Vector2Int> empty = new();
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (grid[x, y] == -1)
                        empty.Add(new Vector2Int(x, y));

            return empty.Count == 0
                ? new Vector2Int(-1, -1)
                : empty[Random.Range(0, empty.Count)];
        }

        private static int CountFilled(int[,] grid, int size)
        {
            int c = 0;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (grid[x, y] != -1)
                        c++;
            return c;
        }

        private static bool IsGridFull(int[,] grid, int size)
        {
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (grid[x, y] == -1)
                        return false;
            return true;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int r = Random.Range(i, list.Count);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }
    }
}
