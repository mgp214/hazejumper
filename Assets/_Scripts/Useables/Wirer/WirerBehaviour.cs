using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WirerBehaviour : Useable {
	public float range;
	public float currentPointDistance;
	public float pointDistanceDelta;
	public float deploySeconds;
	public GameObject nodeListUI;
	public GameObject nodeUITemplate;
	public ScrollRect scroll;
	public float wireRadius;
	public float wireSegmentLength;
	private GameObject nodeListUIParent;
	private IWireable wireable, previousWireable;
	private Material material;
	private List<NodeUIItemBehaviour> nodeUIList;

	[SerializeField]
	private NodeUIItemBehaviour selectedNodeUI;

	[SerializeField]
	private Node selectedNode;

	[SerializeField]
	private Node startNode;

	private List<GameObject> wireSegments;
	private Vector3 currentPoint;
	private Vector3 startPoint;

	private float deployedFraction;

	[SerializeField]
	private GameObject[] visibleWireSegments;

	public override bool SwitchIn() {
		transform.rotation = transform.parent.rotation * Quaternion.Euler(90 - (90 * deployedFraction / deploySeconds), 0, 0);
		if (deployedFraction == deploySeconds)
			return true;
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

	void Start() {
		onSelected += Selected;
		onDeselected += Deselected;
		onPrimaryDown += Wire;
		onSecondaryDown += CancelWire;
		nodeListUIParent = nodeListUI.transform.parent.parent.gameObject;
		startNode = Node.Empty;
		selectedNode = null;
		material = transform.GetChild(0).GetComponent<MeshRenderer>().material;
		CanSwitchOut = true;
	}

	private void AddSegment(Vector3 point) {
		var segmentHull = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		var segment = new GameObject("wire segment");
		segment.layer = LayerMask.NameToLayer("Wires");
		segment.transform.position = point;
		var lastPoint = wireSegments.Count > 0 ? wireSegments.Last().transform.position : startPoint;
		segment.transform.LookAt(lastPoint);
		segmentHull.transform.SetParent(segment.transform);
		segmentHull.transform.localRotation = Quaternion.Euler(90, 0, 0);
		segmentHull.transform.localPosition = new Vector3(0, 0, -wireSegmentLength / 2f);
		segmentHull.transform.localScale = new Vector3(wireRadius, wireSegmentLength / 2f, wireRadius);

		var segmentRigidbody = segment.AddComponent<Rigidbody>();
		segmentRigidbody.useGravity = false;
		segmentRigidbody.mass = 0.1f;
		Rigidbody targetBody;
		if (wireSegments.Count > 0) {
			targetBody = wireSegments.Last().GetComponent<Rigidbody>();
		} else {
			targetBody = startNode.owner.parentChunk.rigidbody;
		}
		var joint = segment.AddComponent<ConfigurableJoint>();
		SetWireJoint(joint);
		joint.connectedBody = targetBody;
		joint.anchor = new Vector3(0, 0, -wireSegmentLength);

		wireSegments.Add(segment);
	}

	private void EndWire(Vector3 point, Rigidbody target) {
		AddSegment(point);
		var lastSegment = wireSegments.Last();
		var lastSegmentBody = lastSegment.GetComponent<Rigidbody>();
		var joint = lastSegment.AddComponent<ConfigurableJoint>();
		SetWireJoint(joint);
		joint.connectedBody = target;

		startPoint = Vector3.negativeInfinity;
	}

	private void SetWireJoint(ConfigurableJoint joint) {
		joint.autoConfigureConnectedAnchor = false;
		joint.connectedAnchor = Vector3.zero;
		joint.xMotion = ConfigurableJointMotion.Locked;
		joint.yMotion = ConfigurableJointMotion.Locked;
		joint.zMotion = ConfigurableJointMotion.Locked;
		var limit = joint.linearLimit;
		limit.limit = 0f;
		joint.linearLimit = limit;
		joint.angularXMotion = ConfigurableJointMotion.Free;
		joint.angularYMotion = ConfigurableJointMotion.Free;
		joint.angularZMotion = ConfigurableJointMotion.Free;
	}

	private void Wire() {
		if (startNode == Node.Empty) {
			//set this node as our starting point
			if (selectedNode != null) {
				startNode = selectedNode;
				wireSegments = new List<GameObject>();
				//wirePoints = new List<Vector3>();
				//wirePoints.Add(currentPoint);
				selectedNode = Node.Empty;
			}
		} else {
			if (ValidateWire()) {
				var wire = startNode.BuildWire(startNode, selectedNode, wireSegments[0]);
				EndWire(currentPoint, selectedNode.owner.parentChunk.rigidbody);
				//wirePoints = null;
				startNode = Node.Empty;
			} else {
				InvalidConnection();
				//wirePoints.Add(currentPoint);
				return;
			}
		}
	}

	private bool ValidateWire() {
		var result = false;
		if (startNode != Node.Empty && selectedNode != Node.Empty) {
			result = startNode.GetDirection() != selectedNode.GetDirection()
				&& startNode.GetValueType() == selectedNode.GetValueType();
			material.color = result ? Color.green : Color.red;
		} else {
			material.color = Color.white;
		}

		return result;
	}

	private void InvalidConnection() {
		//Debug.LogWarning($"Invalid: [{startNode.owner.name}.{startNode.name}] and [{selectedNode.owner.name}.{selectedNode.name}]");
	}

	private void CancelWire() {
		//wirePoints = null;
		startNode = Node.Empty;
		startPoint = Vector3.negativeInfinity;
	}

	private void Update() {
		visibleWireSegments = wireSegments?.ToArray() ?? new GameObject[0];
		if (PlayerBehaviour.Instance.ActiveUseable != this) {
			if (nodeListUIParent.activeSelf)
				nodeListUIParent.SetActive(false);
			return;
		}
		ValidateWire();
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, range, Chunk.GetLayerMask(Chunk.SpaceState.Normal))) {
			var thing = Thing.GetThing(hit.collider);
			startPoint = hit.point;
			currentPoint = hit.point + (hit.normal * wireRadius);
			if (thing is IWireable) {
				if (thing as IWireable != wireable) {
					wireable = thing as IWireable;
				}
			} else {
				wireable = null;
			}
		} else {
			currentPoint = ray.origin + ray.direction * currentPointDistance;
			wireable = null;
		}
		if (wireable != null) {
			if (!nodeListUIParent.activeSelf)
				nodeListUIParent.SetActive(true);
			if (wireable != previousWireable) {
				ClearNodeUI();
				nodeUIList = new List<NodeUIItemBehaviour>();
				foreach (var node in wireable.GetNodes()) {
					var nodeUI = Instantiate(nodeUITemplate);
					nodeUI.name = $"{node.name} UI";
					var nodeBehaviour = nodeUI.GetComponent<NodeUIItemBehaviour>();
					nodeUIList.Add(nodeBehaviour);
					nodeBehaviour.Node = node;
					nodeUI.transform.SetParent(nodeListUI.transform);
				}
				if (!nodeUIList.Exists(ui => ui.Node == selectedNode)) {
					selectedNodeUI = nodeUIList[0];
				} else {
					selectedNodeUI = nodeUIList.Find(n => n.Node == selectedNode);
				}
				selectedNodeUI.Selected = true;
				selectedNode = selectedNodeUI.Node;
			}
			if (Input.GetButton("Action Modifier")) {
				var index = nodeUIList.IndexOf(selectedNodeUI);
				if (Input.GetAxis("Switch Equipped") < 0) {
					if (index + 1 == nodeUIList.Count) {
						index = 0;
					} else {
						index++;
					}
				}
				if (Input.GetAxis("Switch Equipped") > 0) {
					if (index == 0) {
						index = nodeUIList.Count - 1;
					} else {
						index--;
					}
				}
				if (index != nodeUIList.IndexOf(selectedNodeUI)) {
					selectedNodeUI.Selected = false;
					selectedNodeUI = nodeUIList[index];
					selectedNodeUI.Selected = true;
					selectedNode = selectedNodeUI.Node;
					scroll.content.localPosition = scroll.GetSnapToPositionToBringChildIntoView(selectedNodeUI.GetComponent<RectTransform>());
				}
			}
		} else {
			if (Input.GetButton("Action Modifier")) {
				currentPointDistance = Mathf.Clamp(currentPointDistance - Input.GetAxis("Switch Equipped") * pointDistanceDelta * Time.deltaTime, 0.2f, range);
			}
			selectedNode = Node.Empty;
			if (previousWireable != null) {
				ClearNodeUI();
				nodeListUIParent.SetActive(false);
			}
		}
		if (startNode != Node.Empty) {
			var lastPoint = wireSegments.Count > 0 ? wireSegments.Last().transform.position : startPoint;
			if ((currentPoint - lastPoint).magnitude >= wireSegmentLength) {
				var endPoint = currentPoint + (lastPoint - currentPoint).normalized * wireSegmentLength;
				AddSegment(endPoint);
				currentPoint = endPoint;
			}
			//	if (currentPointPreview == null) {
			//		currentPointPreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			//		currentPointPreview.layer = Chunk.GetLayer(Chunk.SpaceState.NormalPreview);
			//		currentPointPreview.transform.localScale = Vector3.one * 0.1f;
			//	}
			//	currentPointPreview.transform.position = currentPoint;
			//} else {
			//	if (currentPointPreview)
			//		Destroy(currentPointPreview);
		}
		previousWireable = wireable;
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.green;
		//if (wirePoints != null)
		//for (int i = 0; i < wirePoints.Count; i++) {
		//var point = wirePoints[i];
		//if (i > 0) {
		//Gizmos.DrawLine(wirePoints[i - 1], point);
		//}
		Gizmos.DrawSphere(currentPoint, wireRadius);
		//}
	}

	private void ClearNodeUI() {
		for (int i = 1; i < nodeListUI.transform.childCount; i++) {
			Destroy(nodeListUI.transform.GetChild(i).gameObject);
		}
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