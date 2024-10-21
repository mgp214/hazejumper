using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShoverBehaviour : Useable {

	public LayerMask grabbableLayers;

	[Range(0,30)]
	public float range;
	public float minForce;
	public float maxForce;
	public float fullChargeTime = 5f;
	public AnimationCurve chargeForceCurve;

	private bool isCharging = false;

	public float CurrentForce {
		get {
			return chargeForceCurve.Evaluate(forceCurvePosition) * (maxForce - minForce) + minForce;
		}
	}

	// the current position on the carge force curve (from 0.0 to 1.0)
	[SerializeField]
	private float forceCurvePosition;

	private void Start() {
		this.onPrimary += Charge;
		this.onPrimaryDown += StartChargingShove;
		this.onPrimaryUp += Shove;
	}

	public void StartChargingShove() {
		isCharging = true;
		forceCurvePosition = 0;
	}

	public void Update() {
		if (isCharging) {
			extraValue = $"{forceCurvePosition * 100f:F0}% / {CurrentForce:F0} N";
		} else {
			extraValue = string.Empty;
		}
	}

	void Charge() {
		if (!isCharging) Debug.LogWarning("attempting to charge but isCharging flag is false!");
		if (forceCurvePosition < 1f) {
			forceCurvePosition += Time.deltaTime / fullChargeTime;
		}
		forceCurvePosition = Mathf.Clamp01(forceCurvePosition);
	}

	public void Shove() {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, range, grabbableLayers)) {
				var thing = hit.collider.gameObject.GetComponent<Thing>() ?? hit.collider.GetComponentInParent<Thing>();
				if (thing != null) {
					hit.rigidbody.AddForceAtPosition(ray.direction.normalized * CurrentForce, hit.point);
					thing.QueueMessage(new ForceMessage(PlayerBehaviour.Instance, ray.direction.normalized * CurrentForce));
				}
			}
			forceCurvePosition = 0;
			isCharging = false;
	}
}