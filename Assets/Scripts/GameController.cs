using DG.Tweening;
using UnityEngine;

namespace Busta.Diggy
{
    public class GameController : MonoBehaviour
    {
        public Vector2Int gridSize;
        public int decoWidth;

        public GridMesh gridMesh;

        public int initialOffset;
        public Vector2Int initialPosition;
        public int maxYPosition;

        private int _yPosition;
        private int _offset;

        private int[][] _grid;

        private Tween _gridTween;

        private void Start()
        {
            _offset = initialOffset;
            _yPosition = initialPosition.y;

            _grid = new int[gridSize.y][];
            for (var i = 0; i < gridSize.y; i++)
            {
                _grid[i] = new int[gridSize.x];

                GenerateRow(i);
            }

            gridMesh.InitDecor(gridSize, decoWidth);

            gridMesh.UpdateMesh(_grid, _offset);

            gridMesh.transform.position = new Vector3(initialPosition.x, initialPosition.y, 0);
        }

        private void GenerateRow(int i)
        {
            var hazardJ = Random.Range(0, 2) * 2;

            for (var j = 0; j < gridSize.x; j++)
            {
                if (j == hazardJ)
                {
                    _grid[i][j] = 2;
                }
                else
                {
                    _grid[i][j] = 1;
                }
            }
        }

        private void Update()
        {
            var input = Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0;
            if (input != 0)
            {
                _gridTween?.Kill();

                var row = (_yPosition + 1) % _grid.Length;
                _grid[row][0] = input == -1 ? 0 : _grid[row][0];
                _grid[row][1] = 0;
                _grid[row][2] = input == 1 ? 0 : _grid[row][2];

                _yPosition++;

                var pos = gridMesh.transform.position;

                if (_yPosition > maxYPosition)
                {
                    row = _offset % _grid.Length;
                    GenerateRow(row);
                    _offset++;
                    
                    gridMesh.transform.position = pos + Vector3.down;
                    _gridTween = gridMesh.transform.DOMoveY(maxYPosition, 0.3f);
                }
                else
                {
                    _gridTween = gridMesh.transform.DOMoveY(_yPosition, 0.3f);
                }
                
                gridMesh.UpdateMesh(_grid, _offset);
            }
        }
    }
}