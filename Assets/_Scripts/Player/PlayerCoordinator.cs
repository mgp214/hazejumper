using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCoordinator : MonoBehaviour {
	public bool interceptedMovementInput = false;
	public new Camera camera;

	private static PlayerCoordinator _Instance;
	public static PlayerCoordinator Instance => _Instance;

	void Start() {
		_Instance = this;
	}
}
