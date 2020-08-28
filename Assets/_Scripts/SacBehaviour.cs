using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Chunk;

public class SacBehaviour : MonoBehaviour {
	public float size;
	public float viewSize;
	public float rotateSpeed;

	[Range(0, 1)]
	public float linearTransferRatio;

	public RawImage sacView;
	public float zoomCoefficient;
	public GameObject player, cameraObj;
	private static SacBehaviour _Instance;

	private float screenWidth, screenHeight;
	private new Rigidbody rigidbody;
	public new Camera camera;
	private List<Rigidbody> contents;

	public static SacBehaviour Instance {
		get {
			return _Instance;
		}
		private set {
			if (_Instance != null)
				throw new System.Exception("Cannot have more than one SacManager!");
			_Instance = value;
		}
	}

	public bool IsMaximized { get; private set; }

	public void AddContents(Rigidbody item) {
		if (!contents.Contains(item)) {
			contents.Add(item);
		}
	}

	public void RemoveContents(Rigidbody item) {
		if (contents.Contains(item)) {
			contents.Remove(item);
		}
	}

	private void Start() {
		Instance = this;
		rigidbody = GetComponent<Rigidbody>();
		contents = new List<Rigidbody>();
		size = 2;
		IsMaximized = false;
		sacView.rectTransform.sizeDelta = new Vector2(viewSize, viewSize);
		sacView.rectTransform.anchoredPosition = new Vector2((Screen.width - viewSize) / 2f, -(Screen.height - viewSize) / 2f);
	}

	private void ToggleCursor(bool locked) {
		if (locked) {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		} else {
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			Cursor.lockState = CursorLockMode.Confined;
			if (SubspacerBehaviour.Instance.Chunk != null) {
				SubspacerBehaviour.Instance.SwitchSpace();
			}
		}
	}

	private void Maximize() {
		var minDimension = Mathf.Min(Screen.width, Screen.height);
		sacView.rectTransform.sizeDelta = new Vector2(minDimension, minDimension);
		screenWidth = Screen.width;
		screenHeight = Screen.height;
		sacView.rectTransform.anchoredPosition = Vector2.zero;
	}

	private void Update() {
		if (Input.GetButtonDown("Toggle SAC") && ((!PlayerBehaviour.Instance.IsBusy) || (PlayerBehaviour.Instance.ActiveUseable is SubspacerBehaviour))) {
			IsMaximized = !IsMaximized;
			PlayerCoordinator.Instance.interceptedMovementInput = IsMaximized;
			if (IsMaximized) {
				Maximize();
				ToggleCursor(false);
			} else {
				ToggleCursor(true);
				sacView.rectTransform.sizeDelta = new Vector2(viewSize, viewSize);
				sacView.rectTransform.anchoredPosition = new Vector2((Screen.width - viewSize) / 2f, -(Screen.height - viewSize) / 2f);
				if (SubspacerBehaviour.Instance.Chunk != null) {
					SubspacerBehaviour.Instance.SwitchSpace();
				}
			}
		}
		if (IsMaximized) {
			if (screenHeight != Screen.height || screenWidth != Screen.width) {
				Maximize();
			}
			LookUpdate();
			InputUpdate();
		}
	}

	private void FixedUpdate() {
		rigidbody.MovePosition(player.transform.position);
		rigidbody.MoveRotation(player.transform.rotation);
		foreach (var item in contents) {
			item.velocity = Vector3.Lerp(item.velocity, rigidbody.velocity, linearTransferRatio);
			if ((item.position - transform.position).magnitude > size * 1.5f) {
				Debug.LogError($"{item.name} has breached the Sac!");
				item.position = rigidbody.position;
			}
		}
		if (transform.localScale.x != size) {
			transform.localScale = Vector3.one * size;
			gameObject.GetComponentInChildren<Light>().range = size * 5f;
		}
	}

	void LookUpdate() {
		if (!Input.GetButton("Action Modifier")) {
			var x = Input.GetAxis("Forward / Backward");
			var y = -Input.GetAxis("Right / Left");
			var z = -Input.GetAxis("Roll");
			var rotateVector = new Vector3(x, y, z) * rotateSpeed * Time.deltaTime;
			cameraObj.transform.RotateAround(transform.position, cameraObj.transform.right, rotateVector.x);
			cameraObj.transform.RotateAround(transform.position, cameraObj.transform.up, rotateVector.y);
			cameraObj.transform.RotateAround(transform.position, cameraObj.transform.forward, rotateVector.z);
		}
	}

	void InputUpdate() {
		var primaryDown = Input.GetButtonDown("Primary Action");
		if (SubspacerBehaviour.Instance.Chunk != null) {
			var horizontal = Input.GetAxis("Yaw");
			var vertical = -Input.GetAxis("Pitch");
			var zoom = Input.GetAxis("Switch Equipped") * zoomCoefficient;
			var offset = new Vector3(horizontal, -vertical, zoom) * SubspacerBehaviour.Instance.force * Time.deltaTime * size;
			SubspacerBehaviour.Instance.Offset(offset);

			//handle rotation logic
			if (Input.GetButton("Action Modifier")) {
				var x = Input.GetAxis("Forward / Backward");
				var y = -Input.GetAxis("Right / Left");
				var z = Input.GetAxis("Roll");
				var rotateVector = new Vector3(x, y, z) * rotateSpeed * Time.deltaTime;
				SubspacerBehaviour.Instance.Chunk.transform.Rotate(cameraObj.transform.forward, z, Space.World);
				SubspacerBehaviour.Instance.Chunk.transform.Rotate(cameraObj.transform.right, x, Space.World);
				SubspacerBehaviour.Instance.Chunk.transform.Rotate(cameraObj.transform.up, y, Space.World);
				SubspacerBehaviour.Instance.Chunk.transform.Translate(cameraObj.transform.forward * zoom, Space.World);
			}
			if (primaryDown && SubspacerBehaviour.Instance.IsEligible) {
				ToggleCursor(false);
				SubspacerBehaviour.Instance.DropChunk(SpaceState.Subspace);
			}
			return;
		}

		if (SubspacerBehaviour.Instance.Chunk == null) {
			var localPoint = (Input.mousePosition - sacView.rectTransform.position + ((Vector3)sacView.rectTransform.sizeDelta / 2f));
			localPoint = new Vector3(
				localPoint.x / (sacView.rectTransform.sizeDelta.x),
				localPoint.y / (sacView.rectTransform.sizeDelta.y)
			);
			if (RectTransformUtility.RectangleContainsScreenPoint(sacView.rectTransform, Input.mousePosition)) {
				localPoint = Vector3.Scale(localPoint, new Vector3(camera.targetTexture.width, camera.targetTexture.height));
				Ray ray = camera.ScreenPointToRay(localPoint);
				Debug.DrawRay(ray.origin, ray.direction * size * 2f, Color.cyan);
				if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 100, LayerMask.GetMask("Subspace"))) {
					if (primaryDown) {
						SubspacerBehaviour.Instance.GrabChunk(GetChunk(hit.collider));
						ToggleCursor(true);
					}
				}
			}
		}
	}
}