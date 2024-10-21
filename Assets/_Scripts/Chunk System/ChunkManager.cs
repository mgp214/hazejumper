using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour {
	public float forceMessageThreshold;
	public float forceTransmissionCoefficient;
	public float breakingWeldForceTransmissionCoefficient;

	public static ChunkManager Instance { get; private set; }

	private void Start() {
		if (Instance == null) {
			Instance = this;
		}
	}
}