using System;
using Unity.Netcode;
using UnityEngine;

namespace Building
{
    public class PlotGrid : MonoBehaviour
    {
        public float cellSize;
        public float numColumns;
        public float numRows;
        private Bounds _bounds;

        private void Start()
        {
            _bounds = new Bounds(
                transform.position, 
                new Vector3(cellSize * numColumns, 0, cellSize * numRows)
                );
        }

        
        
        public bool InBounds(Vector2 position) => _bounds.Contains(position);
    }
}
