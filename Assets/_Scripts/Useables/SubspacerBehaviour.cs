using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubspacerBehaviour : Useable {
	public float deploySeconds;
	public float range;
	public float force;

	private static SubspacerBehaviour _Instance;
	private float deployedFraction;

	[SerializeField]
	private Chunk _Chunk;

	private Vector3 offset;

	public Transform player;

	[SerializeField]
	private bool _IsEligible;

	public static SubspacerBehaviour Instance { get => _Instance; }
	public bool IsEligible { get => _IsEligible; private set => _IsEligible = value; }

	public Chunk Chunk { get => _Chunk; }

	public override bool SwitchIn() {
		transform.rotation = transform.parent.rotation * Quaternion.Euler(90 - (90 * deployedFraction / deploySeconds), 0, 0);

		deployedFraction += Time.deltaTime;

		if (deployedFraction >= deploySeconds) {
			deployedFraction = deploySeconds;
			idleAvailable = true;
			return true;
		} else {
			idleAvailable = false;
		}
		return false;
	}

	public override bool SwitchOut() {
		idleAvailable = false;
		transform.rotation = transform.parent.rotation * Quaternion.Euler(90 - (90 * deployedFraction / deploySeconds), 0, 0);

		deployedFraction -= Time.deltaTime;

		if (deployedFraction <= 0) {
			deployedFraction = 0;
			return true;
		}
		return false;
	}

	public void GrabChunk(Chunk chunk) {
		this._Chunk = chunk;
		CanSwitchOut = false;
		if (PlayerBehaviour.Instance.ActiveUseable != this) {
			PlayerBehaviour.Instance.SetAsActive(this);
		}
		if (chunk.State == Chunk.SpaceState.Normal) {
			chunk.ChangeSpaceState(Chunk.SpaceState.NormalPreview);
			offset = player.InverseTransformDirection(chunk.transform.position - player.position);
		} else {
			chunk.ChangeSpaceState(Chunk.SpaceState.SubspacePreview);
			offset = SacBehaviour.Instance.cameraObj.transform.InverseTransformDirection(chunk.transform.position - player.position);
		}
	}

	public void Offset(Vector3 vector) {
		var oldOffset = offset;
		offset += vector;
		CalculatePosition();
		while (Chunk.CheckCollisionWith(Chunk.SpaceState.SacWalls)) {
			if (vector.magnitude < 0.01f) {
				offset = Vector3.Lerp(offset, Vector3.zero, 0.01f);
			} else {
				offset = oldOffset;
				break;
			}
			if (offset.magnitude < 0.01f)
				break;
			CalculatePosition();
		}
	}

	public void DropChunk(Chunk.SpaceState newSpace) {
		CanSwitchOut = true;
		_Chunk.ChangeSpaceState(newSpace);
		_Chunk = null;
	}

	public void SwitchSpace() {
		if (Chunk.State == Chunk.SpaceState.NormalPreview) {
			offset = Vector3.zero;
			Chunk.ChangeSpaceState(Chunk.SpaceState.SubspacePreview);
		} else {
			offset = Vector3.forward * 2;
			Chunk.ChangeSpaceState(Chunk.SpaceState.NormalPreview);
		}
	}

	private void Start() {
		onSelected += Selected;
		onDeselected += Deselected;
		onPrimaryDown += Grab;
		if (_Instance != null)
			throw new System.Exception("Cannot instantiate more than one Subspacer!");
		_Instance = this;
	}

	private void Grab() {
		if (_Chunk == null) {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, range, LayerMask.GetMask("Chunks"))) {
				GrabChunk(Chunk.GetChunk(hit.collider));
			}
		} else if (IsEligible) {
			DropChunk(Chunk.SpaceState.Normal);
		}
	}

	private void CalculatePosition() {
		if (_Chunk != null) {
			switch (_Chunk.State) {
				case Chunk.SpaceState.NormalPreview:
					IsEligible = !_Chunk.CheckCollisionWith(Chunk.SpaceState.Normal);
					_Chunk.transform.position = player.position + player.TransformDirection(offset);
					break;

				case Chunk.SpaceState.SubspacePreview:
					IsEligible = !_Chunk.CheckCollisionWith(Chunk.SpaceState.Subspace, Chunk.SpaceState.SacWalls);
					_Chunk.transform.position = player.position + SacBehaviour.Instance.cameraObj.transform.TransformDirection(offset);
					break;
			}
		}
	}

	private void Update() {
		CalculatePosition();
	}

	/// <summary>
	/// Logic for when this tool is selected from the tool list.
	/// </summary>
	private void Selected() {
		transform.GetChild(0).gameObject.SetActive(true);
	}

	/// <summary>
	/// Logic for when this tool is deselected from the tool list.
	/// </summary>
	private void Deselected() {
		transform.GetChild(0).gameObject.SetActive(false);
	}
}