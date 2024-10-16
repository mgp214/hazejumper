using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public interface IMarching {

	float Surface { get; set; }

	void Generate(float[,,] voxels, Vector3Int size, IList<Vector3> verts, IList<int> indices);

}

