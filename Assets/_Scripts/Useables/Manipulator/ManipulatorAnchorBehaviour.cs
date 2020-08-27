using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behaviour logic for the Manipulator tool's anchors for "frozen" Chunks.
/// </summary>
public class ManipulatorAnchorBehaviour : MonoBehaviour {

	/// <summary>
	/// The GameObject containing the ManipulatorProxyBehaviour component that binds this anchor with a Chunk.
	/// </summary>
	public GameObject proxy;

	public FixedJoint fixedJoint;
	public Chunk chunk;

	private void Update() {
		if (proxy == null || fixedJoint == null) {
			ManipulatorBehaviour.Instance.RemoveAnchor(chunk);
		}
		foreach (var anchor in new List<GameObject>(ManipulatorBehaviour.Instance.anchorList)) {
			if (anchor != gameObject
				&& anchor.GetComponent<ManipulatorAnchorBehaviour>().proxy.GetComponent<ManipulatorProxyBehaviour>().GetComponent<SpringJoint>().connectedBody
					== proxy.GetComponent<SpringJoint>().connectedBody) {
				ManipulatorBehaviour.Instance.RemoveAnchor(chunk);
			}
		}
	}

	private void OnJointBreak(float breakForce) {
		ManipulatorBehaviour.Instance.RemoveAnchor(chunk);
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, 0.3f);
	}
}