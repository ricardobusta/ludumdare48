using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

        [Serializable]
        public class PlayerScore
        {
            public List<int> scores;

            public void SortScore()
            {
                scores.Sort((i, i1) => i1 - i);
            }
        }

        [Header("Config")]
        public Vector2Int gridSize = new Vector2Int(3, 20);

        public Vector2Int initialPosition = new Vector2Int(-1, 0);
        public int decoWidth = 5;
        public int initialOffset;
        public int maxYPosition = 10;

        public SpawnConfig[] spawnConfigs;

        public float hpTakenEachSecond;
        public float hurtDamage;
        public float pickAxeHeal;
        public int goldScore;
        public int diamondScore;

        [Header("Refs")]
        public GameObject player;

        public Animator playerAnimator;
        public GridMesh gridMesh;

        public GameObject[] surfaceObjects;
        public AudioSystem audioSystem;

        public TMP_Text scoreLabel;
        public Slider pickAxeHealth;

        public TMP_Text rewardTextPrefab;

        public Canvas gameHudCanvas;
        public Canvas scoreCanvas;

        public Button startGameButton;

        public TMP_Text highScoreLabel;

        private int _score;
        private float _health;

        private bool _surfaceObjectsDisabled;

        private int _yPosition;
        private int _offset;

        private int[][] _grid;

        private Tween _gridTween;

        private float _totalSpawnWeight;

        private TMP_Text[] rewardTextList;

        private EventSystem _eventSystem;

        private bool _gameStarted;

        private static readonly Quaternion RotationLeft = Quaternion.Euler(0, 45, 0);
        private static readonly Quaternion RotationRight = Quaternion.Euler(0, -45, 0);
        private static readonly Vector3 ScaleLeft = Vector3.one;
        private static readonly Vector3 ScaleRight = new Vector3(-1, 1, 1);

        private static readonly Color HurtColor = Color.red;
        private static readonly Color GoldColor = Color.yellow;
        private static readonly Color HealColor = Color.green;

        private static readonly int AttackTrigger = Animator.StringToHash("attack");
        private static readonly int HurtTrigger = Animator.StringToHash("hurt");
        private static readonly int DefeatTrigger = Animator.StringToHash("defeat");
        private static readonly int ResetTrigger = Animator.StringToHash("reset");

        private const string SCORE_FORMAT = "0000000000";

        private StringBuilder _stringBuilder = new StringBuilder();

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
            _eventSystem = FindObjectOfType<EventSystem>();

            _offset = initialOffset;
            _yPosition = initialPosition.y;

            _totalSpawnWeight = spawnConfigs.Sum(s => s.weight);

            _grid = new int[gridSize.y][];
            for (var i = 0; i < gridSize.y; i++)
            {
                _grid[i] = new int[gridSize.x];
            }

            gridMesh.InitDecor(gridSize, decoWidth);

            rewardTextList = new TMP_Text[10];
            for (var i = 0; i < rewardTextList.Length; i++)
            {
                var newText = rewardTextList[i] = Instantiate(rewardTextPrefab);
                newText.gameObject.SetActive(false);
            }

            scoreCanvas.gameObject.SetActive(true);
            gameHudCanvas.gameObject.SetActive(false);

            startGameButton.onClick.AddListener(StartGame);

            ComputeScore(-1);
            
            audioSystem.PlayIdleMusic();
            
            ResetGame();
        }

        private void ResetGame()
        {
            _gridTween?.Kill();

            playerAnimator.SetTrigger(ResetTrigger);

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
            
            UpdateHud();
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
            if (_gameStarted)
            {
                GameUpdate();
            }
        }

        private void StartGame()
        {
            ResetGame();
            gameHudCanvas.gameObject.SetActive(true);
            scoreCanvas.gameObject.SetActive(false);

            var sequence = DOTween.Sequence();
            sequence.AppendCallback(() => ShowRewardText("3", Color.white));
            sequence.AppendInterval(1);
            sequence.AppendCallback(() => ShowRewardText("2", Color.white));
            sequence.AppendInterval(1);
            sequence.AppendCallback(() => ShowRewardText("1", Color.white));
            sequence.AppendInterval(1);
            sequence.AppendCallback(() => ShowRewardText("Go!", Color.white));
            sequence.AppendCallback(() =>
            {
                audioSystem.PlayActionMusic();
                _gameStarted = true;
            });
        }

        private void GameUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ResetGame();
            }

            _health -= Time.deltaTime * hpTakenEachSecond;

            if (_health <= 0)
            {
                _gameStarted = false;
                playerAnimator.SetTrigger(DefeatTrigger);
                ShowRewardText("Time Over!", Color.white);
                audioSystem.PlayIdleMusic();
                ComputeScore(_score);
                
                DOVirtual.DelayedCall(1.0f, () =>
                {
                    scoreCanvas.gameObject.SetActive(true);
                    gameHudCanvas.gameObject.SetActive(false);
                });
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

            UpdateHud();
        }

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
                    playerAnimator.SetTrigger(AttackTrigger);
                    break;
                case 2: // Gold
                    audioSystem.PlayBreakSfx();
                    playerAnimator.SetTrigger(AttackTrigger);
                    _score += goldScore;
                    ShowRewardText(goldScore.ToString(), GoldColor);
                    break;
                case 4: // Diamond
                    audioSystem.PlayBreakSfx();
                    playerAnimator.SetTrigger(AttackTrigger);
                    _score += diamondScore;
                    ShowRewardText(diamondScore.ToString(), GoldColor);
                    break;
                case 5: // Hurt
                    audioSystem.PlayHurtSfx();
                    playerAnimator.SetTrigger(HurtTrigger);
                    _health -= hurtDamage;
                    ShowRewardText($"-{(int) (hurtDamage / hpTakenEachSecond)} sec", HurtColor);
                    break;
                case 6: // PickAxe
                    audioSystem.PlayPowerUpSfx();
                    playerAnimator.SetTrigger(AttackTrigger);
                    _health += pickAxeHeal;
                    ShowRewardText($"+{(int) (pickAxeHeal / hpTakenEachSecond)} sec", HealColor);
                    break;
                default:
                    break;
            }

            row[index] = 0;
            row[1] = 0;
        }

        private void UpdateHud()
        {
            pickAxeHealth.value = _health;
            scoreLabel.text = _score.ToString(SCORE_FORMAT);
        }

        private int GetInput()
        {
            var mouseInput = !_eventSystem.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)
                ? (Input.mousePosition.x < Screen.width / 2f ? -1 : 1)
                : 0;

            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || mouseInput == -1 ? 0 :
                Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || mouseInput == 1 ? 2 : 1;
        }

        private const string PLAYER_SCORE_KEY = "PLAYER_SCORE_KEY";
        private const int PLAYER_SCORE_AMOUNT = 10;

        private void ComputeScore(int newScore)
        {
            var playerScores = PlayerPrefs.HasKey(PLAYER_SCORE_KEY)
                ? JsonUtility.FromJson<PlayerScore>(PlayerPrefs.GetString(PLAYER_SCORE_KEY))
                : new PlayerScore {scores = Enumerable.Repeat(0, PLAYER_SCORE_AMOUNT).ToList()};

            var min = playerScores.scores.Min();
            if (newScore > min)
            {
                playerScores.scores.Remove(min);
                playerScores.scores.Add(newScore);
                playerScores.SortScore();
                Debug.Log("Writing "+ JsonUtility.ToJson(playerScores));
                PlayerPrefs.SetString(PLAYER_SCORE_KEY, JsonUtility.ToJson(playerScores));
            }

            var index = playerScores.scores.IndexOf(newScore);

            _stringBuilder.Clear();
            for(var i=0;i<playerScores.scores.Count;i++)
            {
                var scoreString = playerScores.scores[i].ToString(SCORE_FORMAT);
                _stringBuilder.AppendLine(i==index?$"<color=yellow>{scoreString}</color>":scoreString);
            }

            highScoreLabel.text = _stringBuilder.ToString();
        }

        private void ShowRewardText(string value, Color color)
        {
            for (var i = 0; i < 10; i++)
            {
                var text = rewardTextList[i];
                if (!text.gameObject.activeSelf)
                {
                    text.gameObject.SetActive(true);
                    text.transform.position = new Vector3(0, 0, -1.5f);
                    text.transform.DOMoveY(3, 0.5f).OnComplete(() => text.gameObject.SetActive(false));
                    text.text = value;
                    text.color = color;
                    return;
                }
            }
        }
    }
}