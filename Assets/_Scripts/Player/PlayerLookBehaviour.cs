using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLookBehaviour : MonoBehaviour {

	public float mouseSensitivity, rollSensitivity;
	public float maxTorque;
	public Transform cameraTransform;
	public Transform orientationReference;
	public Transform head;
	public Rigidbody body;

	public AnimationCurve neckBodyCurve;

	public float headPitchRange, headYawRange;

	private float yawInput, pitchInput, rollInput;
	[Range(0, 1)]
	public float inputSmoothing;

	void Start() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update() {
		yawInput = Mathf.Lerp(yawInput, Input.GetAxis("Yaw") * mouseSensitivity, inputSmoothing);
		pitchInput = Mathf.Lerp(pitchInput, Input.GetAxis("Pitch") * mouseSensitivity, inputSmoothing);
		rollInput = Mathf.Lerp(rollInput, Input.GetAxis("Roll") * mouseSensitivity, inputSmoothing);
	}

	void FixedUpdate() {
		if (!PlayerState.Instance.isViewingSac) {
			UpdateYaw();
			UpdatePitch();
			UpdateRoll();
		}
	}

	void UpdateYaw() {


		var localEuler = head.localRotation.eulerAngles;
		var zeroCenteredYawAngle = localEuler.y > 180 ? localEuler.y - 360 : localEuler.y;
		var desiredYawAngle = zeroCenteredYawAngle;
		desiredYawAngle += yawInput;
		desiredYawAngle = Mathf.Clamp(desiredYawAngle, -headYawRange, headYawRange);

		var neckBodyCurveEvaluation = 0.5f;
		if (yawInput != 0)
			neckBodyCurveEvaluation = neckBodyCurve.Evaluate(
				yawInput / Mathf.Abs(yawInput)
				* desiredYawAngle / headYawRange);
		if (Input.GetButton("Use Suit Thrusters"))
			neckBodyCurveEvaluation = 1;

		var appliedYawAngle = Mathf.Lerp(
			zeroCenteredYawAngle,
			desiredYawAngle,
			1 - neckBodyCurveEvaluation);
		head.localRotation = Quaternion.Euler(localEuler.x, appliedYawAngle, localEuler.z);
		var yawTorque = orientationReference.up
			* yawInput
			* maxTorque
			* neckBodyCurveEvaluation;
		body.AddTorque(yawTorque, ForceMode.Acceleration);
	}

	void UpdatePitch() {
		var localEuler = head.localRotation.eulerAngles;
		var zeroCenteredPitchAngle = localEuler.x > 180 ? localEuler.x - 360 : localEuler.x;
		var desiredPitchAngle = zeroCenteredPitchAngle;
		desiredPitchAngle += pitchInput;
		desiredPitchAngle = Mathf.Clamp(desiredPitchAngle, -headPitchRange, headPitchRange);

		var neckBodyCurveEvaluation = 0.5f;
		if (pitchInput != 0)
			neckBodyCurveEvaluation = neckBodyCurve.Evaluate(
				pitchInput / Mathf.Abs(pitchInput)
				* desiredPitchAngle / headPitchRange);
		if (Input.GetButton("Use Suit Thrusters"))
			neckBodyCurveEvaluation = 1;

		var appliedPitchAngle = Mathf.Lerp(
			zeroCenteredPitchAngle,
			desiredPitchAngle,
			1 - neckBodyCurveEvaluation);
		head.localRotation = Quaternion.Euler(appliedPitchAngle, localEuler.y, localEuler.z);
		var pitchTorque = orientationReference.right
			* pitchInput
			* maxTorque
			* neckBodyCurveEvaluation;
		body.AddTorque(-pitchTorque, ForceMode.Acceleration);
	}

	void UpdateRoll() {
		var rollTorque = orientationReference.forward
			* rollInput
			* maxTorque
			* rollSensitivity;
		body.AddTorque(rollTorque, ForceMode.Acceleration);
	}
}
