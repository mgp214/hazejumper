using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimTargetBehaviour : MonoBehaviour {
	public Transform target;
	public LayerMask raycastLayers;
	public float maxDistance;
	private float currentDistance;
	public float maxChange;

	[Range(0, 1)]
	public float smoothing;

	void Start() {
		currentDistance = (target.transform.position - transform.position).magnitude;
	}

	void Update() {
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
