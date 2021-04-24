using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

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

        [Serializable]
        public class SpawnConfig
        {
            public float weight;
            public int id;
        }

        public SpawnConfig[] spawnConfigs;
        private float _totalSpawnWeight;

        /*
         * 0 = Hole
         * 1 = Dirt
         * 2 = Gold
         * 3 = Sky / Grass
         * 4 = Diamond
         * 5 = Hurt
         * 6 = PickAxe
         */

        private int[] items = {2, 4, 5, 6};

        private void Start()
        {
            _offset = initialOffset;
            _yPosition = initialPosition.y;

            _totalSpawnWeight = spawnConfigs.Sum(s => s.weight);

            _grid = new int[gridSize.y][];
            for (var i = 0; i < gridSize.y; i++)
            {
                _grid[i] = new int[gridSize.x];

                if (i > 0)
                {
                    GenerateRow(i);
                }
                else
                {
                    for (var j = 0; j < gridSize.x; j++)
                    {
                        _grid[i][j] = 3;
                    }
                }
            }

            gridMesh.InitDecor(gridSize, decoWidth);

            gridMesh.UpdateMesh(_grid, _offset);

            gridMesh.transform.position = new Vector3(initialPosition.x, initialPosition.y, 0);
        }

        private void ResetGame()
        {
            _gridTween?.Kill();

            _offset = initialOffset;
            _yPosition = initialPosition.y;

            for (var j = 0; j < gridSize.x; j++)
            {
                _grid[0][j] = 3;
            }

            for (var i = 1; i < gridSize.y; i++)
            {
                GenerateRow(i);
            }

            gridMesh.UpdateMesh(_grid, _offset);

            gridMesh.transform.position = new Vector3(initialPosition.x, initialPosition.y, 0);
        }

        private void GenerateRow(int i)
        {
            var item = Random.Range(0, 2) * 2;

            for (var j = 0; j < gridSize.x; j++)
            {
                if (j == item)
                {
                    _grid[i][j] = GetRandomSpawnConfig();
                }
                else
                {
                    _grid[i][j] = 1;
                }
            }
        }

        private int GetRandomSpawnConfig()
        {
            var result = Random.Range(0f, _totalSpawnWeight);
            var sum = 0f;
            foreach (var s in spawnConfigs)
            {
                sum += s.weight;
                if (result <= sum)
                {
                    return s.id;
                }
            }

            return 1;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ResetGame();
            }

            var input = GetInput();
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
                    _gridTween = gridMesh.transform.DOMoveY(maxYPosition, 0.1f);
                }
                else
                {
                    _gridTween = gridMesh.transform.DOMoveY(_yPosition, 0.1f);
                }

                gridMesh.UpdateMesh(_grid, _offset);
            }
        }

        private int GetInput()
        {
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? -1
                : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 1
                : 0;
        }
    }
}