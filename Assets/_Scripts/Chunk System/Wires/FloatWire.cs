using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatWire : Wire {

	public override void Break() {
		(inNode as InFloatNode).value = 0f;
		inNode.wires.Remove(this);
		outNode.wires.Remove(this);
	}
}