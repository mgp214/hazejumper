using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Behaviour logic for the Player object.
/// </summary>
public class PlayerBehaviour : MonoBehaviour {
	public Useable[] useables;
	public bool isBusy;
	public Text interactionText;
	public AimIK aimIk;
	public HandDataController handDataController;
	private static PlayerBehaviour _Instance;

	[SerializeField]
	private int selectedUseableSlot;
	public Text currentToolTextElement;
	public Text currentToolExtraTextElement;

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
		ActiveUseable = useables[selectedUseableSlot];
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update() {
		isBusy = IsBusy;
		InputUpdate();
	}

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
				aimIk.solver.transform = ActiveUseable.IkAimTransform;
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
		currentToolTextElement.text = ActiveUseable.name;
		currentToolExtraTextElement.text = ActiveUseable.extraValue;
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