using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButtonBehaviour : Thing, IInteractable, IWireable {
	public float value1, value2;

	[SerializeField]
	private float value;

	[SerializeField]
#pragma warning disable CS0649
	OutFloatNode output;

#pragma warning restore CS0649

	public List<Node> GetNodes() {
		var nodes = new List<Node>();
		nodes.Add(output);
		return nodes;
	}

	public string GetText() {
		return $"Toggle button: {value:0.#}";
	}

	public void Interact() {
		if (value == value1) {
			value = value2;
		} else {
			value = value1;
		}
		output.Output(value);
	}
}