using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class VoxelObject : MonoBehaviour {
	[ContextMenuItem("generate", "Generate")]
	public Vector3Int size;
	public int seed;
	public float noiseScale;
	public int radius;

	[SerializeField]
	public float[,,] data;

	public void Generate() {
		NoiseS3D.seed = seed;
		data = new float[size.x, size.y, size.z];
		for (int x = 0; x < size.x; x++) {
			for (int y = 0; y < size.y; y++) {
				for (int z = 0; z < size.z; z++) {
					// data[x, y, z] = (float)NoiseS3D.Noise(x / noiseScale, y / noiseScale, z / noiseScale);
					data[x, y, z] = IsPointInSphere(x, y, z) ? 1 : -1;
				}
			}
		}

		var verts = new List<Vector3>();
		var indices = new List<int>();

		var marching = new MarchingCubes();

		marching.Generate(data, size, verts, indices);

		var m = new Mesh();
		m.SetVertices(verts);
		m.SetTriangles(indices.ToArray(), 0);
		m.RecalculateBounds();
		m.RecalculateNormals();
		GetComponent<MeshFilter>().mesh = m;
	}

	public bool IsPointInSphere(int x, int y, int z) {
		Vector3Int center = new Vector3Int(size.x / 2, size.y / 2, size.z / 2);
		return Vector3Int.Distance(center, new Vector3Int(x, y, z)) < radius;
	}
}