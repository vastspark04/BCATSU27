using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WheelSurface : MonoBehaviour
{
	public WheelSurfaceMaterial material;

	public bool useBiomeDefault;

	private static Dictionary<Collider, WheelSurfaceMaterial> matDictionary;

	private Collider col;

	private static bool addedSceneChangeListener;

	private void Awake()
	{
		Init();
	}

	private void Start()
	{
		Collider component = GetComponent<Collider>();
		if (useBiomeDefault && (bool)VTCustomMapManager.instance && VTCustomMapManager.instance.mapGenerator.currentBiome != null)
		{
			material = VTCustomMapManager.instance.mapGenerator.currentBiome.defaultSurfaceMaterial;
		}
		matDictionary.Add(component, material);
	}

	private void OnDestroy()
	{
		if (matDictionary != null && col != null)
		{
			matDictionary.Remove(col);
		}
	}

	public static bool TryGetMaterial(Collider c, out WheelSurfaceMaterial material)
	{
		if (c != null && matDictionary != null)
		{
			return matDictionary.TryGetValue(c, out material);
		}
		material = null;
		return false;
	}

	public static void RegisterMaterial(Collider c, WheelSurfaceMaterial material)
	{
		Init();
		matDictionary.Add(c, material);
	}

	private static void Init()
	{
		if (matDictionary == null)
		{
			matDictionary = new Dictionary<Collider, WheelSurfaceMaterial>();
		}
		if (!addedSceneChangeListener)
		{
			SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
			addedSceneChangeListener = true;
		}
	}

	private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
	{
		if (matDictionary != null)
		{
			matDictionary.Clear();
		}
	}
}
