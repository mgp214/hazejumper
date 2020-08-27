using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Behaviour logic for the Player object.
/// </summary>
public class PlayerBehaviour : MonoBehaviour {
	// public float lookTorque;
	// public float angDampingLimit;
	// public float angDampingCoeff;
	// public float moveForce;
	// public float moveDampingLimit;
	// public float moveDampingCoeff;
	public Useable[] useables;

	public bool isBusy;
	public Text interactionText;
	private static PlayerBehaviour _Instance;

	[SerializeField]
	private int selectedUseableSlot;

	private new Rigidbody rigidbody;
	private float initialMassCoefficient;

	public static PlayerBehaviour Instance {
		get {
			return _Instance;
		}
	}

	public Useable ActiveUseable { get; private set; }

	/// <summary>
	/// True if the player is busy and cannot change focus to the UI.
	/// </summary>
	public bool IsBusy {
		get {
			return !(ActiveUseable?.CanSwitchOut ?? true)
				&& !(ActiveUseable is SubspacerBehaviour);
		}
	}

	public void SetAsActive(Useable useable) {
		var index = useables.ToList().IndexOf(useable);
		if (index == -1)
			throw new System.Exception("That useable isn't on the player's belt!");
		selectedUseableSlot = index;
	}

	void Start() {
		if (_Instance != null)
			throw new System.Exception("Cannot instantiate more than one player!");

		_Instance = this;
		rigidbody = GetComponent<Rigidbody>();
		initialMassCoefficient = rigidbody.mass;
		// moveForce *= initialMassCoefficient;
		// lookTorque *= initialMassCoefficient;
		// moveDampingLimit *= initialMassCoefficient;
		ActiveUseable = useables[selectedUseableSlot];
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update() {
		isBusy = IsBusy;
		InputUpdate();
	}

	private void FixedUpdate() {
		// LookUpdate();
		// MoveUpdate();
	}

	/// <summary>
	/// Update logic for mouselook.
	// /// </summary>
	// private void LookUpdate() {
	// 	var yaw = Input.GetAxis("Yaw");
	// 	var pitch = Input.GetAxis("Pitch");
	// 	var roll = Input.GetAxis("Roll");

	// 	//if we are holding the action modifier on the manipulator, we're stealing the Roll keys for manipulation -- don't use them here.
	// 	if (Input.GetButton("Action Modifier") && ActiveUseable is ManipulatorBehaviour) {
	// 		roll = 0;
	// 	}

	// 	//movement keys are also used for Sac view rotation
	// 	if (SacBehaviour.Instance.IsMaximized) {
	// 		yaw = pitch = roll = 0;
	// 	}

	// 	var dampingX = rigidbody.angularVelocity.x > 0
	// 	? Mathf.Min(rigidbody.angularVelocity.x * rigidbody.mass, angDampingLimit)
	// 	: Mathf.Max(rigidbody.angularVelocity.x * rigidbody.mass, -angDampingLimit);
	// 	var dampingY = rigidbody.angularVelocity.y > 0
	// 		? Mathf.Min(rigidbody.angularVelocity.y * rigidbody.mass, angDampingLimit)
	// 		: Mathf.Max(rigidbody.angularVelocity.y * rigidbody.mass, -angDampingLimit);
	// 	var dampingZ = rigidbody.angularVelocity.z > 0
	// 		? Mathf.Min(rigidbody.angularVelocity.z * rigidbody.mass, angDampingLimit)
	// 		: Mathf.Max(rigidbody.angularVelocity.z * rigidbody.mass, -angDampingLimit);

	// 	var dampingVector = new Vector3(dampingX, dampingY, dampingZ) * -1;

	// 	var rotateVector = new Vector3(pitch, yaw, roll) * lookTorque * Time.deltaTime;

	// 	rigidbody.AddRelativeTorque(rotateVector);
	// 	rigidbody.AddTorque(dampingVector, ForceMode.Force);
	// }

	/// <summary>
	/// Update logic for movement.
	// /// </summary>
	// private void MoveUpdate() {
	// 	var x = Input.GetAxis("Right / Left");
	// 	var y = Input.GetAxis("Up / Down");
	// 	var z = Input.GetAxis("Forwards / Backwards");

	// 	//if we are holding the action modifier on the manipulator, don't use move keys for movement! (Except up/down, because we don't need those.)
	// 	if (Input.GetButton("Action Modifier") && ActiveUseable is ManipulatorBehaviour) {
	// 		x = z = 0;
	// 	}
	// 	//movement keys are also used for Sac view rotation
	// 	if (SacBehaviour.Instance.IsMaximized) {
	// 		x = y = z = 0;
	// 	}

	// 	var locVelocity = transform.InverseTransformDirection(rigidbody.velocity);

	// 	var dampingX = locVelocity.x > 0
	// 		? Mathf.Min(locVelocity.x * rigidbody.mass, moveDampingLimit)
	// 		: Mathf.Max(locVelocity.x * rigidbody.mass, -moveDampingLimit);
	// 	var dampingY = locVelocity.y > 0
	// 		? Mathf.Min(locVelocity.y * rigidbody.mass, moveDampingLimit)
	// 		: Mathf.Max(locVelocity.y * rigidbody.mass, -moveDampingLimit);
	// 	var dampingZ = locVelocity.z > 0
	// 		? Mathf.Min(locVelocity.z * rigidbody.mass, moveDampingLimit)
	// 		: Mathf.Max(locVelocity.z * rigidbody.mass, -moveDampingLimit);

	// 	var dampingVector = new Vector3(
	// 			Mathf.Approximately(x, 0) ? dampingX : 0,
	// 			Mathf.Approximately(y, 0) ? dampingY : 0,
	// 			Mathf.Approximately(z, 0) ? dampingZ : 0
	// 		) * -moveDampingCoeff;

	// 	var moveVector = new Vector3(x, y, z) * moveForce * Time.deltaTime;

	// 	rigidbody.AddRelativeForce(moveVector, ForceMode.Force);
	// 	rigidbody.AddRelativeForce(dampingVector);
	// }

	/// <summary>
	/// Update logic for all other input.
	/// </summary>
	private void InputUpdate() {
		//handle switching between useables
		var currentSlot = selectedUseableSlot;
		if (ActiveUseable.CanSwitchOut && !SacBehaviour.Instance.IsMaximized
			&& !(ActiveUseable is WirerBehaviour && Input.GetButton("Action Modifier"))) {
			if (Input.GetAxis("Switch Equipped") < 0) {
				if (selectedUseableSlot + 1 == useables.Length) {
					selectedUseableSlot = 0;
				} else {
					selectedUseableSlot++;
				}
			}
			if (Input.GetAxis("Switch Equipped") > 0) {
				if (selectedUseableSlot == 0) {
					selectedUseableSlot = useables.Length - 1;
				} else {
					selectedUseableSlot--;
				}
			}
		}
		if (ActiveUseable != useables[selectedUseableSlot]) {
			var switchedOut = ActiveUseable?.SwitchOut() ?? true;
			if (switchedOut) {
				ActiveUseable?.Deselect();
				ActiveUseable = useables[selectedUseableSlot];
				ActiveUseable?.Select();
			}
		} else {
			if (ActiveUseable) {
				var switchedIn = ActiveUseable.SwitchIn();

				if (switchedIn) {
					var primary = Input.GetButton("Primary Action");
					var secondary = Input.GetButton("Secondary Action");
					var modifier = Input.GetButton("Action Modifier");
					if (!SacBehaviour.Instance.IsMaximized) {
						ActiveUseable.SetInput(primary, secondary, modifier);
					}
				}
			}
		}
		interactionText.text = string.Empty;
		var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
		if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 2f, Chunk.GetLayerMask(Chunk.SpaceState.Normal))) {
			IInteractable interactable = hit.collider.gameObject.GetComponent<IInteractable>();
			if (interactable == null) {
				interactable = hit.collider.gameObject.GetComponentInParent<IInteractable>();
			}
			if (interactable == null)
				return;
			interactionText.text = interactable.GetText();
			if (Input.GetButtonDown("Interact")) {
				interactable.Interact();
			}
		}
	}
}