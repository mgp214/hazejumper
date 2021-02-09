using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hand Data", fileName = "new hand data.asset")]
public class HandData : ScriptableObject {
	public Quaternion pinky0, pinky1, pinky2;
	public Quaternion ring0, ring1, ring2;
	public Quaternion middle0, middle1, middle2;
	public Quaternion index0, index1, index2;
	public Quaternion thumb0, thumb1, thumb2;
}
