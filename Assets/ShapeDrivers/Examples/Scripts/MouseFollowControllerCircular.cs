using UnityEngine;

namespace ShapeDrivers.Examples {
	public class MouseFollowControllerCircular : MonoBehaviour {
		public float maxRange = 0.31f;

		float worldZ;
		
		void Start(){
			worldZ = transform.position.z;
		}

		void Update(){
			Vector3 cursorWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f));
			Vector3 targetPos = transform.parent.position + Vector3.ClampMagnitude(cursorWorld - transform.parent.position, maxRange);
			transform.position = new Vector3(targetPos.x, targetPos.y, worldZ);
		}
	}
}