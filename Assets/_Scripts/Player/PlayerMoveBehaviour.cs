using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveBehaviour : MonoBehaviour {

	public float maxForce;
	public Transform orientationReference;
	public new Rigidbody rigidbody;

	void FixedUpdate() {
		if (PlayerCoordinator.Instance.interceptedMovementInput)
			return;

		var direction = new Vector3(
			Input.GetAxis("Right / Left"),
			Input.GetAxis("Up / Down"),
			Input.GetAxis("Forward / Backward")
		);

		var appliedForce = direction.normalized * maxForce;
		appliedForce = orientationReference.TransformDirection(appliedForce);
		rigidbody.AddForce(appliedForce, ForceMode.Force);
	}
}
