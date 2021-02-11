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

	public Light arcLight;
	public VisualEffect arcEffect;
	public AudioSource arcEffectAudioSource;
	private bool arcEffectIsPlaying = false;
	public float minIntensity, maxIntensity;
	[Range(0, 1)]
	public float arcInstability;

	[Range(0, 1)]
	public float arcFadeRate;

	[Range(0, 1)]
	public float arcVolumeChangeRate;

	public Material weldMaterial;

	private static int weldCount = 0;
	private float cooldownRemaining;

	void Start() {
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
		var weld = new GameObject($"Weld {++weldCount}");
		weld.transform.position = position;
		var weldThing = weld.AddComponent<WeldThing>();
		cooldownRemaining = cooldown;
		weldThing.BuildWeld(weldMaterial, weldSize, compressionStrength, tensileStrength, shearStrength);
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
					arcEffectAudioSource.Play();
					arcEffectAudioSource.volume = 0;
					arcEffect.Play();
					arcEffectIsPlaying = true;
				}
				arcLight.intensity = Mathf.Lerp(arcLight.intensity, Random.Range(minIntensity, maxIntensity), arcInstability);
				arcEffectAudioSource.volume = Mathf.Lerp(arcEffectAudioSource.volume, 1, arcVolumeChangeRate);
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
				arcEffectAudioSource.volume = Mathf.Lerp(arcEffectAudioSource.volume, 0, arcVolumeChangeRate);
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
		arcEffectAudioSource.volume = Mathf.Lerp(arcEffectAudioSource.volume, 0, arcVolumeChangeRate);
		if (cooldownRemaining > 0) {
			cooldownRemaining -= Time.deltaTime;
			cooldownRemaining = Mathf.Max(0, cooldownRemaining);
		}
	}
}