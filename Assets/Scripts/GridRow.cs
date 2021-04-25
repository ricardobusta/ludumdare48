using UnityEngine;

namespace Busta.Diggy
{
    public class GridRow : MonoBehaviour
    {
        public MeshRenderer leftEdge;
        public MeshRenderer rightEdge;

        public MeshRenderer leftBlock;
        public MeshRenderer centerBlock;
        public MeshRenderer rightBlock;

        public Material dirtMaterial;
        public Material hazardMaterial;

        public bool leftHazard;

        public void InitRow()
        {
            leftBlock.material = hazardMaterial;
            rightBlock.material = dirtMaterial;

            leftBlock.gameObject.SetActive(true);
            centerBlock.gameObject.SetActive(true);
            rightBlock.gameObject.SetActive(true);
        }

        public void BreakRow()
        {
            (leftHazard ? rightBlock : leftBlock).gameObject.SetActive(false);
            centerBlock.gameObject.SetActive(false);
        }
    }
}