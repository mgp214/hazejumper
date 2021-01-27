using UnityEngine;

namespace ShapeDrivers.Examples {
	public class MouseFollowControllerFree : MonoBehaviour {
		float worldZ;
		
		void Start(){
			worldZ = transform.position.z;
		}

		void Update(){
			Vector3 cursorWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f));
			transform.position = new Vector3(cursorWorld.x, cursorWorld.y, worldZ);
		}
	}
}