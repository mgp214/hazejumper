using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Chunk;

/// <summary>
/// An atomic object that is encapsulated by a Chunk.
/// </summary>
public class Thing : MonoBehaviour {
	public float mass;
	public new Collider collider;

	public Chunk parentChunk;

	[SerializeField]
	protected List<Thing> connectedThings;

	private Dictionary<Thing, Vector3> connectionVectors;
	private List<Message> messageQueue;
	private bool initialized = false;

	public int Mask {
		get {
			return LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
		}
	}

	public static Thing GetThing(Collider collider) {
		var thing = collider.gameObject.GetComponent<Thing>();
		if (thing != null)
			return thing;
		thing = collider.gameObject.GetComponentInParent<Thing>();
		if (thing != null)
			return thing;
		return null;
	}

	public void Initialize() {
		if (collider == null)
			collider = GetComponent<Collider>();
		if (collider == null)
			collider = GetComponentInChildren<Collider>();
		messageQueue = new List<Message>();
		connectedThings = new List<Thing>();
		connectionVectors = new Dictionary<Thing, Vector3>();
		if (parentChunk == null) {
			parentChunk = Chunk.Create(this, Vector3.zero, Vector3.zero);
		}
		initialized = true;
	}

	public Chunk Connect(Thing thing, Vector3 connection) {
		if (connectedThings.Contains(thing)) {
			Debug.LogWarning($"{name} trying to add redundant connection to {thing.name}.");
			return parentChunk;
		}
		connectedThings.Add(thing);
		connectionVectors.Add(thing, connection);
		thing.connectedThings.Add(this);
		thing.connectionVectors.Add(this, -connection);
		if (thing.parentChunk != parentChunk) {
			//we're part of separate Chunks! Let's smoosh em together.
			return parentChunk.Merge(thing.parentChunk);
		} else {
			//we're already part of the same Chunk, just return that.
			return parentChunk;
		}
	}

	public void Disconnect(Thing thing) {
		connectedThings.Remove(thing);
	}

	/// <summary>
	/// Returns a depth-first search of all connected Things, self included. Does not retread covered ground.
	/// </summary>
	/// <param name="collection">The Things already visited.</param>
	public List<Thing> GetConnectedThings(List<Thing> collection) {
		//some other branch has already added this, just return.
		if (collection.Contains(this))
			return collection;
		collection.Add(this);
		var newThings = connectedThings.Where(t => !collection.Contains(t));

		foreach (var t in newThings) {
			t.GetConnectedThings(collection);
		}
		return collection;
	}

	public void QueueMessage(Message message) {
		messageQueue.Add(message);
	}

	protected void Start() {
		if (!initialized)
			Initialize();
		if (this is IWireable) {
			foreach (var node in (this as IWireable).GetNodes()) {
				node.owner = this;
			}
		}
	}

	private void Update() {
		var accumulatedForce = Vector3.zero;
		var forceSenders = new List<Thing>();
		//var announcedMessages = false;
		while (messageQueue.Count > 0) {
			//if (!announcedMessages) {
			//	Debug.Log($"{name} beginning msg processing...");
			//}
			var message = messageQueue[0];
			messageQueue.RemoveAt(0);
			if (message is ForceMessage) {
				var forceMsg = message as ForceMessage;
				if (this is WeldThing) {
					var weld = this as WeldThing;
					Vector3 connectionVector = transform.position;
					if (message.Sender is Thing) {
						connectionVector = connectionVectors[((Thing)message.Sender)];
					} else if (message.Sender is Vector3) {
						connectionVector = (Vector3)message.Sender - transform.position;
					}
					float compression = 0;
					float tension = 0;
					float shear = 0;
					if (Vector3.Dot(connectionVector, forceMsg.Force) > 0) {
						//compression
						compression = Vector3.Project(forceMsg.Force, connectionVector).magnitude;
					} else {
						//tension
						tension = Vector3.Project(forceMsg.Force, connectionVector).magnitude;
					}
					shear = forceMsg.Force.magnitude - compression - tension;
					//Debug.Log($"shr: {shear}, cmp: {compression}, tns: {tension}");
					if (shear > weld.shearStrength
						|| tension > weld.tensileStrength
						|| compression > weld.compressionStrength) {
						//Debug.Log($"{name} breaking!");
						weld.Break(accumulatedForce);
					} else {
						accumulatedForce += forceMsg.Force;
					}
				} else {
					accumulatedForce += forceMsg.Force;
				}
				if (message.Sender is Thing) {
					forceSenders.Add((Thing)message.Sender);
				}
			}
		}
		if (accumulatedForce.magnitude >= ChunkManager.Instance.forceMessageThreshold) {
			//Debug.Log($"{name}: cumulatedForce force: {cumulatedForce}[{cumulatedForce.magnitude}]");
			var recipients = connectedThings.Where(t => !forceSenders.Contains(t)).ToList();
			//if (recipients.Count > 0) {
			//	Debug.Log($"{name} sending {recipients.Count} forceMessages:");
			//} else {
			//	Debug.Log($"{name} at a dead end.");
			//}
			foreach (var thing in recipients) {
				var forceMessage = new ForceMessage(this, accumulatedForce * ChunkManager.Instance.forceTransmissionCoefficient / recipients.Count);
				//Debug.Log($"       {thing.name} -->  {forceMessage.Force}[{forceMessage.Force.magnitude}]");
				thing.QueueMessage(forceMessage);
			}
		}
	}
}