using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace ShapeDrivers {
	[CustomEditor(typeof(ShapeDriverInspector))]
	public class ShapeDriverInspectorCInspector : Editor {
		SkinnedMeshRenderer skinnedMeshRenderer;
		ShapeDriver[] drivers;
		Dictionary<GameObject, List<ShapeDriver>> driverAmounts;
		string curShapeName;
		
		public override void OnInspectorGUI(){
			skinnedMeshRenderer = ((ShapeDriverInspector)target).GetComponent<SkinnedMeshRenderer>();

			if(skinnedMeshRenderer == null){
				EditorGUILayout.HelpBox("There is no SkinnedMeshRenderer attached to this object. No ShapeDrivers to display.", MessageType.Warning);
				return;
			}

			driverAmounts = new Dictionary<GameObject, List<ShapeDriver>>();
			drivers = GameObject.FindObjectsOfType<ShapeDriver>().Where(sd => sd.SkinnedMeshRenderer == skinnedMeshRenderer).ToArray();

			foreach(ShapeDriver driver in drivers){
				if(driverAmounts.ContainsKey(driver.gameObject)){
					driverAmounts[driver.gameObject].Add(driver);
				}
				else{
					driverAmounts.Add(driver.gameObject, new List<ShapeDriver>(){driver});
				}
			}

			for(int i=0; i<skinnedMeshRenderer.sharedMesh.blendShapeCount; i++){
				curShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
				EditorGUILayout.LabelField(curShapeName, EditorStyles.helpBox);
				foreach(KeyValuePair<GameObject, List<ShapeDriver>> driverAmount in driverAmounts){
					int active = 0;
					int total = 0;

					foreach(ShapeDriver driver in driverAmount.Value){
						if(driver.Shape == curShapeName){
							total++;
							if(driver.enabled) active++;
						}
					}
					if(total > 0){
						GUILayout.BeginHorizontal();
						GUILayout.Space(20);
						GUI.enabled = driverAmount.Value.Any(sd => sd.Shape == curShapeName && sd.enabled);
						EditorGUILayout.LabelField(driverAmount.Key.name+" ("+active+"/"+total+")", GUILayout.Width(Screen.width*0.5f - 40));
						GUI.enabled = true;
						if(GUILayout.Button("Select")){
							Selection.activeGameObject = driverAmount.Key.gameObject;
						}
						GUILayout.EndHorizontal();
					}
				}
			}
		}
	}
}