using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatNode : Node {

	public override Wire BuildWire(Node a, Node b, GameObject wireObject) {
		return AddWire<FloatWire>(a, b, wireObject);
	}
}