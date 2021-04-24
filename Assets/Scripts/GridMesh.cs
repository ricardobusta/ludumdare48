using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Busta.Diggy
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridMesh : MonoBehaviour
    {
        public Vector2[] uvOffsets;
        public int textureSize;

        public Mesh frontFaceTemplate;
        public Mesh backFaceTemplate;

        public Mesh topFaceTemplate;
        public Mesh bottomFaceTemplate;
        public Mesh leftFaceTemplate;
        public Mesh rightFaceTemplate;

        public Mesh topLeftCornerTemplate;
        public Mesh topRightCornerTemplate;
        public Mesh bottomLeftCornerTemplate;
        public Mesh bottomRightCornerTemplate;

        public MeshFilter decorationLeft;
        public MeshFilter decorationRight;

        private List<Vector3> _vertexBuffer;
        private List<Vector2> _uvBuffer;
        private List<int> _triangleBuffer;
        private List<Vector3> _normalBuffer;

        private Mesh _mesh;
        private Vector2[] _uvOffsets;

        private void Start()
        {
            _uvOffsets = uvOffsets.Select(uv => uv * (1f / textureSize)).ToArray();

            var meshFilter = GetComponent<MeshFilter>();
            _mesh = meshFilter.mesh = new Mesh();

            _vertexBuffer = new List<Vector3>();
            _triangleBuffer = new List<int>();
            _uvBuffer = new List<Vector2>();
            _normalBuffer = new List<Vector3>();
        }

        public void InitDecor(Vector2Int size, int decoWidth)
        {
            var row = Enumerable.Repeat(1, decoWidth).ToArray();
            var grid = Enumerable.Repeat(row, size.y).ToArray();
            var decorationMesh = new Mesh();
            UpdateMesh(grid, 0, decorationMesh);
            decorationLeft.mesh = decorationMesh;
            decorationLeft.transform.localPosition = new Vector3(-decoWidth, 0, 0);
            decorationRight.mesh = decorationMesh;
            decorationRight.transform.localPosition = new Vector3(size.x, 0, 0);
        }

        public void UpdateMesh(int[][] grid, int offset, Mesh updateMesh = null)
        {
            ClearBuffers();
            for (var i = 0; i < grid.Length; i++)
            {
                var row = grid[(i + offset) % grid.Length];
                for (var j = 0; j < row.Length; j++)
                {
                    var pos = new Vector3(j, -i, 0);
                    var element = row[j];

                    if (element == 0) // hole
                    {
                        AddMeshTemplate(backFaceTemplate, pos, _uvOffsets[element]);

                        var left = j <= 0 || row[j - 1] > 0;
                        if (left)
                        {
                            AddMeshTemplate(rightFaceTemplate, pos + Vector3.left, _uvOffsets[element]);
                        }

                        var right = j >= row.Length - 1 || row[j + 1] > 0;
                        if (right)
                        {
                            AddMeshTemplate(leftFaceTemplate, pos + Vector3.right, _uvOffsets[element]);
                        }

                        var previousRow = grid[(i + offset + grid.Length - 1) % grid.Length];

                        var top = previousRow[j] > 0;

                        if (top)
                        {
                            AddMeshTemplate(bottomFaceTemplate, pos + Vector3.up, _uvOffsets[element]);
                        }

                        var nextRow = grid[(i + offset + 1) % grid.Length];
                        var bottom = nextRow[j] > 0;

                        if (bottom)
                        {
                            AddMeshTemplate(topFaceTemplate, pos + Vector3.down, _uvOffsets[element]);
                        }
                    }
                    else // wall
                    {
                        AddMeshTemplate(frontFaceTemplate, pos, _uvOffsets[element]);
                    }
                }
            }

            UpdateMesh(updateMesh == null ? _mesh : updateMesh);
        }

        private void UpdateMesh(Mesh mesh)
        {
            mesh.vertices = _vertexBuffer.ToArray();
            mesh.uv = _uvBuffer.ToArray();
            mesh.normals = _normalBuffer.ToArray();
            mesh.triangles = _triangleBuffer.ToArray();
            mesh.UploadMeshData(false);
            Debug.Log($"{mesh.vertexCount}{mesh.uv.Length}{mesh.normals.Length}{mesh.triangles.Length}");
        }

        private void ClearBuffers()
        {
            _vertexBuffer.Clear();
            _triangleBuffer.Clear();
            _uvBuffer.Clear();
            _normalBuffer.Clear();
            _mesh.Clear();
        }

        private void AddMeshTemplate(Mesh meshTemplate, Vector3 positionOffset)
        {
            AddMeshTemplate(meshTemplate, positionOffset, Vector2.zero);
        }

        private void AddMeshTemplate(Mesh meshTemplate, Vector3 positionOffset, Vector2 uvOffset)
        {
            var startFaceIndex = _vertexBuffer.Count;

            _vertexBuffer.AddRange(positionOffset != Vector3.zero
                ? meshTemplate.vertices.Select(v => v + positionOffset)
                : meshTemplate.vertices);

            _uvBuffer.AddRange(uvOffset != Vector2.zero
                ? meshTemplate.uv.Select(uv => uv + uvOffset)
                : meshTemplate.uv);

            _normalBuffer.AddRange(meshTemplate.normals);

            _triangleBuffer.AddRange(meshTemplate.triangles.Select(t => t + startFaceIndex));
        }
    }
}