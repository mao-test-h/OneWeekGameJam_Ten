﻿namespace MainContents.ECS
{
    using System;
    using System.Linq;
    using UnityEngine;
    using Unity.Rendering;

    /// <summary>
    /// MeshInstanceRendererに渡すデータ
    /// </summary>
    [Serializable]
    public sealed class SpriteMeshInstanceRenderer
    {
        /// <summary>
        /// 表示スプライト
        /// </summary>
        [SerializeField] public Sprite Sprite;

        /// <summary>
        /// 表示マテリアル
        /// </summary>
        [SerializeField] public Material Material;
    }

    /// <summary>
    /// Sprite補助クラス
    /// </summary>
    public static class SpriteUtility
    {
        /// <summary>
        /// MeshInstanceRendererの生成
        /// </summary>
        /// <param name="data">表示データ</param>
        /// <returns>生成したMeshInstanceRenderer</returns>
        public static MeshInstanceRenderer CreateMeshInstanceRenderer(SpriteMeshInstanceRenderer data)
        {
            // Sprite to Mesh
            var mesh = new Mesh();
            var sprite = data.Sprite;
            mesh.SetVertices(Array.ConvertAll(sprite.vertices, _ => (Vector3)_).ToList());
            mesh.SetUVs(0, sprite.uv.ToList());
            mesh.SetTriangles(Array.ConvertAll(sprite.triangles, _ => (int)_), 0);

            var matInst = new Material(data.Material);

            // 渡すマテリアルはGPU Instancingに対応させる必要がある
            var meshInstanceRenderer = new MeshInstanceRenderer();
            meshInstanceRenderer.mesh = mesh;
            meshInstanceRenderer.material = matInst;
            return meshInstanceRenderer;
        }

        /// <summary>
        /// MeshInstanceRendererの生成
        /// </summary>
        /// <param name="data">表示データ</param>
        /// <param name="offset">デフォルトの移動値(モデル空間から見たオフセット)</param>
        /// <param name="rotation">デフォルトの回転値</param>
        /// <param name="size">デフォルトのスケール</param>
        /// <returns>生成したMeshInstanceRenderer</returns>
        public static MeshInstanceRenderer CreateMeshInstanceRenderer(
            SpriteMeshInstanceRenderer data, Vector3 offset, Quaternion rotation, Vector3 scale)
        {
            // Sprite to Mesh
            var mesh = new Mesh();
            var sprite = data.Sprite;

            var vertices = Array.ConvertAll(data.Sprite.vertices, _ => (Vector3)_).ToList();
            Matrix4x4 mat = Matrix4x4.TRS(offset, rotation, scale);
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = mat.MultiplyPoint3x4(vertices[i]);
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, sprite.uv.ToList());
            mesh.SetTriangles(Array.ConvertAll(sprite.triangles, _ => (int)_), 0);

            var matInst = new Material(data.Material);

            // 渡すマテリアルはGPU Instancingに対応させる必要がある
            var meshInstanceRenderer = new MeshInstanceRenderer();
            meshInstanceRenderer.mesh = mesh;
            meshInstanceRenderer.material = matInst;
            return meshInstanceRenderer;
        }
    }
}
