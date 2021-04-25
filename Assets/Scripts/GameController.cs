using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Busta.Diggy
{
    public class GameController : MonoBehaviour
    {
        [Serializable]
        public class SpawnConfig
        {
            public float weight;
            public int id;
        }

        [Header("Config")]
        public Vector2Int gridSize = new Vector2Int(3, 20);

        public Vector2Int initialPosition = new Vector2Int(-1, 0);
        public int decoWidth = 5;
        public int initialOffset;
        public int maxYPosition = 10;

        public SpawnConfig[] spawnConfigs;

        [Header("Refs")]
        public GameObject player;
        public Animator playerAnimator;
        public GridMesh gridMesh;

        public GameObject[] surfaceObjects;
        public AudioSystem audioSystem;

        private int _score;
        private float _health;

        private bool _surfaceObjectsDisabled;

        private int _yPosition;
        private int _offset;

        private int[][] _grid;

        private Tween _gridTween;

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

        private void Start()
        {
            audioSystem.PlayActionMusic();
            _offset = initialOffset;
            _yPosition = initialPosition.y;

            _totalSpawnWeight = spawnConfigs.Sum(s => s.weight);

            _grid = new int[gridSize.y][];
            for (var i = 0; i < gridSize.y; i++)
            {
                _grid[i] = new int[gridSize.x];
            }

            gridMesh.InitDecor(gridSize, decoWidth);

            ResetGame();
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
                GenerateRow(_grid[i]);
            }

            gridMesh.UpdateMesh(_grid, _offset);

            gridMesh.transform.position = new Vector3(initialPosition.x, initialPosition.y, 0);

            foreach (var surfaceObject in surfaceObjects)
            {
                surfaceObject.SetActive(true);
            }

            _surfaceObjectsDisabled = false;

            _score = 0;

            _health = 1;
        }

        private void GenerateRow(int[] row)
        {
            var item = Random.Range(0, 2) * 2;

            for (var j = 0; j < gridSize.x; j++)
            {
                if (j == item)
                {
                    row[j] = GetRandomSpawnConfig();
                }
                else
                {
                    row[j] = 1;
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
            if (input != 1)
            {
                _gridTween?.Kill();

                var rowIndex = (_yPosition + 1) % _grid.Length;
                var row = _grid[rowIndex];

                HitRow(row, input);


                _yPosition++;

                var pos = gridMesh.transform.position;

                if (_yPosition > maxYPosition)
                {
                    GenerateRow(_grid[_offset % _grid.Length]);
                    _offset++;

                    gridMesh.transform.position = pos + Vector3.down;
                    _gridTween = gridMesh.transform.DOMoveY(maxYPosition, 0.1f);

                    if (!_surfaceObjectsDisabled)
                    {
                        foreach (var obj in surfaceObjects)
                        {
                            obj.SetActive(false);
                        }

                        _surfaceObjectsDisabled = true;
                    }
                }
                else
                {
                    _gridTween = gridMesh.transform.DOMoveY(_yPosition, 0.1f);
                }

                gridMesh.UpdateMesh(_grid, _offset);
            }
        }
        
        private static readonly Quaternion RotationLeft = Quaternion.Euler(0, 45, 0);
        private static readonly Quaternion RotationRight = Quaternion.Euler(0, -45, 0);
        private static readonly Vector3 ScaleLeft = Vector3.one;
        private static readonly Vector3 ScaleRight = new Vector3(-1, 1, 1);
        

        private void HitRow(int[] row, int index)
        {
            if (index == 0)
            {
                player.transform.rotation = RotationLeft;
                player.transform.localScale = ScaleLeft;
            }
            else
            {
                player.transform.rotation = RotationRight;
                player.transform.localScale = ScaleRight;
            }

            switch (row[index])
            {
                case 1: // Dirt
                    audioSystem.PlayDigSfx();
                    playerAnimator.SetTrigger("attack");
                    break;
                case 2: // Gold
                    audioSystem.PlayBreakSfx();
                    playerAnimator.SetTrigger("attack");
                    _score += 1;
                    break;
                case 4: // Diamond
                    audioSystem.PlayBreakSfx();
                    playerAnimator.SetTrigger("attack");
                    _score += 5;
                    break;
                case 5: // Hurt
                    audioSystem.PlayHurtSfx();
                    playerAnimator.SetTrigger("hurt");
                    break;
                case 6: // PickAxe
                    audioSystem.PlayPowerUpSfx();
                    playerAnimator.SetTrigger("attack");
                    break;
                default:
                    break;
            }

            row[index] = 0;
            row[1] = 0;
        }

        private int GetInput()
        {
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? 0 :
                Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 2 : 1;
        }
    }
}