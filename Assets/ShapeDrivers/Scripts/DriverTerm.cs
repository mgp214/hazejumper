using UnityEngine;
using System;
using System.Reflection;
using System.Linq.Expressions;

namespace ShapeDrivers {
	public enum DriverTermType {Position, Rotation, Scale, DistanceToPoint, DistanceToObject, MecanimTime,
								MecanimParameter, LegacyAnimTime, ScriptCallback}
	public enum DriverTermModifyMode {Add, Multiply}
	public enum DriverTermChannel {X, Y, Z}
	public enum DriverTermRotationType {EulerAngles, PitchYawRoll, SingleAxis}
	public enum DriverTermMecParamType {Float, Integer, Bool}

	[System.Serializable]
	public class DriverTerm {
		public float CurrentValue {get{return currentValue;}}

		public DriverTermModifyMode mode;
		[SerializeField] DriverTermType type;
		[SerializeField] Space space = Space.Self;
		[SerializeField] float startValue;
		[SerializeField] float endValue;
		[SerializeField] DriverTermRotationType rotationType;
		[SerializeField] DriverTermChannel channel;
		[SerializeField] Vector3 compareVector;
		[SerializeField] Transform compareTarget;
		[SerializeField] bool useX = true;
		[SerializeField] bool useY = true;
		[SerializeField] bool useZ = true;
		[SerializeField] bool useForwardRotation = true;
		[SerializeField] DriverTermMecParamType mecParamType;
		[SerializeField] string animName; //Param, state or animation name
		[SerializeField] bool animUseWeight;
		[SerializeField] int mecAnimLayer;
		[SerializeField] AnimationCurve influence;
		[SerializeField] bool clampToRange;
		[SerializeField] string callbackName;
		[SerializeField] Component callbackComponent;

		float realStartValue;
		float currentValue;

		public Func<float> GetDriverValue {get; private set;}

		public DriverTerm(DriverTermModifyMode mode){
			this.mode = mode;

			influence = new AnimationCurve();
			influence.AddKey(0f, 0f);
			influence.AddKey(1f, 1f);
		}

		public void Init(Transform transform){
			if(type == DriverTermType.Rotation){
				if(useForwardRotation && endValue < startValue){
					realStartValue = startValue;
					startValue -= 360f;
				}
				else if(startValue < 0f){
					realStartValue = startValue + 360f;
				}
			}
			else if(type == DriverTermType.MecanimParameter && mecParamType == DriverTermMecParamType.Bool){
				startValue = 0f;
				endValue = 1f;
			}

			GetDriverValue = GetMonitoredPropertyGetter(transform);
		}

		public float Evaluate(out bool useResult){
			currentValue = GetDriverValue();
			useResult = !clampToRange || (clampToRange && currentValue > startValue && currentValue < endValue);
			return influence.Evaluate((currentValue - startValue) / (endValue - startValue));
		}

		public System.Func<float> GetMonitoredPropertyGetter(Transform transform){
			switch(type){
				case DriverTermType.Position:
					if(space == Space.Self){
						switch(channel){
							case DriverTermChannel.X:	return () => {return transform.localPosition.x;};
							case DriverTermChannel.Y:	return () => {return transform.localPosition.y;};
							case DriverTermChannel.Z:	return () => {return transform.localPosition.z;};
						}
					}
					else{
						switch(channel){
							case DriverTermChannel.X:	return () => {return transform.position.x;};
							case DriverTermChannel.Y:	return () => {return transform.position.y;};
							case DriverTermChannel.Z:	return () => {return transform.position.z;};
						}
					}
					break;
				case DriverTermType.Rotation:
					switch(rotationType){
						case DriverTermRotationType.EulerAngles:
							if(space == Space.Self){
								switch(channel){
									case DriverTermChannel.X:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = transform.localEulerAngles.x; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return transform.eulerAngles.x;});
									case DriverTermChannel.Y:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = transform.localEulerAngles.y; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return transform.eulerAngles.y;});
									case DriverTermChannel.Z:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = transform.localEulerAngles.z; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return transform.eulerAngles.z;});
								}
							}
							else{
								switch(channel){
									case DriverTermChannel.X:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = transform.eulerAngles.x; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return transform.eulerAngles.x;});
									case DriverTermChannel.Y:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = transform.eulerAngles.y; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return transform.eulerAngles.y;});
									case DriverTermChannel.Z:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = transform.eulerAngles.z; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return transform.eulerAngles.z;});
								}
							}
							break;
						case DriverTermRotationType.PitchYawRoll:
							if(space == Space.Self){
								switch(channel){
									case DriverTermChannel.X:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetPitchYawRoll(transform.localRotation).x; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetPitchYawRoll(transform.localRotation).x;});
									case DriverTermChannel.Y:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetPitchYawRoll(transform.localRotation).y; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetPitchYawRoll(transform.localRotation).y;});
									case DriverTermChannel.Z:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetPitchYawRoll(transform.localRotation).z; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetPitchYawRoll(transform.localRotation).z;});
								}
							}
							else{
								switch(channel){
									case DriverTermChannel.X:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetPitchYawRoll(transform.rotation).x; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetPitchYawRoll(transform.rotation).x;});
									case DriverTermChannel.Y:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetPitchYawRoll(transform.rotation).y; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetPitchYawRoll(transform.rotation).y;});
									case DriverTermChannel.Z:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetPitchYawRoll(transform.rotation).z; return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetPitchYawRoll(transform.rotation).z;});
								}
							}
							break;
						case DriverTermRotationType.SingleAxis:
							if(space == Space.Self){
								switch(channel){
									case DriverTermChannel.X:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetSingleAxisRotation(transform.localRotation, Vector3.forward, 2, 1); return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetSingleAxisRotation(transform.localRotation, Vector3.forward, 2, 1);});
									case DriverTermChannel.Y:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetSingleAxisRotation(transform.localRotation, Vector3.forward, 2, 0); return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetSingleAxisRotation(transform.localRotation, Vector3.forward, 2, 0);});
									case DriverTermChannel.Z:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetSingleAxisRotation(transform.localRotation, Vector3.up, 1, 0); return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetSingleAxisRotation(transform.localRotation, Vector3.up, 1, 0);});
								}
							}
							else{
								switch(channel){
									case DriverTermChannel.X:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetSingleAxisRotation(transform.rotation, Vector3.forward, 2, 1); return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetSingleAxisRotation(transform.rotation, Vector3.forward, 2, 1);});
									case DriverTermChannel.Y:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetSingleAxisRotation(transform.rotation, Vector3.forward, 2, 0); return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetSingleAxisRotation(transform.rotation, Vector3.forward, 2, 0);});
									case DriverTermChannel.Z:	return (startValue < 0f) ?
										(Func<float>)(() => {float f = GetSingleAxisRotation(transform.rotation, Vector3.up, 1, 0); return f >= realStartValue ? f - 360f : f;}) :
										(Func<float>)(() => {return GetSingleAxisRotation(transform.rotation, Vector3.up, 1, 0);});
								}
							}
							break;
				}
				break;
				case DriverTermType.Scale:
					if(space == Space.Self){
						switch(channel){
							case DriverTermChannel.X:	return () => {return transform.localScale.x;};
							case DriverTermChannel.Y:	return () => {return transform.localScale.y;};
							case DriverTermChannel.Z:	return () => {return transform.localScale.z;};
						}
					}
					else{
						switch(channel){
							case DriverTermChannel.X:	return () => {return (transform.rotation*transform.lossyScale).x;};
							case DriverTermChannel.Y:	return () => {return (transform.rotation*transform.lossyScale).y;};
							case DriverTermChannel.Z:	return () => {return (transform.rotation*transform.lossyScale).z;};
						}
					}
					break;
			case DriverTermType.DistanceToPoint:
				if(space == Space.Self){
					return () => {return Vector3.Distance(VectorFromUsedAxes(transform.localPosition), VectorFromUsedAxes(compareVector));};
				}
				else{
					return () => {return Vector3.Distance(VectorFromUsedAxes(transform.position), VectorFromUsedAxes(compareVector));};
				}
			case DriverTermType.DistanceToObject:
				if(space == Space.Self){
					return () => {return Vector3.Distance(VectorFromUsedAxes(transform.localPosition), VectorFromUsedAxes(compareTarget.localPosition));};
				}
				else{
					return () => {return Vector3.Distance(VectorFromUsedAxes(transform.position), VectorFromUsedAxes(compareTarget.position));};
				}
			case DriverTermType.MecanimTime:
				Animator mtAnimator = transform.GetComponent<Animator>();
				System.Func<Animator, AnimatorStateInfo, float> mecValDelegate;

				if(animUseWeight && mecAnimLayer > 0){ //GetLayerWeight(0) always returns 0f, despite it always acting as 1f. Can't be user-set anyway, so hack around it.
					mecValDelegate = delegate(Animator anm, AnimatorStateInfo asi){return asi.normalizedTime * asi.length * anm.GetLayerWeight(mecAnimLayer);};
				}
				else{
					mecValDelegate = delegate(Animator anm, AnimatorStateInfo asi){return asi.normalizedTime * asi.length;};
				}

				return() => {
					AnimatorStateInfo stateInfo = mtAnimator.GetCurrentAnimatorStateInfo(mecAnimLayer);
					if(stateInfo.IsName(animName)){
						return mecValDelegate(mtAnimator, stateInfo);
					}
					return 0f;
				};
			case DriverTermType.MecanimParameter:
				Animator mpAnimator = transform.GetComponent<Animator>();

				switch(mecParamType){
					case DriverTermMecParamType.Float:		return () => {return mpAnimator.GetFloat(animName);};
					case DriverTermMecParamType.Integer:	return () => {return mpAnimator.GetInteger(animName);};
					case DriverTermMecParamType.Bool:		return () => {return mpAnimator.GetBool(animName) ? 1f : 0f;};
				}
				break;
			case DriverTermType.LegacyAnimTime:
				Animation animation = transform.GetComponent<Animation>();
				System.Func<AnimationState, float> legValDelegate;
				
				if(animUseWeight){
					legValDelegate = delegate(AnimationState anms){return anms.time * anms.weight;};
				}
				else{
					legValDelegate = delegate(AnimationState anms){return anms.time;};
				}
				return() => {
					if(animation[animName].enabled){
						return legValDelegate(animation[animName]);
					}
					return 0f;
				};
			case DriverTermType.ScriptCallback:
				MethodInfo method = callbackComponent.GetType().GetMethod(callbackName, BindingFlags.Instance | BindingFlags.Public);
				return Expression.Lambda<System.Func<float>>(Expression.Call(Expression.Constant(callbackComponent), method)).Compile();
			}
			return () => {return 0f;};
		}

		Vector3 VectorFromUsedAxes(Vector3 vec){
			if(!useX) vec.x = 0f;
			if(!useY) vec.y = 0f;
			if(!useZ) vec.z = 0f;
			return vec;
		}

		float GetSingleAxisRotation(Quaternion rotation, Vector3 forward, int atanComponent1, int atanComponent2){
			forward = rotation * forward;
			float angle = Mathf.Atan2(forward[atanComponent1], forward[atanComponent2]) * Mathf.Rad2Deg - 90f;
			if(angle < 0f){ //change this to "angle + 1E-05 < 0f" if rounding errors prove to be a hassle
				angle += 360f;
			}
			return angle;
		}

		Vector3 GetPitchYawRoll(Quaternion rotation){
			float pitch = Mathf.Atan2(2*rotation.x*rotation.w - 2*rotation.y*rotation.z, 1 - 2*rotation.x*rotation.x - 2*rotation.z*rotation.z) * Mathf.Rad2Deg;
			float yaw = Mathf.Atan2(2*rotation.y*rotation.w - 2*rotation.x*rotation.z, 1 - 2*rotation.y*rotation.y - 2*rotation.z*rotation.z) * Mathf.Rad2Deg;
			float roll = Mathf.Asin(2*rotation.x*rotation.y + 2*rotation.z*rotation.w) * Mathf.Rad2Deg;
			
			return new Vector3(pitch+180f, yaw+180f, roll+180f);
		}
	}
}