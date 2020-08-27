using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketBehaviour : Thing, IWireable {
#pragma warning disable CS0649

	[SerializeField]
	InFloatNode throttle;

	[SerializeField]
	OutFloatNode test;

	[SerializeField]
	OutFloatNode test2;

#pragma warning restore CS0649

	public List<Node> GetNodes() {
		var nodes = new List<Node>();
		nodes.Add(throttle);
		nodes.Add(test);
		nodes.Add(test2);
		return nodes;
	}

	private void Update() {
		if (throttle.value > 0) {
			parentChunk.rigidbody.AddForceAtPosition(transform.forward * throttle.value, transform.position);
		}
	}
}