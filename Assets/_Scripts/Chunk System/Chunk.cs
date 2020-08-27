using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Thing;

/// <summary>
/// A composite, physics-enabled object that encapsulates a rigid collection of Things.
/// </summary>
public class Chunk : MonoBehaviour {
	public float drag = 0.5f;

	public float angularDrag = 0.1f;

	public new Rigidbody rigidbody;

	public Thing[] visibleThings;

	private static int chunkCount = 0;

	private List<Thing> things = new List<Thing>();

	private bool initialized = false;
	public SpaceState State { get; private set; }

	/// <summary>
	/// Returns true if and only if there is only one Thing in this Chunk.
	/// </summary>
	public bool IsAlone {
		get {
			return things.Count == 1;
		}
	}

	public enum SpaceState {
		Normal,
		NormalPreview,
		Subspace,
		SubspacePreview,
		SacWalls
	}

	public static int GetLayerMask(params SpaceState[] states) {
		var layers = new List<string>();
		if (states.Contains(SpaceState.Normal))
			layers.Add("Chunks");
		if (states.Contains(SpaceState.Subspace))
			layers.Add("Subspace");
		if (states.Contains(SpaceState.SacWalls))
			layers.Add("Sac Walls");
		if (states.Contains(SpaceState.NormalPreview))
			layers.Add("Chunks Preview");
		if (states.Contains(SpaceState.SubspacePreview))
			layers.Add("Subspace Preview");
		return LayerMask.GetMask(layers.ToArray());
	}

	public static int GetLayer(SpaceState state) {
		switch (state) {
			case SpaceState.Normal:
				return LayerMask.NameToLayer("Chunks");

			case SpaceState.Subspace:
				return LayerMask.NameToLayer("Subspace");

			case SpaceState.NormalPreview:
				return LayerMask.NameToLayer("Chunks Preview");

			case SpaceState.SubspacePreview:
				return LayerMask.NameToLayer("Subspace Preview");

			default:
				throw new Exception("unknown state?!");
		}
	}

	public static string GetLayerName(SpaceState state) {
		switch (state) {
			case SpaceState.Normal:
				return "Chunks";

			case SpaceState.NormalPreview:
				return "Chunks Preview";

			case SpaceState.Subspace:
				return "Subspace";

			case SpaceState.SubspacePreview:
				return "Subspace Preview";

			default:
				throw new Exception("unknown state?!");
		}
	}

	/// <summary>
	/// Instantiates a new Chunk around the given Thing.
	/// </summary>
	/// <param name="thing">The Thing to encapulate in the new Chunk.</param>
	/// <returns>Newly built Chunk.</returns>
	public static Chunk Create(Thing thing) {
		var gameObject = new GameObject("Chunk " + ++chunkCount);
		gameObject.AddComponent<Rigidbody>();
		gameObject.AddComponent<Chunk>();

		var chunk = gameObject.GetComponent<Chunk>();
		chunk.AddThings(thing.GetConnectedThings(new List<Thing>()));
		chunk.Initialize();
		gameObject.layer = thing.gameObject.layer;
		if (LayerMask.LayerToName(thing.gameObject.layer) == "Chunks") {
			chunk.State = SpaceState.Normal;
			chunk.rigidbody.interpolation = RigidbodyInterpolation.None;
		} else {
			gameObject.name = $"Subspace {gameObject.name}";
			chunk.State = SpaceState.Subspace;
			chunk.rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			SacBehaviour.Instance.AddContents(chunk.rigidbody);
		}
		return chunk;
	}

	public static Chunk GetChunk(Collider collider) {
		return collider.gameObject.GetComponentInParent<Chunk>();
	}

	public void Initialize() {
		rigidbody = GetComponent<Rigidbody>();
		rigidbody.drag = drag;
		rigidbody.angularDrag = angularDrag;
		rigidbody.useGravity = false;
		rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		Calculate();
	}

	public void Start() {
		if (!initialized)
			Initialize();
	}

	public Chunk Merge(Chunk chunk) {
		if (things.Count >= chunk.things.Count) {
			//remove any existing anchors and adding one to JUST THIS chunk.
			var manipulator = ManipulatorBehaviour.Instance;
			var hasAnchor = false;
			if (manipulator.anchoredChunks.Contains(this)) {
				manipulator.RemoveAnchor(this);
				hasAnchor = true;
			}
			if (manipulator.anchoredChunks.Contains(chunk)) {
				manipulator.RemoveAnchor(chunk);
				hasAnchor = true;
			}
			foreach (var t in chunk.things) {
				AddThing(t);
			}
			Calculate();
			rigidbody.velocity = ((rigidbody.velocity * rigidbody.mass)
								+ (chunk.rigidbody.velocity * chunk.rigidbody.mass))
								/ (rigidbody.mass + chunk.rigidbody.mass);
			if (hasAnchor) {
				manipulator.Anchor(rigidbody);
			}

			//we have become one, this isn't goodbye. It's a new beginning.
			Destroy(chunk.gameObject);
			return this;
		}
		//if we're smaller, call this from the other chunk
		return chunk.Merge(this);
	}

	public void ChangeSpaceState(SpaceState state) {
		gameObject.layer = GetLayer(state);
		switch (state) {
			case SpaceState.Normal:
				rigidbody.interpolation = RigidbodyInterpolation.None;
				break;

			case SpaceState.Subspace:
				SacBehaviour.Instance.AddContents(rigidbody);
				rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				break;

			case SpaceState.NormalPreview:
				break;

			case SpaceState.SubspacePreview:
				SacBehaviour.Instance.RemoveContents(rigidbody);
				break;

			default:
				throw new Exception("unknown state?!");
		}

		if (ManipulatorBehaviour.Instance.anchoredChunks.Contains(this)) {
			ManipulatorBehaviour.Instance.RemoveAnchor(this);
		}
		foreach (var thing in things) {
			thing.gameObject.layer = gameObject.layer;
		}
		State = state;
	}

	/// <summary>
	/// Checks if the chunk would collide with any other chunks in the given state. Returns true if there is a collision.
	/// </summary>
	/// <param name="states">The states to check for collisions on.</param>
	/// <returns>Returns true if they is a collision.</returns>
	public bool CheckCollisionWith(params SpaceState[] states) {
		if (states.Contains(State)) {
			Debug.LogError("Trying to check collision with current layer (always true!)");
			return true;
		}
		var layers = GetLayerMask(states);
		foreach (var thing in things) {
			if (thing.collider is BoxCollider) {
				var boxCollider = thing.collider as BoxCollider;
				var scale = new Vector3(
					boxCollider.size.x * thing.transform.lossyScale.x,
					boxCollider.size.y * thing.transform.lossyScale.y,
					boxCollider.size.z * thing.transform.lossyScale.z
					);
				if (Physics.CheckBox(thing.transform.position + boxCollider.center, scale / 2, thing.transform.rotation, layers))
					return true;
			} else if (thing.collider is SphereCollider) {
				var sphereCollider = thing.collider as SphereCollider;
				var scaleMax = Mathf.Max(thing.transform.lossyScale.x,
					thing.transform.lossyScale.y,
					thing.transform.lossyScale.z);
				if (Physics.CheckSphere(thing.transform.position + sphereCollider.center, sphereCollider.radius * scaleMax, layers))
					return true;
			} else {
				Debug.LogError($"{thing.name} has a non-standard collider!");
				return true;
			}
		}
		return false;
	}

	private void OnDestroy() {
		SacBehaviour.Instance.RemoveContents(rigidbody);
	}

	private void Update() {
		for (int i = 0; i < things.Count; i++) {
			var thing = things[i];
			if (thing == null || thing.transform.parent != transform) {
				i--;
				things.Remove(thing);
			}
		}
		if (things.Count == 0) {
			Destroy(gameObject);
		}
		visibleThings = things.ToArray();
	}

	/// <summary>
	/// Calculates Center of Mass, Inertia Tensor, and Inertia Tensor Rotation for this Chunk.
	/// </summary>
	private void Calculate() {
		var mass = 0f;
		var centerOfMass = Vector3.zero;
		var velocity = rigidbody.velocity;
		var angularVelocity = rigidbody.angularVelocity;
		foreach (var thing in things) {
			thing.transform.parent = null;
			//only change the CoM if the thing has mass.
			if (thing.mass != 0) {
				centerOfMass = new Vector3(
						((centerOfMass.x) * mass + (thing.transform.position.x * thing.mass)) / (mass + thing.mass),
						((centerOfMass.y) * mass + (thing.transform.position.y * thing.mass)) / (mass + thing.mass),
						((centerOfMass.z) * mass + (thing.transform.position.z * thing.mass)) / (mass + thing.mass)
					);
				mass += thing.mass;
			}
		}

		rigidbody.mass = mass;
		transform.position = centerOfMass;
		rigidbody.centerOfMass = Vector3.zero;
		rigidbody.ResetInertiaTensor();

		foreach (var thing in things) {
			thing.transform.SetParent(transform);
		}

		rigidbody.centerOfMass = Vector3.zero;
		rigidbody.velocity = velocity;
		rigidbody.angularVelocity = angularVelocity;
	}

	/// <summary>
	/// Adds a given Thing to the chunk.
	/// </summary>
	/// <param name="thing">The Thing to add to the Chunk.</param>
	private void AddThing(Thing thing) {
		things.Add(thing);
		thing.parentChunk = this;
	}

	/// <summary>
	/// Adds a given Thing to the chunk.
	/// </summary>
	/// <param name="thing">The Thing to add to the Chunk.</param>
	private void AddThings(IEnumerable<Thing> things) {
		foreach (var thing in things) {
			this.things.Add(thing);
			thing.parentChunk = this;
		}
	}

	private void OnCollisionEnter(Collision collision) {
		var point = collision.GetContact(0);
		var thing = Thing.GetThing(point.thisCollider);
		var force = collision.impulse;
		//Debug.Log($"New collision from outside chunk with {collision.gameObject.name}");
		thing.QueueMessage(new ForceMessage(point, force));
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.red;
		Gizmos.DrawCube(transform.position + transform.TransformDirection(rigidbody.centerOfMass), Vector3.one * 0.1f);
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * rigidbody.inertiaTensorRotation, transform.localScale);

		Gizmos.DrawWireCube(rigidbody.centerOfMass, rigidbody.inertiaTensor);
	}
}