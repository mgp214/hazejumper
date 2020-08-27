using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeldThing : Thing {

	/// <summary>
	/// How much force this weld can take before breaking.
	/// </summary>
	public float compressionStrength, tensileStrength, shearStrength;

	public virtual void Break() {
		var disconnectedThings = new List<Thing>(connectedThings);
		while (connectedThings.Count > 0) {
			connectedThings[0].Disconnect(this);
			connectedThings.RemoveAt(0);
		}
		var thingsToMakeChunksFrom = new List<Thing>();
		var thingsScanned = new List<Thing>();
		foreach (var thing in disconnectedThings) {
			var newlyScannedThings = thing.GetConnectedThings(new List<Thing>());
			if (!thingsScanned.Exists(t => newlyScannedThings.Contains(t))) {
				thingsToMakeChunksFrom.Add(thing);
			}
			thingsScanned.AddRange(newlyScannedThings);
		}
		foreach (var thing in thingsToMakeChunksFrom) {
			if (thing != this) {
				Chunk.Create(thing);
			}
		}
		Destroy(gameObject);
	}
}