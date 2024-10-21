using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent class for all "Useable" obects: Tools, Weapons, et cetera. Handles input event logic.
/// </summary>
public class Useable : MonoBehaviour {

	public Transform IkAimTransform;
	public HandData handData;

	public float switchDuration;
	private float switchProgressDuration;

	public string extraValue = string.Empty;

	public float SwitchedInFraction { get { return switchProgressDuration / switchDuration; } }

	/// <summary>
	/// Called each frame the primary action is held down.
	/// </summary>
	protected Action onPrimary;

	/// <summary>
	/// Called on the first frame the primary action is pressed.
	/// </summary>
	protected Action onPrimaryDown;

	/// <summary>
	/// Called the first frame the primary action is released.
	/// </summary>
	protected Action onPrimaryUp;

	/// <summary>
	/// Called each frame the secondary action is held down.
	/// </summary>
	protected Action onSecondary;

	/// <summary>
	/// Called on the first frame the secondary action is pressed.
	/// </summary>
	protected Action onSecondaryDown;

	/// <summary>
	/// Called the first frame the secondary action is released.
	/// </summary>
	protected Action onSecondaryUp;

	/// <summary>
	/// Called each frame the action modifier is held down.
	/// </summary>
	protected Action onModifier;

	/// <summary>
	/// Called on the first frame the action modifier is pressed.
	/// </summary>
	protected Action onModifierDown;

	/// <summary>
	/// Called the first frame the action modifier is released.
	/// </summary>
	protected Action onModifierUp;

	/// <summary>
	/// Called every frame niether the primary nor secondary actions are being pressed.
	/// </summary>
	protected Action onIdle;

	/// <summary>
	/// Called when this useable should begin being "deployed"
	/// </summary>
	protected Action onSelected;

	/// <summary>
	/// Called when this useable should begin being "retracted"
	/// </summary>
	protected Action onDeselected;

	protected bool primary = false, secondary = false, modifier = false;

	protected bool IdleAvailable { get { return SwitchedInFraction == 1f; } }

	/// <summary>
	/// Indicates whether or not this Useable can be switched out at the moment.
	/// </summary>
	public bool CanSwitchOut { get; protected set; } = true;

	/// <summary>
	/// Incrementally switches this tool into selection.
	/// </summary>
	/// <returns>True when complete, otherwise false.</returns>
	public virtual bool SwitchIn() {
		if (switchProgressDuration == switchDuration) return true;
		var handDataController = PlayerBehaviour.Instance.handDataController;
		if (switchProgressDuration == 0) {
			handDataController.handData = handData;
			handDataController.Apply();
		}
		switchProgressDuration += Time.deltaTime;
		if (switchProgressDuration >= switchDuration) {
			switchProgressDuration = switchDuration;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Incrementally switches this tool out of selection.
	/// </summary>
	/// <returns>True when complete, otherwise false.</returns>
	public virtual bool SwitchOut() {
		if (switchProgressDuration == 0) return true;
		switchProgressDuration -= Time.deltaTime;
		if (switchProgressDuration <= 0) {
			switchProgressDuration = 0;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Called when this Useable is deselected.
	/// </summary>
	public void Deselect() {
		transform.GetChild(0).gameObject.SetActive(false);
		onDeselected?.Invoke();
	}

	/// <summary>
	/// Called when this Useable is selected.
	/// </summary>
	public void Select() {
		transform.GetChild(0).gameObject.SetActive(true);
		onSelected?.Invoke();
	}

	/// <summary>
	/// Sets the input values for this frame. Calls any relevant actions.
	/// </summary>
	/// <param name="primary">The value of the primary action input.</param>
	/// <param name="secondary">The value of the secondary action input.</param>
	/// <param name="modifier">The value of the action modifier input</param>
	public void SetInput(bool primary, bool secondary, bool modifier) {
		//idle if we aren't pressing anything for a full frame and it's available (e.g. not switching)
		if (IdleAvailable && !(primary && secondary && this.primary && this.secondary)) {
			onIdle?.Invoke();
		}

		//handle possible primary action events.
		if (primary) {
			if (!this.primary) {
				onPrimaryDown?.Invoke();
			}
			onPrimary?.Invoke();
			this.primary = true;
		} else {
			if (this.primary) {
				onPrimaryUp?.Invoke();
				this.primary = false;
			}
		}

		//handle possible secondary action events.
		if (secondary) {
			if (!this.secondary) {
				onSecondaryDown?.Invoke();
			}
			onSecondary?.Invoke();
			this.secondary = true;
		} else {
			if (this.secondary) {
				onSecondaryUp?.Invoke();
				this.secondary = false;
			}
		}

		//handle possible modifier action events.
		if (modifier) {
			if (!this.modifier) {
				onModifierDown?.Invoke();
			}
			onModifier?.Invoke();
			this.modifier = true;
		} else {
			if (this.modifier) {
				onModifierUp?.Invoke();
				this.modifier = false;
			}
		}
	}
}