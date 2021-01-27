using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace ShapeDrivers {
	[CustomEditor(typeof(ShapeDriver)), CanEditMultipleObjects]
	public class ShapeDriverCInspector : Editor {
		public static int curTermDrawIndex;
		public static ShapeDriver shapeDriver {get; private set;}
		public static readonly int minHalfWidth = 360;

		ReorderableList roDriverTerms;

		void OnEnable(){
			roDriverTerms = new ReorderableList(serializedObject, serializedObject.FindProperty("driverTerms"),
			                                    true, true, true, true);

			roDriverTerms.drawHeaderCallback += DrawHeader;
			roDriverTerms.drawElementCallback += DrawTerm;
			roDriverTerms.onAddCallback += AddTerm;
			roDriverTerms.onRemoveCallback += RemoveTerm;
		}

		void OnDisable(){
			roDriverTerms.drawHeaderCallback -= DrawHeader;
			roDriverTerms.drawElementCallback -= DrawTerm;
			roDriverTerms.onAddCallback -= AddTerm;
			roDriverTerms.onRemoveCallback -= RemoveTerm;
		}

		void DrawHeader(Rect rect){
			GUI.Label(rect, "Driver Terms");
		}

		void DrawTerm(Rect rect, int index, bool active, bool focused){
			curTermDrawIndex = index;
			EditorGUI.PropertyField(rect, serializedObject.FindProperty("driverTerms").GetArrayElementAtIndex(index));
		}

		void AddTerm(ReorderableList list){
			Undo.RecordObject(shapeDriver, "Add Driver Term");
			shapeDriver.AddDriverTerm();
			EditorUtility.SetDirty(target);
		}

		void RemoveTerm(ReorderableList list){
			Undo.RecordObject(shapeDriver, "Remove Driver Term");
			shapeDriver.RemoveDriverTerm(list.index);
			EditorUtility.SetDirty(target);
		}

		public override void OnInspectorGUI(){
			shapeDriver = target as ShapeDriver;

			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			SerializedProperty spSkinnedMeshRenderer = serializedObject.FindProperty("skinnedMeshRenderer");
			SerializedProperty spShape = serializedObject.FindProperty("shape");
			SerializedProperty spUseDefaultFeedback = serializedObject.FindProperty("useDefaultFallback");

			EditorGUILayout.PropertyField(spSkinnedMeshRenderer);

			SkinnedMeshRenderer smr = spSkinnedMeshRenderer.objectReferenceValue as SkinnedMeshRenderer;
			if(smr == null){
				GUI.enabled = false;
				EditorGUILayout.Popup(new GUIContent("Shape"), 0, new GUIContent[]{new GUIContent(spShape.stringValue)});
				GUI.enabled = true;
			}
			else{
				GUIContent[] blendShapes = new GUIContent[smr.sharedMesh.blendShapeCount];
				int selectedIndex = -1;
				int newIndex = -1;
				
				for(int i=0; i<smr.sharedMesh.blendShapeCount; i++){
					string shapeName = smr.sharedMesh.GetBlendShapeName(i);
					blendShapes[i] = new GUIContent(shapeName);
					if(shapeName == spShape.stringValue){
						selectedIndex = i;
					}
				}

				if(selectedIndex > -1){
					newIndex = EditorGUILayout.Popup(new GUIContent("Shape"), selectedIndex, blendShapes);
				}
				else{
					if(spShape.stringValue == ""){ //Never initialized yet; might as well set a default shape
						newIndex = 0;
					}
					else{
						newIndex = EditorGUILayout.Popup(new GUIContent("Shape"), selectedIndex, blendShapes);
						EditorGUILayout.HelpBox("Error: The specified shape name \""+spShape.stringValue+"\" does not exist on the targeted SkinnedMeshRender's mesh.", MessageType.Error);
					}
				}

				if(newIndex != selectedIndex){
					spShape.stringValue = blendShapes[newIndex].text;
				}
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("priority"));
			EditorGUILayout.PropertyField(spUseDefaultFeedback);

			if(spUseDefaultFeedback.boolValue){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultValue"));
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeBlendMode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("influence"));

			roDriverTerms.elementHeight = Screen.width < minHalfWidth ?
										  EditorGUIUtility.singleLineHeight * 11 + EditorGUIUtility.standardVerticalSpacing * 10 + 18 :
										  EditorGUIUtility.singleLineHeight * 7 + EditorGUIUtility.standardVerticalSpacing * 7 + 20;

			roDriverTerms.DoLayoutList();

			if(EditorGUI.EndChangeCheck()){
				serializedObject.ApplyModifiedProperties();
			}
		}

		public override bool RequiresConstantRepaint(){
			return Application.isPlaying;
		}
	}
}