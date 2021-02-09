using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutterBehaviour : Useable {

	void Start() {
		onSelected += Selected;
		onDeselected += Deselected;
		CanSwitchOut = true;
	}

	/// <summary>
	/// Logic for when this tool is selected from the tool list.
	/// </summary>
	private void Selected() {
		transform.GetChild(0).gameObject.SetActive(true);
	}

	/// <summary>
	/// Logic for when this tool is deselected from the tool list.
	/// </summary>
	private void Deselected() {
		transform.GetChild(0).gameObject.SetActive(false);
	}
}
