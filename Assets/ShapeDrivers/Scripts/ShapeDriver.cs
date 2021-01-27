using UnityEngine;
using System.Collections.Generic;

namespace ShapeDrivers {
	public enum DriverModifyMode {Add, Multiply, Min, Max}

	public class ShapeDriver : MonoBehaviour {
		//public getters, used by ShapeDriverManager
		public SkinnedMeshRenderer @SkinnedMeshRenderer	{get{return skinnedMeshRenderer;}}
		public string Shape								{get{return shape;}}
		public int Priority								{get{return priority;}}
		public DriverModifyMode Mode					{get{return shapeBlendMode;}}
		public List<DriverTerm> DriverTerms				{get{return driverTerms;}}

		public AnimationState s;
		[SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;
		[SerializeField] string shape;
		[SerializeField] int priority;
		[SerializeField] bool useDefaultFallback;
		[SerializeField, Range(0f, 1f)] float defaultValue;
		[SerializeField] DriverModifyMode shapeBlendMode;
		[SerializeField, Range(0f, 1f)] float influence = 1f;
		[SerializeField] List<DriverTerm> driverTerms = new List<DriverTerm>();

		bool termsInitialized = false;

		void Start(){} //Only here so that the enable/ disable Inspector toggle shows up

		void OnEnable(){
			int shapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(shape);
			if(shapeIndex > -1){
				ShapeDriverManager.AddShapeDriver(this, shapeIndex);

				if(!termsInitialized){
					for(int i=0; i<driverTerms.Count; i++){
						driverTerms[i].Init(transform);
					}
					termsInitialized = true;
				}
			}
		}

		void OnDisable(){
			ShapeDriverManager.RemoveShapeDriver(this);
		}

		public DriverTerm AddDriverTerm(){
			DriverTerm term = new DriverTerm(DriverTermModifyMode.Add);
			driverTerms.Add(term);
			return term;
		}

		public void RemoveDriverTerm(DriverTerm term){
			driverTerms.Remove(term);
		}

		public void RemoveDriverTerm(int index){
			driverTerms.RemoveAt(index);
		}

		public float Evaluate(out bool useDriverResult){
			float newValue = 0f;
			float termValue = 0f;
			bool useTermResult;
			bool hasTermResult = false;

			if(driverTerms.Count > 0){
				newValue = driverTerms[0].Evaluate(out useTermResult);

				hasTermResult = useTermResult;
				if(!useTermResult) newValue = 0f;

				for(int i=1; i<driverTerms.Count; i++){
					termValue = driverTerms[i].Evaluate(out useTermResult);

					if(useTermResult){
						switch(driverTerms[i].mode){
							case DriverTermModifyMode.Add:		newValue += termValue;
																hasTermResult = true;
																break;
							case DriverTermModifyMode.Multiply:	newValue *= termValue;
																break;
						}
					}
				}
			}

			if(hasTermResult){
				useDriverResult = true;
				return newValue * 100f * influence;
			}
			else{
				useDriverResult = useDefaultFallback;
				return defaultValue * 100f;
			}
		}
	}
}