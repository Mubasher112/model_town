using System.Collections.Generic;
using UnityEngine;

namespace Game.Services
{
    public class ExplorationService
    {
        private readonly HashSet<Vector2Int> _discoveredCells = new HashSet<Vector2Int>();

        public ExplorationService(IEnumerable<Vector2Int> initialDiscoveredCells = null)
        {
            if (initialDiscoveredCells != null)
            {
                foreach (var cell in initialDiscoveredCells)
                {
                    _discoveredCells.Add(cell);
                }
            }
        }

        public bool IsDiscovered(Vector2Int cell)
        {
            return _discoveredCells.Contains(cell);
        }

        public bool DiscoverCell(Vector2Int cell)
        {
            return _discoveredCells.Add(cell);
        }

        public int DiscoverRadius(Vector2Int center, int radius)
        {
            int newlyDiscovered = 0;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int cell = new Vector2Int(center.x + dx, center.y + dy);
                    if (_discoveredCells.Add(cell))
                    {
                        newlyDiscovered++;
                    }
                }
            }
            return newlyDiscovered;
        }

        public float GetDiscoveredPercentage(int gridWidth, int gridHeight)
        {
            if (gridWidth <= 0 || gridHeight <= 0) return 0f;
            int totalCells = gridWidth * gridHeight;
            return Mathf.Clamp01((float)_discoveredCells.Count / totalCells);
        }

        public List<Vector2Int> ExportDiscoveredCells()
        {
            return new List<Vector2Int>(_discoveredCells);
        }
    }
}
