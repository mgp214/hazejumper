using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimTargetBehaviour : MonoBehaviour {
	public Transform target;
	public LayerMask raycastLayers;
	public float maxDistance;
	private float currentDistance;
	public float maxChange;

	public float holsterAngle;
	public float holsterHorizontalAmount;

	[Range(0, 1)]
	public float smoothing;

	void Start() {
		currentDistance = (target.transform.position - transform.position).magnitude;
	}

	void Update() {
		if (PlayerBehaviour.Instance.ActiveUseable.SwitchedInFraction == 1) {
			UpdateAimDistance();
		} else {
			UpdateSwitchingAimTarget();
		}

	}

	/// <summary>
	/// Updates the position of the aim target to lower/raise the weapon based on percent switched in/out
	/// </summary>
	void UpdateSwitchingAimTarget() {
		// if (PlayerBehaviour.Instance.ActiveUseable.SwitchedInFraction == 0) 
		target.position = transform.position + transform.forward * maxDistance;
		var percentToRotate = 1 - PlayerBehaviour.Instance.ActiveUseable.SwitchedInFraction;
		target.RotateAround(transform.position, transform.right, holsterAngle * percentToRotate);
		target.position += percentToRotate * transform.right * holsterHorizontalAmount;
	}

	/// <summary>
	/// Updates the position of the aim target based on what is in front of the camera.
	/// </summary>
	void UpdateAimDistance() {
		var newDistance = maxDistance;
		if (Physics.Raycast(
				transform.position,
				transform.forward,
				out var hit,
				maxDistance,
				raycastLayers)) {
			newDistance = (hit.point - transform.position).magnitude;
		}

		if (currentDistance > newDistance) {
			if (currentDistance - newDistance > maxChange)
				newDistance = currentDistance - maxChange;
		} else {
			if (newDistance - currentDistance > maxChange)
				newDistance = currentDistance + maxChange;
		}
		currentDistance = Mathf.Lerp(currentDistance, newDistance, smoothing);
		target.position = transform.position + (transform.forward * currentDistance);
	}
}
