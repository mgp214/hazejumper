using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Proxy object that lives between the a "stiff" object (FixedJoint) and "soft/wiggly object (array of SpringJoints)
/// </summary>
public class ManipulatorProxyBehaviour : MonoBehaviour {
	private GameObject fixedObj, springObj;
	private FixedJoint fixedJoint;

	/// <summary>
	/// Creates a new Manipulator proxy.
	/// </summary>
	/// <param name="fixedObj">The object to have a stiff joint with</param>
	/// <param name="springObj">The object to have a soft joint with</param>
	/// <param name="breakForce">How much force it takes to break this proxy and its corresponding joints.</param>
	/// <param name="breakTorque">How much torque it takes to break this proxy and its corresponding joints.</param>
	/// <param name="offsetAmount">How wide apart to make the net of SpringJoints.</param>
	/// <param name="damper">The damping to be used on the SpringJoints</param>
	/// <param name="spring">The spring strength to be used on the SpringJoints</param>
	/// <param name="massScale">The amount to scale the force between the soft joint object and proxy. Higher values make the mass of the soft joint body less impactful.</param>
	/// <returns></returns>
	public static GameObject Create(GameObject fixedObj, GameObject springObj, float breakForce, float breakTorque, float offsetAmount, float damper, float spring, float massScale) {
		var gameObject = new GameObject("Manipulator Proxy");
		gameObject.transform.position = springObj.transform.position;
		var rigidbody = gameObject.AddComponent<Rigidbody>();
		rigidbody.useGravity = false;
		var manipulatorProxyBehaviour = gameObject.AddComponent<ManipulatorProxyBehaviour>();
		manipulatorProxyBehaviour.fixedObj = fixedObj;
		manipulatorProxyBehaviour.springObj = springObj;

		var fixedJoint = gameObject.AddComponent<FixedJoint>();
		manipulatorProxyBehaviour.fixedJoint = fixedJoint;
		fixedJoint.connectedBody = fixedObj.GetComponent<Rigidbody>();
		fixedJoint.massScale = massScale;
		fixedJoint.breakForce = breakForce;
		fixedJoint.breakTorque = breakTorque;

		var offsets = new Vector3[] {
			new Vector3(0,0, -offsetAmount),
			new Vector3(0,0, offsetAmount),
			new Vector3(0,-offsetAmount,0),
			new Vector3(0,offsetAmount,0),
			new Vector3(-offsetAmount,0,0),
			new Vector3(offsetAmount,0,0),
		};
		foreach (var v in offsets) {
			var joint = gameObject.AddComponent<SpringJoint>();
			joint.autoConfigureConnectedAnchor = false;
			joint.anchor = v;
			joint.connectedAnchor = springObj.transform.InverseTransformDirection(v);
			joint.maxDistance = Vector3.Distance(gameObject.transform.TransformDirection(joint.anchor), springObj.transform.TransformDirection(joint.connectedAnchor));
			joint.minDistance = Vector3.Distance(gameObject.transform.TransformDirection(joint.anchor), springObj.transform.TransformDirection(joint.connectedAnchor));
			joint.damper = damper;
			joint.spring = spring;
			joint.connectedBody = springObj.GetComponent<Rigidbody>();
			joint.tolerance = 0.0001f;
		}
		return gameObject;
	}

	/// <summary>
	/// Called when the Chunk we were connected to was merged into a new chunk.
	/// </summary>
	/// <param name="target">the new chunk</param>
	public void SwitchChunk(Chunk owner, Chunk target) {
		if (gameObject) {
			Destroy(gameObject);
			ManipulatorBehaviour.Instance.Anchor(target.rigidbody);
		}
	}

	private void Update() {
		if (fixedObj == null || springObj == null) {
			Destroy(gameObject);
		}
	}

	private void OnJointBreak(float breakForce) {
		ManipulatorBehaviour.Instance.RemoveAnchor(springObj.GetComponent<Chunk>());
	}

	private void OnDrawGizmos() {
		foreach (var j in GetComponents<SpringJoint>()) {
			if (j.connectedBody != null) {
				Gizmos.color = Color.red;
				var source = transform.position + transform.TransformDirection(j.anchor);
				var target = j.connectedBody.position + j.connectedBody.transform.TransformDirection(j.connectedAnchor);
				Gizmos.DrawSphere(source, 0.1f);
				Gizmos.DrawCube(target, Vector3.one * 0.1f);
				Gizmos.DrawLine(source, target);
			}
		}
	}
}