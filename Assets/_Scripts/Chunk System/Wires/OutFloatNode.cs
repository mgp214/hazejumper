using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OutFloatNode : FloatNode {

	public override string GetDirection() {
		return DIRECTION_OUT;
	}

	public override string GetValueType() {
		return ValueType.Rational.ToString();
	}

	public void Output(float output) {
		for (int i = 0; i < wires.Count; i++) {
			var wire = wires[i];
			if (wire == null)
				wires.RemoveAt(i--);
			if (wire.inNode != null) {
				(wire.inNode as InFloatNode).value = output;
				//wire.inNode.OnInput(output);
			}
		}
	}
}