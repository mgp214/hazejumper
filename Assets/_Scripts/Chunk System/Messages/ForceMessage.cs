using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceMessage : Message {
	public Vector3 Force { get; private set; }

	public ForceMessage(object sender, Vector3 force) {
		Force = force;
		this.Sender = sender;
	}
}