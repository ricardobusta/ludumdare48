using UnityEngine;

namespace Busta.Diggy
{
    public class GameController : MonoBehaviour
    {
        public GameObject rowPrefab;
        public GameObject dirtPrefab;
        public GameObject hazardPrefab;

        public GameObject gridContainer;

        public Vector2Int boardSize;

        private GameObject[] rows;

        private void Start()
        {
            rows = new GameObject[boardSize.y];

            for (var j = 0; j < boardSize.y; j++)
            {
                var row = rows[j] = Instantiate(rowPrefab, gridContainer.transform);

                var dirtPos = Random.Range(0, 2) % 2 == 0 ? 0 : 2;

                for (var i = 0; i < boardSize.x; i++)
                {
                    Instantiate(dirtPos == i ? hazardPrefab : dirtPrefab, new Vector3(i - 1, -j, 0),
                        Quaternion.identity,
                        row.transform);
                }
            }
        }

        void Update()
        {
        }
    }
}