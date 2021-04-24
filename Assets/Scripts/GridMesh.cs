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

        public Mesh grassLeft;
        public Mesh grassCenter;
        public Mesh grassRight;

        public MeshFilter leftDeco;
        public MeshFilter rightDeco;

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
            grid[0] = Enumerable.Repeat(3, decoWidth).ToArray();
            var decorationMesh = new Mesh();
            UpdateMesh(grid, 0, decorationMesh);

            var trLeft = leftDeco.transform;
            trLeft.SetParent(transform);
            trLeft.localPosition = new Vector3(-decoWidth, 0, 0);
            leftDeco.mesh = decorationMesh;

            var trRight = rightDeco.transform;
            trRight.SetParent(transform);
            trRight.localPosition = new Vector3(size.x, 0, 0);
            rightDeco.mesh = decorationMesh;
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
                            AddMeshTemplate(rightFaceTemplate, pos, _uvOffsets[element]);
                        }

                        var right = j >= row.Length - 1 || row[j + 1] > 0;
                        if (right)
                        {
                            AddMeshTemplate(leftFaceTemplate, pos, _uvOffsets[element]);
                        }

                        var previousRow = grid[(i + offset + grid.Length - 1) % grid.Length];

                        var top = previousRow[j] != 0 && previousRow[j] != 3;
                        if (top)
                        {
                            AddMeshTemplate(bottomFaceTemplate, pos, _uvOffsets[element]);
                        }

                        var nextRow = grid[(i + offset + 1) % grid.Length];

                        var bottom = nextRow[j] > 0;
                        if (bottom)
                        {
                            AddMeshTemplate(topFaceTemplate, pos, _uvOffsets[element]);
                        }

                        if (!top && !left && (j <= 0 || (previousRow[j - 1] != 0 && previousRow[j - 1] != 3)))
                        {
                            AddMeshTemplate(bottomRightCornerTemplate, pos, _uvOffsets[element]);
                        }

                        if (!top && !right && (j >= row.Length - 1 || (previousRow[j + 1] != 0 && previousRow[j + 1] != 3)))
                        {
                            AddMeshTemplate(bottomLeftCornerTemplate, pos, _uvOffsets[element]);
                        }

                        if (!bottom && !left && (j <= 0 || (nextRow[j - 1] != 0 && nextRow[j - 1] != 3)))
                        {
                            AddMeshTemplate(topRightCornerTemplate, pos, _uvOffsets[element]);
                        }

                        if (!bottom && !right && (j >= row.Length - 1 || (nextRow[j + 1] != 0 && nextRow[j + 1] != 3)))
                        {
                            AddMeshTemplate(topLeftCornerTemplate, pos, _uvOffsets[element]);
                        }
                    }
                    else if (element == 3) // Air / Sky
                    {
                        var nextRow = grid[(i + offset + 1) % grid.Length];
                        if (nextRow[j] != 0)
                        {
                            AddMeshTemplate(grassCenter, pos, _uvOffsets[element]);
                        }
                    }
                    else
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