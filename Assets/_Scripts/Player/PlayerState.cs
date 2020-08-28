using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour {
	public bool isViewingSac = false;

	private static PlayerState _Instance;
	public static PlayerState Instance => _Instance;

	void Start() {
		_Instance = this;
	}
}
