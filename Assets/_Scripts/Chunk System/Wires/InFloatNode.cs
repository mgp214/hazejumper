using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InFloatNode : FloatNode {
	public float value;

	public override string GetDirection() {
		return DIRECTION_IN;
	}

	public override string GetValueType() {
		return ValueType.Rational.ToString();
	}
}