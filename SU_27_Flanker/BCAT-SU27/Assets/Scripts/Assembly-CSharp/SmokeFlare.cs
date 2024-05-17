using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class SmokeFlare : MonoBehaviour
{
	public enum FlareColors
	{
		Red,
		Blue,
		Orange,
		Yellow,
		White,
		Green,
		Purple
	}

	public delegate void IgniteFlareDelegate(float duration, FlareColors color, Vector3D globalPosition, float timestamp);

	public ParticleSystem smokePs;

	public ParticleSystem[] coloredPs;

	public ParticleSystem[] allPs;

	public Light pointLight;

	public float heatGeneration;

	public HeatEmitter heatEmitter;

	private bool alive = true;

	private const string resourcePath = "Effects/SmokeFlare";

	private static GameObject prefab;

	public static event IgniteFlareDelegate OnFiredFlare;

	public void Ignite(float duration, FlareColors flareColor, Vector3 position)
	{
		base.transform.position = position;
		Color color = GetColor(flareColor);
		color = Color.Lerp(color, Color.white, 0.25f);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		Color value = Color.Lerp(color, Color.white, 0.15f);
		materialPropertyBlock.SetColor("_TintColor", value);
		smokePs.GetComponent<Renderer>().SetPropertyBlock(materialPropertyBlock);
		pointLight.color = color;
		ParticleSystem[] array = coloredPs;
		for (int i = 0; i < array.Length; i++)
		{
			ParticleSystem.MainModule main = array[i].main;
			main.startColor = new ParticleSystem.MinMaxGradient(color);
		}
		StartCoroutine(LifeRoutine(duration));
		StartCoroutine(WindRoutine());
		StartCoroutine(LightFlickerRoutine());
		FlightSceneManager.instance.OnExitScene += Instance_OnExitScene;
		if (VTOLMPUtils.IsMultiplayer())
		{
			Vector3D globalPosition = VTMapManager.WorldToGlobalPoint(position);
			SmokeFlare.OnFiredFlare?.Invoke(duration, flareColor, globalPosition, VTNetworkManager.GetNetworkTimestamp());
		}
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= Instance_OnExitScene;
		}
	}

	private void Instance_OnExitScene()
	{
		Object.Destroy(base.gameObject);
	}

	private IEnumerator LifeRoutine(float lifeTime)
	{
		float origCooldown = heatEmitter.cooldownRate;
		heatEmitter.cooldownRate = 0f;
		heatEmitter.AddHeat(heatGeneration);
		yield return new WaitForSeconds(lifeTime);
		heatEmitter.cooldownRate = origCooldown;
		allPs.SetEmission(emit: false);
		alive = false;
		yield return new WaitForSeconds(allPs.GetLongestLife());
		Object.Destroy(base.gameObject);
	}

	private IEnumerator WindRoutine()
	{
		if ((bool)WindVolumes.instance && WindVolumes.windEnabled)
		{
			while (base.enabled && (bool)smokePs)
			{
				Vector3 wind = WindVolumes.instance.GetWind(base.transform.position);
				ParticleSystem.ForceOverLifetimeModule forceOverLifetime = smokePs.forceOverLifetime;
				float num = 1.5f;
				forceOverLifetime.x = new ParticleSystem.MinMaxCurve(wind.x - num, wind.x + num);
				forceOverLifetime.y = new ParticleSystem.MinMaxCurve(wind.y - num, wind.y + num);
				forceOverLifetime.z = new ParticleSystem.MinMaxCurve(wind.z - num, wind.z + num);
				yield return new WaitForSeconds(5f);
			}
		}
	}

	private IEnumerator LightFlickerRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.08f);
		while (base.enabled && alive && (bool)pointLight)
		{
			pointLight.intensity = Random.Range(1f, 3f);
			yield return wait;
		}
		if ((bool)pointLight)
		{
			pointLight.enabled = false;
		}
	}

	private Color GetColor(FlareColors fC)
	{
		return fC switch
		{
			FlareColors.Red => Color.red, 
			FlareColors.Blue => Color.blue, 
			FlareColors.Orange => new Color(1f, 0.5f, 0f), 
			FlareColors.Yellow => new Color(1f, 1f, 0f), 
			FlareColors.White => Color.white, 
			FlareColors.Green => Color.green, 
			FlareColors.Purple => new Color(0.59607846f, 0f, 1f), 
			_ => Color.magenta, 
		};
	}

	public static void IgniteFlare(float duration, FlareColors color, Vector3 position, bool remote = false)
	{
		if (!VTOLMPUtils.IsMultiplayer() || VTOLMPLobbyManager.isLobbyHost || remote)
		{
			if (prefab == null)
			{
				prefab = Resources.Load<GameObject>("Effects/SmokeFlare");
			}
			Object.Instantiate(prefab).GetComponent<SmokeFlare>().Ignite(duration, color, position);
		}
	}
}
