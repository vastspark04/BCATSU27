using System.Collections;
using System.Collections.Generic;
using OC;
using UnityEngine;
using UnityEngine.Events;

public class PlayerVehicleSetup : MonoBehaviour
{
	public static bool godMode;

	public List<GameObject> hideObjectsOnConfig;

	public List<Component> disableComponentOnConfig;

	public VRLever canopyLever;

	public UnityEvent OnBeginRearming;

	public UnityEvent OnEndRearming;

	private List<IPersistentDataSaver> dataSavers = new List<IPersistentDataSaver>();

	private List<IPersistentVehicleData> pVehicleData = new List<IPersistentVehicleData>();

	private WeaponManager wm;

	private FuelTank fuel;

	private float flightStartTime;

	private bool loadedData;

	private bool savedData;

	public event UnityAction<LoadoutConfigurator> OnBeginUsingConfigurator;

	public event UnityAction<LoadoutConfigurator> OnEndUsingConfigurator;

	private void Start()
	{
		RaySpringDamper[] suspensions = GetComponent<WheelsController>().suspensions;
		foreach (RaySpringDamper obj in suspensions)
		{
			obj.brakeAnchorSpeedThreshold = 0.17f;
			obj.brakeAnchor = true;
		}
	}

	public void SetToConfigurationState()
	{
		foreach (GameObject item in hideObjectsOnConfig)
		{
			item.SetActive(value: false);
		}
		foreach (Component item2 in disableComponentOnConfig)
		{
			Object.Destroy(item2);
		}
		VRInteractable[] componentsInChildren = GetComponentsInChildren<VRInteractable>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	public void StartUsingConfigurator(LoadoutConfigurator lc)
	{
		if (this.OnBeginUsingConfigurator != null)
		{
			this.OnBeginUsingConfigurator(lc);
		}
	}

	public void EndUsingConfigurator(LoadoutConfigurator lc)
	{
		if (this.OnEndUsingConfigurator != null)
		{
			this.OnEndUsingConfigurator(lc);
		}
	}

	public void SetupForFlight()
	{
		wm = GetComponent<WeaponManager>();
		fuel = GetComponent<FuelTank>();
		Loadout loadout;
		if (VehicleEquipper.loadoutSet)
		{
			loadout = VehicleEquipper.loadout;
			PilotSaveManager.currentScenario.inFlightSpending = 0f;
		}
		else
		{
			loadout = new Loadout();
			loadout.normalizedFuel = 0.65f;
			loadout.hpLoadout = new string[5] { "gau-8", "hellfirex4", "h70-4x4", "h70-4x4", "hellfirex4" };
			loadout.cmLoadout = new int[2] { 1000, 1000 };
		}
		wm.EquipWeapons(loadout);
		fuel.startingFuel = loadout.normalizedFuel * fuel.maxFuel;
		fuel.SetNormFuel(loadout.normalizedFuel);
		IPersistentDataSaver[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IPersistentDataSaver>(includeInactive: true);
		foreach (IPersistentDataSaver item in componentsInChildrenImplementing)
		{
			dataSavers.Add(item);
		}
		loadedData = true;
		if (PilotSaveManager.current != null)
		{
			ConfigNode vehicleDataNode = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).vehicleDataNode;
			if (vehicleDataNode != null)
			{
				IPersistentVehicleData[] componentsInChildrenImplementing2 = base.gameObject.GetComponentsInChildrenImplementing<IPersistentVehicleData>(includeInactive: true);
				foreach (IPersistentVehicleData persistentVehicleData in componentsInChildrenImplementing2)
				{
					pVehicleData.Add(persistentVehicleData);
					persistentVehicleData.OnLoadVehicleData(vehicleDataNode);
				}
			}
		}
		FlightSceneManager.instance.OnExitScene += SavePersistentData;
		if (godMode)
		{
			Health[] componentsInChildren = GetComponentsInChildren<Health>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].invincible = true;
			}
		}
		flightStartTime = Time.time;
		if (!QuicksaveManager.quickloading)
		{
			ScreenFader.FadeIn();
		}
		if (VTResources.useOverCloud)
		{
			Camera component = GetComponentInChildren<VRHead>().GetComponent<Camera>();
			component.tag = "MainCamera";
			SetupOCCam(component);
			Camera flybyCam = GetComponentInChildren<FlybyCameraMFDPage>().flybyCam;
			SetupOCCam(flybyCam);
			GameObject overCloudRainFX = VTResources.GetOverCloudRainFX();
			overCloudRainFX.transform.parent = component.transform.parent;
			overCloudRainFX.transform.localPosition = Vector3.zero;
			GameObject overCloudRainFX2 = VTResources.GetOverCloudRainFX();
			overCloudRainFX2.transform.parent = flybyCam.transform;
			overCloudRainFX2.transform.localPosition = Vector3.zero;
		}
	}

	public void LoadPersistentVehicleData()
	{
		loadedData = true;
		Debug.Log("Loading persistent vehicle data...");
		if (PilotSaveManager.current == null)
		{
			return;
		}
		ConfigNode vehicleDataNode = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).vehicleDataNode;
		if (vehicleDataNode != null)
		{
			IPersistentVehicleData[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IPersistentVehicleData>(includeInactive: true);
			foreach (IPersistentVehicleData persistentVehicleData in componentsInChildrenImplementing)
			{
				pVehicleData.Add(persistentVehicleData);
				persistentVehicleData.OnLoadVehicleData(vehicleDataNode);
			}
		}
	}

	public void SetupForFlightMP()
	{
		wm = GetComponent<WeaponManager>();
		fuel = GetComponent<FuelTank>();
		IPersistentDataSaver[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IPersistentDataSaver>(includeInactive: true);
		foreach (IPersistentDataSaver item in componentsInChildrenImplementing)
		{
			dataSavers.Add(item);
		}
		LoadPersistentVehicleData();
		FlightSceneManager.instance.OnExitScene += SavePersistentData;
		flightStartTime = Time.time;
		if (!QuicksaveManager.quickloading)
		{
			ScreenFader.FadeIn();
		}
		if (VTResources.useOverCloud)
		{
			Camera component = GetComponentInChildren<VRHead>().GetComponent<Camera>();
			component.tag = "MainCamera";
			SetupOCCam(component);
			Camera flybyCam = GetComponentInChildren<FlybyCameraMFDPage>().flybyCam;
			SetupOCCam(flybyCam);
			GameObject overCloudRainFX = VTResources.GetOverCloudRainFX();
			overCloudRainFX.transform.parent = component.transform.parent;
			overCloudRainFX.transform.localPosition = Vector3.zero;
			GameObject overCloudRainFX2 = VTResources.GetOverCloudRainFX();
			overCloudRainFX2.transform.parent = flybyCam.transform;
			overCloudRainFX2.transform.localPosition = Vector3.zero;
		}
	}

	private void SetupOCCam(Camera cam)
	{
		OverCloudCamera overCloudCamera = cam.gameObject.AddComponent<OverCloudCamera>();
		overCloudCamera.lightSampleCount = SampleCount.Low;
		overCloudCamera.scatteringMaskSamples = SampleCount.Low;
		overCloudCamera.downsampleFactor = DownSampleFactor.Half;
		overCloudCamera.renderScatteringMask = false;
		overCloudCamera.includeCascadedShadows = false;
	}

	public void SavePersistentData()
	{
		if (!loadedData || savedData)
		{
			return;
		}
		savedData = true;
		Debug.Log("Saving vehicle persistent data...");
		foreach (IPersistentDataSaver dataSaver in dataSavers)
		{
			dataSaver.SavePersistentData();
		}
		if (PilotSaveManager.current != null)
		{
			PilotSaveManager.current.totalFlightTime += Time.time - flightStartTime;
			ConfigNode configNode = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).vehicleDataNode;
			if (configNode == null)
			{
				configNode = new ConfigNode("VDATA");
				PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).vehicleDataNode = configNode;
			}
			foreach (IPersistentVehicleData pVehicleDatum in pVehicleData)
			{
				if ((bool)(Component)pVehicleDatum)
				{
					pVehicleDatum.OnSaveVehicleData(configNode);
				}
			}
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= SavePersistentData;
		}
	}

	private void OnDestroy()
	{
		if (!savedData && loadedData)
		{
			SavePersistentData();
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= SavePersistentData;
		}
	}

	public void LandVehicle(Transform spawnTransform)
	{
		StartCoroutine(LandVehicleRoutine(spawnTransform));
		Debug.Log("Landing player vehicle.");
	}

	private IEnumerator LandVehicleRoutine(Transform spawnTransform)
	{
		Rigidbody rb = GetComponent<Rigidbody>();
		rb.interpolation = RigidbodyInterpolation.None;
		rb.isKinematic = true;
		MovingPlatform movingPlatform = null;
		bool hit = false;
		RaycastHit hitInfo = default(RaycastHit);
		FixedPoint position = new FixedPoint(base.transform.position);
		PlayerVehicle pv = GetComponent<VehicleMaster>().playerVehicle;
		while (!hit)
		{
			if ((bool)spawnTransform)
			{
				base.transform.position = spawnTransform.TransformPoint(pv.playerSpawnOffset);
				Quaternion quaternion3 = (base.transform.rotation = (rb.rotation = Quaternion.AngleAxis(pv.spawnPitch, spawnTransform.right) * spawnTransform.rotation));
			}
			else
			{
				base.transform.position = position.point;
			}
			if (Physics.Raycast(base.transform.position, Vector3.down, out hitInfo, 5000f, 1) && hitInfo.point.y > WaterPhysics.instance.height)
			{
				position.point = hitInfo.point;
				movingPlatform = hitInfo.collider.GetComponent<MovingPlatform>();
				break;
			}
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			yield return null;
		}
		if ((bool)spawnTransform)
		{
			base.transform.position = spawnTransform.TransformPoint(pv.playerSpawnOffset);
			Quaternion quaternion3 = (base.transform.rotation = (rb.rotation = Quaternion.AngleAxis(pv.spawnPitch, spawnTransform.right) * spawnTransform.rotation));
		}
		else
		{
			base.transform.position = position.point;
		}
		rb.position = base.transform.position;
		rb.angularVelocity = Vector3.zero;
		if ((bool)movingPlatform)
		{
			rb.velocity = movingPlatform.GetVelocity(base.transform.position);
			if ((bool)movingPlatform.rb)
			{
				rb.angularVelocity = movingPlatform.rb.angularVelocity;
			}
		}
		else
		{
			rb.velocity = Vector3.zero;
		}
		rb.isKinematic = false;
		yield return new WaitForFixedUpdate();
		rb.interpolation = RigidbodyInterpolation.Interpolate;
	}
}
