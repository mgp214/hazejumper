using UnityEngine;

namespace ShapeDrivers.Examples {
	public class MouseFollowControllerLinear : MonoBehaviour {
		float localZ;

		void Start(){
			localZ = transform.localPosition.z;
		}

		void Update(){
			transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f));
			transform.localPosition = new Vector3(Mathf.Clamp(transform.localPosition.x, -.27f, .4f), 0f, localZ);
		}
	}
}