using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeldThing : Thing {

	public float weldRadius;
	const float WELD_MASS = 0.1f;

	/// <summary>
	/// Generates the weld's connections and mesh.
	/// </summary>
	public void BuildWeld(Material material, float weldRadius, float compressionStrength, float tensileStrength, float shearStrength) {
		gameObject.layer = LayerMask.NameToLayer("Chunks");
		Initialize();
		this.mass = WELD_MASS;
		this.weldRadius = weldRadius;
		this.compressionStrength = compressionStrength;
		this.tensileStrength = tensileStrength;
		this.shearStrength = shearStrength;

		var hits = Physics.OverlapSphere(transform.position, weldRadius, LayerMask.GetMask("Chunks"));
		var things = hits.Where(c => !(Thing.GetThing(c) is WeldThing)).Distinct();

		foreach (var hit in things) {
			var thing = Thing.GetThing(hit);
			thing.Connect(this, hit.ClosestPoint(transform.position) - transform.position);
		}

		BuildMesh(material);

		var collider = gameObject.AddComponent<SphereCollider>();
		collider.radius = weldRadius * 0.33f;
	}

	private void BuildMesh(Material material) {
		// var hits = Physics.SphereCastAll(transform.position, weldRadius, Vector3.zero, 0, LayerMask.GetMask("Chunks"));
		// if (hits.Length > 0) {

		// }
		var hits = new List<RaycastHit>();
		var longitudeLines = 24;
		var latitudeLines = 24;

		var AngleLongitude = 180f / longitudeLines;
		var angleLatitude = 360f / latitudeLines;

		for (var longitude = 0; longitude <= longitudeLines; longitude++) {
			var longitudeRotation = Quaternion.AngleAxis(longitude * AngleLongitude, Vector3.up);
			var longitudeColor = new Color(Random.value, Random.value, Random.value);
			for (var latitude = 0; latitude < latitudeLines; latitude++) {
				var latitudeRotation = Quaternion.AngleAxis(latitude * angleLatitude, Vector3.forward);
				var vector = latitudeRotation * longitudeRotation * Vector3.forward;

				if (Physics.Raycast(transform.position, vector, out RaycastHit hit, weldRadius, LayerMask.GetMask("Chunks"))) {
					hits.Add(hit);
					Debug.DrawRay(transform.position, hit.point - transform.position, Color.red, 2);
				} else {
					Debug.DrawRay(transform.position, vector.normalized * weldRadius, Color.white, 2);
				}
			}
		}

		Debug.Log($"found {hits.Count} hits out of {longitudeLines * (latitudeLines + 1)}");
		var mesh = new Mesh();
		var vertices = new List<Vector3>();
		// adding the origin point
		vertices.Add(transform.InverseTransformPoint(transform.position));
		vertices.AddRange(hits.Select(hit => transform.InverseTransformPoint(hit.point)));

		// mesh.vertices = vertices.ToArray();
		var result = MIConvexHull.ConvexHull.Create(vertices.Select(x => new Vertex(x)).ToArray());
		mesh.vertices = result.Result.Points.Select(x => x.ToVec()).ToArray();
		var vertexList = result.Result.Points.ToList();

		var triangles = new List<int>();
		foreach (var face in result.Result.Faces) {
			triangles.Add(vertexList.IndexOf(face.Vertices[0]));
			triangles.Add(vertexList.IndexOf(face.Vertices[1]));
			triangles.Add(vertexList.IndexOf(face.Vertices[2]));
		}

		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.RecalculateTangents();

		var mFilter = gameObject.AddComponent<MeshFilter>();
		mFilter.mesh = mesh;
		var mRenderer = gameObject.AddComponent<MeshRenderer>();
		mRenderer.material = material;
	}

	/// <summary>
	/// How much force this weld can take before breaking.
	/// </summary>
	public float compressionStrength, tensileStrength, shearStrength;

	public virtual void Break() {
		var disconnectedThings = new List<Thing>(connectedThings);
		while (connectedThings.Count > 0) {
			connectedThings[0].Disconnect(this);
			connectedThings.RemoveAt(0);
		}
		var thingsToMakeChunksFrom = new List<Thing>();
		var thingsScanned = new List<Thing>();
		foreach (var thing in disconnectedThings) {
			var newlyScannedThings = thing.GetConnectedThings(new List<Thing>());
			if (!thingsScanned.Exists(t => newlyScannedThings.Contains(t))) {
				thingsToMakeChunksFrom.Add(thing);
			}
			thingsScanned.AddRange(newlyScannedThings);
		}
		foreach (var thing in thingsToMakeChunksFrom) {
			if (thing != this) {
				Chunk.Create(thing);
			}
		}
		Destroy(gameObject);
	}
}