using UnityEngine;
using System.Collections.Generic;

namespace ShapeDrivers {
	public class ShapeDriverManager : MonoBehaviour {
		class DriverInfo {
			public Dictionary<string, List<ShapeDriver>> drivers = new Dictionary<string, List<ShapeDriver>>(); //<shape name, drivers>
			public Dictionary<string, int> shapeIndices = new Dictionary<string, int>(); //<shape name, shape index>
			public Dictionary<string, float> oldValues = new Dictionary<string, float>(); //<shape name, value>
		}

		static ShapeDriverManager instance;
		static Dictionary<SkinnedMeshRenderer, DriverInfo> shapeDrivers = new Dictionary<SkinnedMeshRenderer, DriverInfo>();
		static System.Action<SkinnedMeshRenderer> SortShapeIfPostStart = smr => {};

		float t;
		float val;
		bool useResult;
		bool hasFirstVal;

		void Start(){
			foreach(KeyValuePair<SkinnedMeshRenderer, DriverInfo> driverInfoKV in shapeDrivers){
				foreach(KeyValuePair<string, List<ShapeDriver>> driversKV in driverInfoKV.Value.drivers){
					driversKV.Value.Sort((a, b) => a.Priority.CompareTo(b.Priority));
				}
			}

			SortShapeIfPostStart = smr => {
				foreach(KeyValuePair<string, List<ShapeDriver>> driversKV in shapeDrivers[smr].drivers){
					driversKV.Value.Sort((a, b) => a.Priority.CompareTo(b.Priority));
				}
			};

			Evaluate();
		}

		void LateUpdate(){
			Evaluate();
		}

		void Evaluate(){
			foreach(KeyValuePair<SkinnedMeshRenderer, DriverInfo> driverInfoKV in shapeDrivers){
				foreach(KeyValuePair<string, List<ShapeDriver>> driversKV in driverInfoKV.Value.drivers){
					val = driversKV.Value[0].Evaluate(out useResult);
					hasFirstVal = useResult;
					if(!useResult) val = 0f;

					for(int i=1; i<driversKV.Value.Count; i++){
						t = driversKV.Value[i].Evaluate(out useResult);

						if(!useResult) continue;

						if(!hasFirstVal){
							if(driversKV.Value[i].Mode == DriverModifyMode.Multiply) continue;
							val = t;
							hasFirstVal = true;
						}
						else{
							switch(driversKV.Value[i].Mode){
								case DriverModifyMode.Add:		val += t;	break;
								case DriverModifyMode.Multiply:	val *= t;	break;
								case DriverModifyMode.Max:		val = Mathf.Max(val, t);	break;
								case DriverModifyMode.Min:		val = Mathf.Min(val, t);	break;
							}
						}
					}

					if(hasFirstVal && val != driverInfoKV.Value.oldValues[driversKV.Key]){
						driverInfoKV.Key.SetBlendShapeWeight(driverInfoKV.Value.shapeIndices[driversKV.Key], val);
						driverInfoKV.Value.oldValues[driversKV.Key] = val;
					}
				}
			}
		}

		public static void AddShapeDriver(ShapeDriver driver, int shapeIndex){
			if(shapeDrivers.ContainsKey(driver.SkinnedMeshRenderer)){
				if(shapeDrivers[driver.SkinnedMeshRenderer].drivers.ContainsKey(driver.Shape)){
					shapeDrivers[driver.SkinnedMeshRenderer].drivers[driver.Shape].Add(driver);
					return;
				}
			}
			else{
				shapeDrivers.Add(driver.SkinnedMeshRenderer, new DriverInfo());
			}

			shapeDrivers[driver.SkinnedMeshRenderer].drivers.Add(driver.Shape, new List<ShapeDriver>{driver});
			shapeDrivers[driver.SkinnedMeshRenderer].oldValues.Add(driver.Shape, 0f);
			shapeDrivers[driver.SkinnedMeshRenderer].shapeIndices.Add(driver.Shape, shapeIndex);

			SortShapeIfPostStart(driver.SkinnedMeshRenderer);

			if(instance == null){
				GameObject go = new GameObject("ShapeDriverManager");
				instance = go.AddComponent<ShapeDriverManager>();
			}
		}

		public static void RemoveShapeDriver(ShapeDriver driver){
			if(shapeDrivers.ContainsKey(driver.SkinnedMeshRenderer) && shapeDrivers[driver.SkinnedMeshRenderer].drivers.ContainsKey(driver.Shape)){
				if(shapeDrivers[driver.SkinnedMeshRenderer].drivers[driver.Shape].Count > 1){
					shapeDrivers[driver.SkinnedMeshRenderer].drivers[driver.Shape].Remove(driver);
				}
				else{ //remove entire shape listing
					shapeDrivers[driver.SkinnedMeshRenderer].drivers.Remove(driver.Shape);
					shapeDrivers[driver.SkinnedMeshRenderer].oldValues.Remove(driver.Shape);
					shapeDrivers[driver.SkinnedMeshRenderer].shapeIndices.Remove(driver.Shape);
				}
			}
		}
	}
}
