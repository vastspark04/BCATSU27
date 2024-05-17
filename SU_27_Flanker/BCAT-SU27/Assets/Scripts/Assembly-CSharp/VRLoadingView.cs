using System.Collections;
using UnityEngine;

public class VRLoadingView : MonoBehaviour
{
	public Transform cameraTf;

	public Transform uiTf;

	public float dist = 15f;

	private float lerpRate = 1f;

	public Renderer mapRenderer;

	public bool showInEditors;

	private MaterialPropertyBlock props;

	private void SetEdgeMode(VTMapGenerator.EdgeModes edgeMode, CardinalDirections coastSide)
	{
		if (!mapRenderer)
		{
			return;
		}
		mapRenderer.sharedMaterial.DisableKeyword("HILLS_BORDER");
		mapRenderer.sharedMaterial.DisableKeyword("WATER_BORDER");
		mapRenderer.sharedMaterial.DisableKeyword("COAST_BORDER");
		mapRenderer.sharedMaterial.DisableKeyword("NORTH_COAST");
		mapRenderer.sharedMaterial.DisableKeyword("EAST_COAST");
		mapRenderer.sharedMaterial.DisableKeyword("SOUTH_COAST");
		mapRenderer.sharedMaterial.DisableKeyword("WEST_COAST");
		switch (edgeMode)
		{
		case VTMapGenerator.EdgeModes.Hills:
			mapRenderer.sharedMaterial.EnableKeyword("HILLS_BORDER");
			break;
		case VTMapGenerator.EdgeModes.Water:
			mapRenderer.sharedMaterial.EnableKeyword("WATER_BORDER");
			break;
		case VTMapGenerator.EdgeModes.Coast:
			mapRenderer.sharedMaterial.EnableKeyword("COAST_BORDER");
			switch (coastSide)
			{
			case CardinalDirections.North:
				mapRenderer.sharedMaterial.EnableKeyword("NORTH_COAST");
				break;
			case CardinalDirections.East:
				mapRenderer.sharedMaterial.EnableKeyword("EAST_COAST");
				break;
			case CardinalDirections.South:
				mapRenderer.sharedMaterial.EnableKeyword("SOUTH_COAST");
				break;
			case CardinalDirections.West:
				mapRenderer.sharedMaterial.EnableKeyword("WEST_COAST");
				break;
			}
			break;
		}
	}

	private void SetProps()
	{
		if ((bool)mapRenderer)
		{
			VTMapGenerator fetch = VTMapGenerator.fetch;
			SetEdgeMode(fetch.edgeMode, fetch.coastSide);
			props = new MaterialPropertyBlock();
			float chunkSize = fetch.chunkSize;
			float num = (float)fetch.gridSize * chunkSize;
			float num2 = fetch.oobProfile.repeatScale;
			float value = num / (chunkSize * num2);
			props.SetFloat("_OOBTexScale", value);
			props.SetTexture("_OOBTex", fetch.oobProfile.heightMap);
			props.SetTexture("_MainTex", VTMapGenerator.fetch.heightMap);
			mapRenderer.SetPropertyBlock(props);
		}
	}

	private IEnumerator Start()
	{
		while (!VTMapGenerator.fetch || !VTMapGenerator.fetch.heightMap)
		{
			yield return null;
		}
		if ((bool)cameraTf)
		{
			cameraTf.parent.localPosition = VRHead.playAreaPosition;
			cameraTf.parent.localRotation = VRHead.playAreaRotation;
		}
		SetProps();
		if ((bool)mapRenderer)
		{
			mapRenderer.gameObject.SetActive(value: true);
			mapRenderer.enabled = true;
		}
		else
		{
			Debug.Log("Missing mapRenderer in VRLoadingView!");
		}
		if (showInEditors || VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
		{
			while (!VRHead.instance)
			{
				if ((bool)cameraTf)
				{
					uiTf.position = Vector3.Lerp(uiTf.position, cameraTf.position + cameraTf.forward * dist, lerpRate * Time.deltaTime);
					uiTf.rotation = Quaternion.LookRotation(uiTf.position - cameraTf.position, Vector3.up);
				}
				if ((bool)mapRenderer)
				{
					float a = Mathf.Clamp01(FlightSceneManager.instance.SceneLoadPercent() * 1.1111112f);
					props.SetColor("_Color", new Color(1f, 1f, 1f, a));
					mapRenderer.SetPropertyBlock(props);
				}
				yield return null;
			}
		}
		Object.Destroy(base.gameObject);
	}
}
