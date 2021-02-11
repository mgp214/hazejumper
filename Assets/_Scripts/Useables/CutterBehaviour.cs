using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutterBehaviour : Useable {

	public LayerMask cuttableLayers;
	public float range;
	public float maxCutThickness;
	public float maxLayerRetries;

	void Start() {
		onPrimary += () => OnPrimary(false);
		onPrimaryUp += () => OnPrimary(true);
		CanSwitchOut = true;
	}

	void OnPrimary(bool up) {
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray.origin, ray.direction, out var hit, range, cuttableLayers)) {
			var reverseCutStartPoint = hit.point + ray.direction.normalized * maxCutThickness;
			if (Physics.Raycast(reverseCutStartPoint, -ray.direction, out var reverseHit, maxCutThickness, cuttableLayers)) {
				var retries = 0;
				while (hit.collider.transform.parent != reverseHit.collider.transform.parent) {
					retries++;
					if (retries > maxLayerRetries) return;
					Physics.Raycast(reverseHit.point - (ray.direction.normalized * 0.001f), -ray.direction, out reverseHit, maxCutThickness, cuttableLayers);
				}
				if (up) {
					Debug.Log($"{(hit.point - reverseHit.point).magnitude} after {retries} layer retries.");
				}
				Debug.DrawLine(hit.point, reverseHit.point, Color.green, up ? 2 : Time.deltaTime);
			} else {
				Debug.DrawLine(hit.point, reverseCutStartPoint, Color.red, up ? 2 : Time.deltaTime);
			}
		}
	}
}
