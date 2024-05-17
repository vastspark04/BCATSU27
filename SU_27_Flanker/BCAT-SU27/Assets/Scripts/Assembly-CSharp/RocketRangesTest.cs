using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class RocketRangesTest : MonoBehaviour
{
	private class ETestStatus
	{
		public int rCount;
	}

	public RocketLauncher rl;

	public RocketRangeProfile profile;

	public Transform floorTf;

	public int minElevationAngle;

	public int maxElevationAngle;

	public float estMaxDist;

	public float maxEstImpactTime;

	public int textureSize = 64;

	public RawImage displayImg;

	public string assetOutputPath;

	private List<AnimationCurve> elevationCurves;

	private BDTexture bdTex;

	private DataGraph g;

	private void Start()
	{
		g = DataGraph.CreateGraph("Rockets", Vector3.zero);
		bdTex = new BDTexture(textureSize, 1 + maxElevationAngle - minElevationAngle);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			StartCoroutine(TestRoutine());
		}
	}

	private IEnumerator TestRoutine()
	{
		for (int y = 0; y < bdTex.height; y++)
		{
			float t = (float)y / (float)bdTex.height;
			Debug.Log("nrm = " + t);
			float elevation = Mathf.Lerp(minElevationAngle, maxElevationAngle, t);
			Debug.Log("elev = " + elevation);
			yield return StartCoroutine(ElevationTestRoutine(elevation, y));
		}
		Texture2D texture2D = bdTex.ToTexture2D(TextureFormat.ARGB32, mipmaps: false, linear: true);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		if ((bool)profile)
		{
			profile.minElevation = minElevationAngle;
			profile.maxElevation = maxElevationAngle;
			profile.maxRange = estMaxDist;
			profile.maxTime = maxEstImpactTime;
			if (!string.IsNullOrEmpty(assetOutputPath))
			{
				byte[] bytes = texture2D.EncodeToPNG();
				File.WriteAllBytes(Path.Combine(Path.GetFullPath("."), assetOutputPath), bytes);
			}
		}
		if ((bool)displayImg)
		{
			displayImg.texture = texture2D;
		}
	}

	private IEnumerator ElevationTestRoutine(float elevation, int y)
	{
		floorTf.rotation = Quaternion.Euler(0f - elevation, 0f, 0f);
		AnimationCurve curve = new AnimationCurve();
		AnimationCurve timeCurve = new AnimationCurve();
		ETestStatus status = new ETestStatus();
		for (int angle = 0; angle <= 45; angle++)
		{
			Fire(angle, curve, timeCurve, status);
			yield return new WaitForFixedUpdate();
			yield return null;
		}
		while (status.rCount < 46)
		{
			yield return null;
		}
		for (int i = 0; i < textureSize; i++)
		{
			float time = (float)i / (float)textureSize * estMaxDist;
			float num = curve.Evaluate(time) / 45f;
			float num2 = timeCurve.Evaluate(time) / maxEstImpactTime;
			bdTex.SetPixel(i, y, new BDColor(num, num2, num, 1f));
		}
	}

	private void Fire(float angle, AnimationCurve curve, AnimationCurve timeCurve, ETestStatus status)
	{
		Vector3 forward = Quaternion.AngleAxis(angle, Vector3.left) * Vector3.forward;
		rl.transform.rotation = Quaternion.LookRotation(forward);
		rl.ReloadAll();
		Rocket r = null;
		Rocket[] componentsInChildren = rl.GetComponentsInChildren<Rocket>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			(r = componentsInChildren[i]).inaccuracy = 0f;
		}
		rl.FireRocket();
		StartCoroutine(RocketRecordRoutine(r, angle, curve, timeCurve, status));
	}

	private IEnumerator RocketRecordRoutine(Rocket r, float angle, AnimationCurve curve, AnimationCurve timeCurve, ETestStatus status)
	{
		Vector3 startPt = rl.transform.position;
		Vector3 endPt = startPt;
		float t = Time.time;
		while ((bool)r)
		{
			endPt = r.transform.position;
			yield return null;
		}
		float magnitude = (endPt - startPt).magnitude;
		float value = Time.time - t;
		Debug.Log("angle: " + angle + ", dist: " + magnitude + ", time: " + value);
		g.AddValue(new Vector2(magnitude, angle));
		curve.AddKey(magnitude, angle);
		timeCurve.AddKey(magnitude, value);
		status.rCount++;
	}
}
