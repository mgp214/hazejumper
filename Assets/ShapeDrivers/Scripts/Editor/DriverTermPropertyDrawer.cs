using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ShapeDrivers {
	[CustomPropertyDrawer(typeof(DriverTerm))]
	public class DriverTermPropertyDrawer : PropertyDrawer {
		float x, y, w, slh, svs;
		float hw, hx;
		float lwSmall, lwBig, fcBtnSize, fcBtnSpacing;
		SerializedProperty spUseX, spUseY, spUseZ;
		bool drawRangeOption;
		GUIStyle s;
		List<string> warnings;
		List<string> errors;
		List<string> reflectionMethodsPopup;
		List<ReflectionMethodInfo> reflectionMethods;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			x = position.x;
			y = position.y;
			w = position.width;
			slh = EditorGUIUtility.singleLineHeight;
			svs = EditorGUIUtility.standardVerticalSpacing;
			
			lwSmall = 40f;
			lwBig = Mathf.Clamp(position.width*0.2f, 85f, position.width*0.45f);
			fcBtnSize = slh;
			fcBtnSpacing = 4f;
			hw = Screen.width >= ShapeDriverCInspector.minHalfWidth ? w*Mathf.Lerp(0.45f, 0.48f, (400-Screen.width)/(400f-ShapeDriverCInspector.minHalfWidth)) : w;
			hx = x + w*0.5f + (w-hw*2)*0.5f;
			s = new GUIStyle(GUI.skin.button);
			s.padding = new RectOffset(2,2,2,2);
			drawRangeOption = true;

			warnings = new List<string>();
			errors = new List<string>();
			
			EditorGUI.BeginProperty(position, label, property);
			
			EditorGUIUtility.labelWidth = lwSmall;

			SerializedProperty spType = property.FindPropertyRelative("type");
			SerializedProperty spSpace = property.FindPropertyRelative("space");
			SerializedProperty spChannel = property.FindPropertyRelative("channel");
			SerializedProperty spStartValue = property.FindPropertyRelative("startValue");
			SerializedProperty spEndValue = property.FindPropertyRelative("endValue");
			spUseX = property.FindPropertyRelative("useX");
			spUseY = property.FindPropertyRelative("useY");
			spUseZ = property.FindPropertyRelative("useZ");

			EditorGUI.PropertyField(new Rect(x, y, hw, slh), spType, new GUIContent("Type"));

			if(ShapeDriverCInspector.curTermDrawIndex > 0){
				TryDrawRightHalf(property.FindPropertyRelative("mode"), new GUIContent("Mode"));
			}

			EditorGUI.DrawRect(new Rect(x, y+slh*1.25f - 1, w, 1), Color.grey);

			y += slh * 1.5f;

			EditorGUIUtility.labelWidth = lwBig;

			if(spType.enumValueIndex == (int)DriverTermType.Position || spType.enumValueIndex == (int)DriverTermType.Rotation ||
			   spType.enumValueIndex == (int)DriverTermType.Scale){
				string typeName = ((DriverTermType)spType.enumValueIndex).ToString().ToLower();
				EditorGUI.PropertyField(new Rect(x, y, hw, slh), spChannel, new GUIContent("Channel"));
				EditorGUIUtility.labelWidth = lwBig;
				TryDrawRightHalf(spSpace, new GUIContent("Space"));

				if(spType.enumValueIndex == (int)DriverTermType.Rotation){
					y += slh + svs;
					EditorGUI.PropertyField(new Rect(x, y, hw, slh), property.FindPropertyRelative("rotationType"), new GUIContent("Rotation type"));
					TryDrawRightHalf(property.FindPropertyRelative("useForwardRotation"), new GUIContent("Force forward"));
				}
				y += slh + svs;
				DrawStartEndValueFields(spStartValue, spEndValue, "Start "+typeName, "End "+typeName);
			}
			else if(spType.enumValueIndex == (int)DriverTermType.DistanceToPoint){
				SerializedProperty spCompareVector = property.FindPropertyRelative("compareVector");
				DrawUseAxes(property);
				x = position.x;
				EditorGUIUtility.labelWidth = lwBig;
				TryDrawRightHalf(property.FindPropertyRelative("space"), new GUIContent("Space"));
				y += slh + svs;
				DrawStartEndValueFields(spStartValue, spEndValue, "Start distance", "End distance");
				y += slh + svs;
				EditorGUI.LabelField(new Rect(x, y, lwBig, slh), "Target"); //Prevent Vector3 line switch/ foldout by placing label independently and giving target an empty GUIContent
				EditorGUI.PropertyField(new Rect(x + lwBig, y, w - lwBig - fcBtnSize - fcBtnSpacing, slh), spCompareVector, new GUIContent(""));

				if(GUI.Button(new Rect(x + w - fcBtnSize, y, fcBtnSize, slh), new GUIContent(AssetPreview.GetMiniTypeThumbnail(typeof(Transform)), "Set from current Transform value"), s)){
					spCompareVector.vector3Value = ShapeDriverCInspector.shapeDriver.transform.position;
				}

				if(!spUseX.boolValue && !spUseY.boolValue && !spUseZ.boolValue){
					warnings.Add("[No axes selected. Value will always be 0.");
				}
			}
			else if(spType.enumValueIndex == (int)DriverTermType.DistanceToObject){
				DrawUseAxes(property);
				x = position.x;
				EditorGUIUtility.labelWidth = lwBig;
				TryDrawRightHalf(spSpace, new GUIContent("Space"));
				y += slh + svs;
				DrawStartEndValueFields(spStartValue, spEndValue, "Start distance", "End distance");
				y += slh + svs;
				EditorGUIUtility.labelWidth = lwBig;
				SerializedProperty spCompareTarget = property.FindPropertyRelative("compareTarget");
				EditorGUI.PropertyField(new Rect(x, y, w, slh), spCompareTarget, new GUIContent("Target"));

				if(!spUseX.boolValue && !spUseY.boolValue && !spUseZ.boolValue){
					warnings.Add("No axes selected. Value will always be 0.");
				}

				if(spCompareTarget.objectReferenceValue as Transform == Selection.activeTransform){
					warnings.Add("The selected compare target is this object's own transform. Value will always be 0.");
				}
			}
			else if(spType.enumValueIndex == (int)DriverTermType.MecanimParameter){
				SerializedProperty spMecParamType = property.FindPropertyRelative("mecParamType");
				SerializedProperty spMecParamName = property.FindPropertyRelative("animName");
				Animator animator = ShapeDriverCInspector.shapeDriver.GetComponent<Animator>();
				bool hasAnimator = animator != null;
				List<AnimatorControllerParameter> mecParams = new List<AnimatorControllerParameter>();
				int selectedParamIndex = -1;
				int newParamIndex = -1;

				EditorGUIUtility.labelWidth = lwBig;
				
				if(!hasAnimator){
					GUI.enabled = false;
					EditorGUI.Popup(new Rect(x, y, w, slh), new GUIContent("Parameter"), 0, new GUIContent[]{new GUIContent(spMecParamName.stringValue)});
					GUI.enabled = true;
					errors.Add("No Animator component found. If one is not added, errors will be generated at runtime.");
				}
				else{
					for(int i=0; i<animator.parameters.Length; i++){
						if(animator.parameters[i].type != AnimatorControllerParameterType.Trigger){
							mecParams.Add(animator.parameters[i]);
							if(animator.parameters[i].name == spMecParamName.stringValue){
								selectedParamIndex = i;
							}
						}
					}

					List<GUIContent> paramNames = mecParams.Select(mp => new GUIContent(mp.name)).ToList();

					if(selectedParamIndex == -1){
						errors.Add("The property \""+spMecParamName.stringValue+"\" was not found on the AnimatorController.");
					}

					newParamIndex = EditorGUI.Popup(new Rect(x, y, w, slh), new GUIContent("Parameter"), selectedParamIndex, paramNames.ToArray());
				}

				if(newParamIndex != selectedParamIndex){
					spMecParamName.stringValue = mecParams[newParamIndex].name;
					switch(mecParams[newParamIndex].type){
						case AnimatorControllerParameterType.Bool:	spMecParamType.enumValueIndex = (int)DriverTermMecParamType.Bool;		break;
						case AnimatorControllerParameterType.Float:	spMecParamType.enumValueIndex = (int)DriverTermMecParamType.Float;		break;
						case AnimatorControllerParameterType.Int:	spMecParamType.enumValueIndex = (int)DriverTermMecParamType.Integer;	break;
					}
				}

				if(spMecParamType.enumValueIndex == (int)DriverTermMecParamType.Bool){
					drawRangeOption = false;
				}
				else{
					y += slh + svs;
					DrawStartEndValueFields(spStartValue, spEndValue, "Start value", "End value", typeof(Animator), "AnimatorController", hasAnimator);
				}
//				
			}
			else if(spType.enumValueIndex == (int)DriverTermType.MecanimTime){
				SerializedProperty spMecAnimName = property.FindPropertyRelative("animName");
				SerializedProperty spMecUseWeight = property.FindPropertyRelative("animUseWeight");
				SerializedProperty spMecAnimLayer = property.FindPropertyRelative("mecAnimLayer");
				Animator animator = ShapeDriverCInspector.shapeDriver.GetComponent<Animator>();
				bool hasAnimator = animator != null;
				AnimatorController ac = null;
				AnimatorState state = null;
				string[] stateNames = null;
				string[] layerNames = null;
				int selectedStateIndex = -1;
				int newStateIndex = -1;
				int selectedLayerIndex = -1;
				int newLayerIndex = -1;
				
				if(hasAnimator){
					ac = animator.runtimeAnimatorController as AnimatorController;
					layerNames = new string[ac.layers.Length];
					
					for(int i=0; i<ac.layers.Length; i++){
						layerNames[i] = i + " (" + ac.layers[i].name + ")";
					}

					AnimatorStateMachine sm = null;

					if(spMecAnimLayer.intValue < ac.layers.Length){
						sm = ac.layers[spMecAnimLayer.intValue].stateMachine;

						stateNames = new string[sm.states.Length];
						selectedLayerIndex = spMecAnimLayer.intValue;

						for(int i=0; i<sm.states.Length; i++){
							stateNames[i] = sm.states[i].state.name;
							if(sm.states[i].state.name == spMecAnimName.stringValue){
								selectedStateIndex = i;
								state = sm.states[i].state;
							}
						}
					}
					
					EditorGUIUtility.labelWidth = lwBig;
					newLayerIndex = EditorGUI.Popup(new Rect(x, y, hw, slh), "Anim. layer", selectedLayerIndex, layerNames);

					if(newLayerIndex != selectedLayerIndex){
						spMecAnimLayer.intValue = newLayerIndex;
					}
				}
				else{
					GUI.enabled = false;
					newLayerIndex = EditorGUI.Popup(new Rect(x, y, hw, slh), "Anim. layer", selectedLayerIndex, new string[]{""});
					GUI.enabled = true;
				}

				EditorGUIUtility.labelWidth = lwBig + 20;
				TryDrawRightHalf(spMecUseWeight, new GUIContent("Use layer weight"));

				y += slh + svs;

				EditorGUIUtility.labelWidth = lwBig;

				if(stateNames == null){
					stateNames = new string[]{""};
					GUI.enabled = false;
				}

				newStateIndex = EditorGUI.Popup(new Rect(x, y, w, slh), "State name", selectedStateIndex, stateNames);

				GUI.enabled = true;

				if(newStateIndex != selectedStateIndex){
					spMecAnimName.stringValue = stateNames[newStateIndex];
				}

				y += slh + svs;

				EditorGUI.PropertyField(new Rect(x, y, hw - fcBtnSize - fcBtnSpacing, slh), spStartValue, new GUIContent("Start time"));
				GUI.enabled = state != null && state.motion != null;
				if(GUI.Button(new Rect(x + hw - fcBtnSize, y, fcBtnSize, slh), new GUIContent(EditorGUIUtility.FindTexture("AnimationClip Icon"), "Set from clip duration"), s)){
					spStartValue.floatValue = state.motion.averageDuration;
				}
				GUI.enabled = true;
				if(Screen.width < ShapeDriverCInspector.minHalfWidth){
					y += slh + svs;
				}
				EditorGUI.PropertyField(new Rect(hx, y, hw - fcBtnSize - fcBtnSpacing, slh), spEndValue, new GUIContent("End time"));
				GUI.enabled = state != null && state.motion != null;
				if(GUI.Button(new Rect(hx + hw - fcBtnSize, y, fcBtnSize, slh), new GUIContent(EditorGUIUtility.FindTexture("AnimationClip Icon"), "Set from clip duration"), s)){
					spEndValue.floatValue = state.motion.averageDuration;
				}
				GUI.enabled = true;

				if(!hasAnimator){
					errors.Add("No Animator component found. Value will always be 0.");
				}
				else{
					if(spMecAnimLayer.intValue >= ac.layers.Length){
						errors.Add("Layer \""+spMecAnimLayer.intValue+"\" could not be found. Value will always be 0.");
					}
					if(state == null){
						errors.Add("State \""+spMecAnimName.stringValue+"\" could not be found on layer \""+spMecAnimLayer.intValue+"\". If it is not added errors will be generated at runtime.");
					}
				}
			}
			else if(spType.enumValueIndex == (int)DriverTermType.LegacyAnimTime){
				SerializedProperty spAnimName = property.FindPropertyRelative("animName");
				SerializedProperty spUseWeight = property.FindPropertyRelative("animUseWeight");
				Animation animation = ShapeDriverCInspector.shapeDriver.GetComponent<Animation>();
				bool hasAnimation = animation != null;
				AnimationState state = hasAnimation ? animation[spAnimName.stringValue] : null;
				
				int selectedAnimationIndex = -1;
				int newAnimationIndex = -1;

				if(hasAnimation){
					string[] animNames = new string[animation.GetClipCount()];

					int i = 0; //can't use regular for-loop to iterate over animation, but still need an index
					foreach(AnimationState animState in animation){
						animNames[i] = animState.name;
						if(animState.name == spAnimName.stringValue){
							selectedAnimationIndex = i;
						}
						i++;
					}

					EditorGUIUtility.labelWidth = lwBig;
					newAnimationIndex = EditorGUI.Popup(new Rect(x, y, hw, slh), "Anim. name", selectedAnimationIndex, animNames);

					if(newAnimationIndex != selectedAnimationIndex){
						spAnimName.stringValue = animNames[newAnimationIndex];
					}
				}
				else{
					GUI.enabled = false;
					EditorGUI.Popup(new Rect(x, y, hw, slh), "Anim. name", selectedAnimationIndex, new string[]{""});
					GUI.enabled = true;
				}

				EditorGUIUtility.labelWidth = lwBig + 20;
				TryDrawRightHalf(spUseWeight, new GUIContent("Use layer weight"));
				
				EditorGUIUtility.labelWidth = lwBig;
				
				y += slh + svs;
				
				EditorGUI.PropertyField(new Rect(x, y, hw - fcBtnSize - fcBtnSpacing, slh), spStartValue, new GUIContent("Start time"));
				GUI.enabled = state != null;
				if(GUI.Button(new Rect(x + hw - fcBtnSize, y, fcBtnSize, slh), new GUIContent(EditorGUIUtility.FindTexture("AnimationClip Icon"), "Set from clip duration"), s)){
					spStartValue.floatValue = state.length;
				}
				GUI.enabled = true;
				if(Screen.width < ShapeDriverCInspector.minHalfWidth){
					y += slh + svs;
				}
				EditorGUI.PropertyField(new Rect(hx, y, hw - fcBtnSize - fcBtnSpacing, slh), spEndValue, new GUIContent("End time"));
				GUI.enabled = state != null;
				if(GUI.Button(new Rect(hx + hw - fcBtnSize, y, fcBtnSize, slh), new GUIContent(EditorGUIUtility.FindTexture("AnimationClip Icon"), "Set from clip duration"), s)){
					spEndValue.floatValue = state.length;
				}
				GUI.enabled = true;
				
				if(!hasAnimation){
					errors.Add("No Animation component found. Value will always be 0.");
				}
				else{
					if(state == null){
						errors.Add("State \""+spAnimName.stringValue+"\" could not be found. If it is not added errors will be generated at runtime.");
					}
				}
			}
			else if(spType.enumValueIndex == (int)DriverTermType.ScriptCallback){
				SerializedProperty spCallbackName = property.FindPropertyRelative("callbackName");
				SerializedProperty spCallbackComponent = property.FindPropertyRelative("callbackComponent");
				string callbackName = spCallbackName.stringValue;
				int popupIndex = 0;
				int selectedIndex = -1;
				Component[] components = ShapeDriverCInspector.shapeDriver.GetComponents<Component>();

				reflectionMethods = new List<ReflectionMethodInfo>();
				reflectionMethodsPopup = new List<string>();

				foreach(Component component in components){
					System.Type componentType = component.GetType();
					MethodInfo[] methods = componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

					foreach(MethodInfo method in methods){
						if(method.ReturnType == typeof(System.Single) && method.GetParameters().Length == 0){
							reflectionMethods.Add(new ReflectionMethodInfo(component, method));
							reflectionMethodsPopup.Add(componentType.ToString().Split('.').Last<string>()+"/"+method.Name);

							if(method.Name == callbackName){
								selectedIndex = popupIndex;
							}

							popupIndex++;
						}
					}
				}

				int newSelection = EditorGUI.Popup(new Rect(x, y, w, slh), new GUIContent("Callback"), selectedIndex, reflectionMethodsPopup.Select(str => new GUIContent(str)).ToArray());

				if(newSelection != selectedIndex){
					spCallbackName.stringValue = reflectionMethods[newSelection].MethodInfo.Name;
					spCallbackComponent.objectReferenceValue = reflectionMethods[newSelection].Component;
				}

				y += slh + svs;

				EditorGUI.PropertyField(new Rect(x, y, hw, slh), spStartValue, new GUIContent("Start value"));
				TryDrawRightHalf(spEndValue, new GUIContent("End value"));
			}

			y += slh + svs;

			EditorGUI.PropertyField(new Rect(x, y, w, slh*2), property.FindPropertyRelative("influence"), new GUIContent("Influence"));

			y += slh*2 + svs;

			if(drawRangeOption){
				EditorGUI.PropertyField(new Rect(hx+hw-98, y, 98, slh), property.FindPropertyRelative("clampToRange"), new GUIContent("In range only"));
			}

			if(Application.isPlaying && ShapeDriverCInspector.shapeDriver.enabled){
				bool unused;
				float rawValue = ShapeDriverCInspector.shapeDriver.DriverTerms[ShapeDriverCInspector.curTermDrawIndex].CurrentValue;
				float evalValue = ShapeDriverCInspector.shapeDriver.DriverTerms[ShapeDriverCInspector.curTermDrawIndex].Evaluate(out unused);
				EditorGUI.LabelField(new Rect(x, y, hw==w?w*0.55f:hw, slh*2 - svs), "Monitored value: "+rawValue.ToString("f3")+"\nEvaluated value: "+evalValue.ToString("f3"), EditorStyles.helpBox);
			}

			int curTerm = ShapeDriverCInspector.curTermDrawIndex+1;
			
			foreach(string error in errors){
				EditorGUILayout.HelpBox("Error: [Term "+curTerm+"] "+error, MessageType.Error);
			}
			foreach(string warning in warnings){
				EditorGUILayout.HelpBox("Warning: [Term "+curTerm+"] "+warning, MessageType.Warning);
			}

			EditorGUI.EndProperty();
		}

		void DrawStartEndValueFields(SerializedProperty spStart, SerializedProperty spEnd, string startLabel, string endLabel){
			DrawStartEndValueFields(spStart, spEnd, startLabel, endLabel, typeof(Transform), "Transform", true);
		}

		void DrawStartEndValueFields(SerializedProperty spStart, SerializedProperty spEnd, string startLabel, string endLabel,
		                             System.Type iconType, string tooltipComponent, bool btnEnabled){
			EditorGUIUtility.labelWidth = lwBig;
			EditorGUI.PropertyField(new Rect(x, y, hw - fcBtnSize - fcBtnSpacing, slh), spStart, new GUIContent(startLabel));
			GUI.enabled = btnEnabled;
			if(GUI.Button(new Rect(x + hw - fcBtnSize, y, fcBtnSize, slh), new GUIContent(AssetPreview.GetMiniTypeThumbnail(iconType), "Set from current "+tooltipComponent+" value"), s)){
				spStart.floatValue = GetMonitoredPropertyValue(ShapeDriverCInspector.curTermDrawIndex);
			}
			GUI.enabled = true;
			if(Screen.width < ShapeDriverCInspector.minHalfWidth){
				y += slh + svs;
			}
			EditorGUI.PropertyField(new Rect(hx, y, hw - fcBtnSize - fcBtnSpacing, slh), spEnd, new GUIContent(endLabel));
			GUI.enabled = btnEnabled;
			if(GUI.Button(new Rect(hx + hw - fcBtnSize, y, fcBtnSize, slh), new GUIContent(AssetPreview.GetMiniTypeThumbnail(iconType), "Set from current "+tooltipComponent+" value"), s)){
				spEnd.floatValue = GetMonitoredPropertyValue(ShapeDriverCInspector.curTermDrawIndex);
			}
			GUI.enabled = true;
		}

		void DrawUseAxes(SerializedProperty property){
			EditorGUI.LabelField(new Rect(x, y, w, slh), "Axes");
			x += EditorGUIUtility.labelWidth;
			EditorGUI.PropertyField(new Rect(x, y, 24, slh), spUseX, GUIContent.none);
			x += 13;
			EditorGUI.LabelField(new Rect(x, y, w, slh), "X");
			x += 16;
			EditorGUI.PropertyField(new Rect(x, y, 24, slh), spUseY, GUIContent.none);
			x += 13;
			EditorGUI.LabelField(new Rect(x, y, w, slh), "Y");
			x += 16;
			EditorGUI.PropertyField(new Rect(x, y, 24, slh), spUseZ, GUIContent.none);
			x += 13;
			EditorGUI.LabelField(new Rect(x, y, w, slh), "Z");
		}

		void TryDrawRightHalf(SerializedProperty prop, GUIContent label){
			if(Screen.width < ShapeDriverCInspector.minHalfWidth){
				y += slh + svs;
			}

			EditorGUI.PropertyField(new Rect(hx, y, hw, slh), prop, label);
		}

		float GetMonitoredPropertyValue(int index){
			return ShapeDriverCInspector.shapeDriver.DriverTerms[index].GetMonitoredPropertyGetter(ShapeDriverCInspector.shapeDriver.transform)();
		}
	}

	class ReflectionMethodInfo{
		public Component @Component {get; private set;}
		public MethodInfo @MethodInfo {get; private set;}

		public ReflectionMethodInfo(Component component, MethodInfo methodInfo){
			this.Component = component;
			this.MethodInfo = methodInfo;
		}
	}
}