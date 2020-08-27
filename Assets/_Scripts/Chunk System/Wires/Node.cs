using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node : object {
	public static readonly string DIRECTION_IN = "IN";
	public static readonly string DIRECTION_OUT = "OUT";
	public List<Wire> wires;

	public string name;

	public string description;
	public Thing owner;

	public static Node Empty {
		get;
	} = new Node() { name = "Empty", description = "This serves as null." };

	public enum ValueType {
		Rational,
		Integer,
		Text
	}

	public virtual string GetDirection() {
		return "none";
	}

	public virtual string GetValueType() {
		return "none";
	}

	public virtual Wire BuildWire(Node a, Node b, GameObject wireObject) {
		return AddWire<Wire>(a, b, wireObject);
	}

	protected Wire AddWire<T>(Node a, Node b, GameObject wireObject) where T : Wire {
		var wireName = a.GetDirection() == Node.DIRECTION_OUT
			? $"{a.owner.name}.{a.name} -> {b.owner.name}.{b.name}"
			: $"{b.owner.name}.{b.name} -> {a.owner.name}.{a.name}";
		//var wireObj = new GameObject(wireName);
		//wireObj.layer = Chunk.GetLayer(Chunk.SpaceState.Normal);
		//var wireMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		//wireMesh.transform.parent = wireObj.transform;

		var wire = wireObject.AddComponent<T>();
		//wire.mass = 0.001f;
		//wire.Initialize();
		wire.inNode = a.GetDirection() == Node.DIRECTION_IN
			? a : b;
		wire.outNode = a.GetDirection() == Node.DIRECTION_OUT
			? a : b;
		var connectionVector = a.owner.transform.position - b.owner.transform.position;
		//wireObj.transform.position = Vector3.Lerp(a.owner.transform.position, b.owner.transform.position, 0.5f);
		//wireObj.transform.LookAt(a.owner.transform);
		//wireObj.transform.rotation *= Quaternion.Euler(90, 0, 0);
		//wireMesh.transform.localScale = new Vector3(0.05f, connectionVector.magnitude / 2f, 0.05f);
		a.wires.Add(wire);
		b.wires.Add(wire);
		return wire;
	}
}