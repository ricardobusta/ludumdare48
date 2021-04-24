using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Busta.Diggy
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridMesh : MonoBehaviour
    {
        public Vector2Int gridSize;

        public Mesh frontFaceTemplate;

        private List<Vector3> _verticeBuffer;
        private List<Vector2> _uvBuffer;
        private List<int> _triangleBuffer;
        private List<Vector3> _normalBuffer;

        private void Start()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var mesh = meshFilter.mesh = new Mesh();

            _verticeBuffer = new List<Vector3>();
            _triangleBuffer = new List<int>();
            _uvBuffer = new List<Vector2>();
            _normalBuffer = new List<Vector3>();

            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var startFaceIndex = _verticeBuffer.Count;
                    var frontFaceMesh = frontFaceTemplate;

                    _verticeBuffer.AddRange(frontFaceMesh.vertices.Select(v => v + new Vector3(x, y, 0)));
                    _uvBuffer.AddRange(frontFaceMesh.uv);
                    _normalBuffer.AddRange(frontFaceMesh.normals);

                    _triangleBuffer.AddRange(frontFaceMesh.triangles.Select(t => t + startFaceIndex));
                }
            }

            mesh.vertices = _verticeBuffer.ToArray();
            mesh.triangles = _triangleBuffer.ToArray();
            mesh.uv = _uvBuffer.ToArray();
            mesh.normals = _normalBuffer.ToArray();
        }
    }
}