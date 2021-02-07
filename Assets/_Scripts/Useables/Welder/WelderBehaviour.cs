using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public class WelderBehaviour : Useable {
	public float range;
	public float weldSize;
	public float weldBackoff;
	public float weldMinDistance;
	public float cooldown;
	public float compressionStrength, tensileStrength, shearStrength;
	public float deploySeconds;

	public Light arcLight;
	public VisualEffect arcEffect;
	private bool arcEffectIsPlaying = false;
	public float minIntensity, maxIntensity;
	[Range(0, 1)]
	public float arcInstability;

	[Range(0, 1)]
	public float arcFadeRate;

	public Material weldMaterial;

	private static int weldCount = 0;
	private float deployedFraction;
	private float cooldownRemaining;

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
		onPrimary += Weld;
		onPrimaryUp += () => {
			arcEffectIsPlaying = false;
			arcEffect.Stop();
		};
		onIdle += Idle;
		arcEffect.Stop();
		CanSwitchOut = true;
	}

	private GameObject CreateWeld(Vector3 position) {
		// var weld = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		var weld = new GameObject($"Weld {++weldCount}");
		weld.transform.position = position;
		// weld.name = "Weld " + ++weldCount;
		// weld.layer = LayerMask.NameToLayer("Chunks");
		var weldThing = weld.AddComponent<WeldThing>();
		// weldThing.mass = 0.01f;
		cooldownRemaining = cooldown;
		weldThing.BuildWeld(weldMaterial, weldSize, compressionStrength, tensileStrength, shearStrength);
		// weldThing.Initialize();
		// weldThing.compressionStrength = compressionStrength;
		// weldThing.tensileStrength = tensileStrength;
		// weldThing.shearStrength = shearStrength;
		// var hits = Physics.OverlapSphere(weld.transform.position, weld.transform.localScale.x, LayerMask.GetMask("Chunks"));
		// var things = hits.Where(c => !(Thing.GetThing(c) is WeldThing)).Distinct();
		// foreach (var hit in things) {
		// 	var thing = Thing.GetThing(hit);
		// 	thing.Connect(weldThing, hit.ClosestPoint(weldThing.transform.position) - weldThing.transform.position);
		// }

		return weld;
	}

	private void Weld() {
		//make sure we're cooled down
		if (cooldownRemaining == 0) {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, range, LayerMask.GetMask("Chunks"))) {

				var adjustedHitPoint = hit.point
					- (ray.direction.normalized * weldBackoff);

				if (!arcEffectIsPlaying) {
					arcEffect.Play();
					arcEffectIsPlaying = true;
				}
				arcLight.intensity = Mathf.Lerp(arcLight.intensity, Random.Range(minIntensity, maxIntensity), arcInstability);
				//don't create a weld if we're too close to an existing weld.
				var distance = Vector3.Distance(ray.origin, adjustedHitPoint);
				var nearbyThings = Physics.OverlapSphere(adjustedHitPoint, weldSize, LayerMask.GetMask("Chunks"));
				var nearbyWelds = nearbyThings.Where(c => Thing.GetThing(c) is WeldThing);
				var weldTooNear = false;
				if (Thing.GetThing(hit.collider) is WeldThing)
					weldTooNear = true;
				foreach (var w in nearbyWelds) {
					weldTooNear = weldTooNear || Vector3.Distance(w.transform.position, adjustedHitPoint) <= weldMinDistance;
					if (weldTooNear)
						break;
				}
				if (!weldTooNear) {
					CreateWeld(adjustedHitPoint);
				}
			} else if (arcEffectIsPlaying) {
				arcEffect.Stop();
				arcEffectIsPlaying = false;
			}

		} else {
			//otherwise, do some cooling
			cooldownRemaining -= Time.deltaTime;
			cooldownRemaining = Mathf.Max(0, cooldownRemaining);

		}
	}

	private void Idle() {
		arcLight.intensity = Mathf.Lerp(arcLight.intensity, 0, arcFadeRate);
		if (cooldownRemaining > 0) {
			cooldownRemaining -= Time.deltaTime;
			cooldownRemaining = Mathf.Max(0, cooldownRemaining);
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