using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HandDataController : MonoBehaviour {
	[SerializeField]
	public Transform pinky0, pinky1, pinky2;
	[SerializeField]
	public Transform ring0, ring1, ring2;
	[SerializeField]
	public Transform middle0, middle1, middle2;
	[SerializeField]
	public Transform index0, index1, index2;
	[SerializeField]
	public Transform thumb0, thumb1, thumb2;

	[ContextMenuItem("Save current", "Capture")]
	[ContextMenuItem("Apply to hand", "Apply")]
	[SerializeField]
	public HandData handData;
	public void Capture() {
		handData.pinky0 = pinky0.localRotation;
		handData.pinky1 = pinky1.localRotation;
		handData.pinky2 = pinky2.localRotation;

		handData.ring0 = ring0.localRotation;
		handData.ring1 = ring1.localRotation;
		handData.ring2 = ring2.localRotation;

		handData.middle0 = middle0.localRotation;
		handData.middle1 = middle1.localRotation;
		handData.middle2 = middle2.localRotation;

		handData.index0 = index0.localRotation;
		handData.index1 = index1.localRotation;
		handData.index2 = index2.localRotation;

		handData.thumb0 = thumb0.localRotation;
		handData.thumb1 = thumb1.localRotation;
		handData.thumb2 = thumb2.localRotation;

		EditorUtility.SetDirty(handData);
		AssetDatabase.SaveAssets();
	}

	public void Apply() {
		pinky0.localRotation = handData.pinky0;
		pinky1.localRotation = handData.pinky1;
		pinky2.localRotation = handData.pinky2;

		ring0.localRotation = handData.ring0;
		ring1.localRotation = handData.ring1;
		ring2.localRotation = handData.ring2;

		middle0.localRotation = handData.middle0;
		middle1.localRotation = handData.middle1;
		middle2.localRotation = handData.middle2;

		index0.localRotation = handData.index0;
		index1.localRotation = handData.index1;
		index2.localRotation = handData.index2;

		thumb0.localRotation = handData.thumb0;
		thumb1.localRotation = handData.thumb1;
		thumb2.localRotation = handData.thumb2;
	}
}