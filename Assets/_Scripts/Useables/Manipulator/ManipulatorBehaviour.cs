using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Child of Useable. Behaviour logic for the Manipulator tool.
/// </summary>
public class ManipulatorBehaviour : Useable {
	public int maxFreezes;
	public LayerMask grabbableLayers;
	public float range;
	public float offsetAmount;
	public float massScale;
	public float damper;
	public float spring;
	public float breakForce;
	public float breakTorque;
	public float rotateSpeed;
	public float zoomSpeed;

	public List<GameObject> anchorList;
	public List<Chunk> anchoredChunks;
	public GameObject proxy;
	private Rigidbody chunkJustAnchored;

	/// <summary>
	/// Singleton instance for this Behaviour.
	/// </summary>
	public static ManipulatorBehaviour Instance { get; private set; }

	/// <summary>
	/// Destroy an anchor.
	/// </summary>
	/// <param name="anchoredChunk">The Chunk to be unanchored</param>
	public void RemoveAnchor(Chunk anchoredChunk) {
		var anchor = anchorList.Find(a => a.GetComponent<ManipulatorAnchorBehaviour>().proxy.GetComponent<SpringJoint>().connectedBody.gameObject == anchoredChunk.gameObject);
		var proxy = anchor?.GetComponent<ManipulatorAnchorBehaviour>()?.proxy;
		if (proxy != null) {
			foreach (var sj in proxy.GetComponents<SpringJoint>()) {
				sj.connectedBody = null;
			}
		}
		anchorList?.Remove(anchor);
		anchoredChunks.Remove(anchoredChunk);
		Destroy(anchor);
	}

	/// <summary>
	/// Create an anchor / freeze whatever object is being grabbed, or whatever object is in front of us that is in range.
	/// </summary>
	/// <param name="target"></param>
	public void Anchor(Rigidbody target) {
		if (anchorList.Count >= maxFreezes) {
			Destroy(anchorList[0]);
			anchorList.RemoveAt(0);
		}
		var anchor = new GameObject("Manipulator Anchor");
		var anchorRigidbody = anchor.AddComponent<Rigidbody>();
		anchorRigidbody.isKinematic = true;
		anchor.transform.position = target.transform.position;
		var anchorProxy = ManipulatorProxyBehaviour.Create(anchor, target.gameObject, breakForce, breakTorque, offsetAmount, damper, spring, massScale);
		var anchorBehaviour = anchor.AddComponent<ManipulatorAnchorBehaviour>();
		anchorBehaviour.proxy = anchorProxy;
		anchorBehaviour.fixedJoint = anchorProxy.GetComponent<FixedJoint>();
		anchorList.Add(anchor);
		anchorBehaviour.chunk = target.GetComponent<Chunk>();
		anchoredChunks.Add(target.GetComponent<Chunk>());
	}

	private void Update() {
		CanSwitchOut = proxy == null;
	}

	void Start() {
		if (Instance)
			throw new Exception("Cannot have more than one ManipulatorBehaviour instance!");
		Instance = this;
		anchorList = new List<GameObject>();
		anchoredChunks = new List<Chunk>();

		onPrimary += Grab;
		onPrimaryUp += DropGrabTarget;
		onSecondaryDown += ToggleAnchor;
		CanSwitchOut = true;
	}

	/// <summary>
	/// Drop the Chunk we're holding, if we are holding one.
	/// </summary>
	private void DropGrabTarget() {
		chunkJustAnchored = null;
		if (proxy != null) {
			Destroy(proxy);
		}
	}

	/// <summary>
	/// Called when Primary Action is Pressed or Held. Picks up whatever Chunk is in front of us that is in range.
	/// </summary>
	/// <param name="breakAnchors">Whether or not to break any anchors/freezes holding Chunks we want to grab.</param>
	private void Grab() {
		if (proxy == null) {
			//Debug.DrawLine(barrel.position, barrel.position + barrel.forward * range, Color.red, 1);
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, range, grabbableLayers) && hit.rigidbody != chunkJustAnchored) {
				proxy = ManipulatorProxyBehaviour.Create(PlayerCoordinator.Instance.camera.gameObject, hit.rigidbody.gameObject, breakForce, breakTorque, offsetAmount, damper, spring, massScale);
				var chunk = hit.rigidbody.GetComponent<Chunk>();
				for (int i = 0; i < anchorList.Count; i++) {
					var anchor = anchorList[i];
					if (anchor?.GetComponent<ManipulatorAnchorBehaviour>()?.proxy == null) {
						i++;
						RemoveAnchor(chunk);
					}
					if (anchor.GetComponent<ManipulatorAnchorBehaviour>().proxy.GetComponent<SpringJoint>().connectedBody == proxy.GetComponent<SpringJoint>().connectedBody) {
						RemoveAnchor(chunk);
					}
				}
			}
		}
		if (modifier && proxy) {
			PlayerCoordinator.Instance.interceptedMovementInput = true;
			var x = Input.GetAxis("Forward / Backward");
			var y = -Input.GetAxis("Right / Left");
			var z = Input.GetAxis("Roll");
			var zoom = Input.GetAxis("Switch Equipped") * zoomSpeed * Time.deltaTime;
			var rotateVector = new Vector3(x, y, z) * rotateSpeed * Time.deltaTime;
			var proxyJoint = proxy.GetComponent<FixedJoint>();
			var proxyRigidbody = proxy.GetComponent<Rigidbody>();
			var buffer = proxyJoint.connectedBody;
			proxyJoint.connectedBody = null;
			proxy.transform.Rotate(transform.parent.forward, z, Space.World);
			proxy.transform.Rotate(transform.parent.right, x, Space.World);
			proxy.transform.Rotate(transform.parent.up, y, Space.World);
			proxy.transform.Translate(transform.parent.forward * zoom, Space.World);
			proxyJoint.connectedBody = buffer;
		} else {
			PlayerCoordinator.Instance.interceptedMovementInput = false;
		}
	}

	/// <summary>
	/// Toggles freeze / anchor on whatever Chunk is in front of us, or grabbed.
	/// </summary>
	private void ToggleAnchor() {
		if (proxy != null) {
			//if we are carrying something, anchor and drop it.
			var target = proxy.GetComponent<SpringJoint>().connectedBody;
			Anchor(target);
			DropGrabTarget();
			chunkJustAnchored = target;
		} else {
			//anchor or unanchor whatever we are looking at.
			//Debug.DrawLine(barrel.position, barrel.position + barrel.forward * range, Color.red, 1);
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, range, grabbableLayers)) {
				var unfrozeSomething = false;
				for (int i = 0; i < anchorList.Count; i++) {
					var anchor = anchorList[i];
					if (anchor.GetComponent<ManipulatorAnchorBehaviour>().proxy.GetComponent<SpringJoint>().connectedBody == hit.rigidbody) {
						RemoveAnchor(hit.rigidbody.GetComponent<Chunk>());
						unfrozeSomething = true;
					}
				}
				if (!unfrozeSomething) {
					Anchor(hit.rigidbody);
				}
			}
		}
	}
}