using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Busta.Diggy
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridMesh : MonoBehaviour
    {
        [Header("Config")]
        public Vector2[] uvOffsets;

        public int textureSize;

        [Header("Assets")]
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

        [Header("References")]
        public MeshFilter edges;

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
            var w = decoWidth * 2 + size.x;
            var row = InitRow(1, w, decoWidth).ToArray();
            var grid = Enumerable.Repeat(row, size.y).ToArray();
            grid[0] = InitRow(3, w, decoWidth).ToArray();
            var decorationMesh = new Mesh();
            UpdateMesh(grid, 0, decorationMesh);

            var edgeTransform = edges.transform;
            edgeTransform.SetParent(transform);
            edgeTransform.localPosition = new Vector3(-decoWidth, 0, 0);
            edges.mesh = decorationMesh;
        }

        private int[] InitRow(int element, int count, int edgeSize)
        {
            var row = Enumerable.Repeat(element, count).ToArray();
            for (var i = edgeSize; i < row.Length - edgeSize; i++)
            {
                row[i] = -1;
            }

            return row;
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

                    switch (element)
                    {
                        case -1: // Empty
                            break;
                        case 0: // hole
                        {
                            AddMeshTemplate(backFaceTemplate, pos, _uvOffsets[element]);

                            int neighbor = 0;

                            var left = j <= 0 || (neighbor = row[j - 1]) != 0;
                            if (left)
                            {
                                AddMeshTemplate(rightFaceTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            var right = j >= row.Length - 1 || (neighbor = row[j + 1]) != 0;
                            if (right)
                            {
                                AddMeshTemplate(leftFaceTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            var previousRow = grid[(i + offset + grid.Length - 1) % grid.Length];

                            var top = (neighbor = previousRow[j]) != 0 && neighbor != 3;
                            if (top)
                            {
                                AddMeshTemplate(bottomFaceTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            var nextRow = grid[(i + offset + 1) % grid.Length];

                            var bottom = (neighbor = nextRow[j]) != 0;
                            if (bottom)
                            {
                                AddMeshTemplate(topFaceTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            if (!top && !left && (j <= 0 || ((neighbor = previousRow[j - 1]) != 0 && neighbor != 3)))
                            {
                                AddMeshTemplate(bottomRightCornerTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            if (!top && !right && (j >= row.Length - 1 ||
                                                   ((neighbor = previousRow[j + 1]) != 0 && neighbor != 3)))
                            {
                                AddMeshTemplate(bottomLeftCornerTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            if (!bottom && !left && (j <= 0 || ((neighbor = nextRow[j - 1]) != 0 && neighbor != 3)))
                            {
                                AddMeshTemplate(topRightCornerTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            if (!bottom && !right &&
                                (j >= row.Length - 1 || ((neighbor = nextRow[j + 1]) != 0 && neighbor != 3)))
                            {
                                AddMeshTemplate(topLeftCornerTemplate, pos, GetUvOffset(element, neighbor));
                            }

                            break;
                        }
                        // Air / Sky
                        case 3:
                        {
                            var nextRow = grid[(i + offset + 1) % grid.Length];
                            if (nextRow[j] != 0)
                            {
                                AddMeshTemplate(grassCenter, pos, Vector2.zero);
                            }else if (j <= 0 || nextRow[j - 1] != 0)
                            {
                                AddMeshTemplate(grassRight, pos, Vector2.zero);
                            }else if (j >= row.Length - 1 || nextRow[j + 1] != 0)
                            {
                                AddMeshTemplate(grassLeft, pos, Vector2.zero);
                            }

                            break;
                        }
                        // Every other solid block
                        default:
                            AddMeshTemplate(frontFaceTemplate, pos, _uvOffsets[element]);
                            break;
                    }
                }
            }

            UpdateMesh(updateMesh == null ? _mesh : updateMesh);
        }

        private Vector2 GetUvOffset(int element, int neighbor)
        {
            return element != 3 ? _uvOffsets[neighbor] : Vector2.zero;
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