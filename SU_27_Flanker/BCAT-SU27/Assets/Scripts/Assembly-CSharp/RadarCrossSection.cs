using System;
using System.Collections.Generic;
using UnityEngine;

public class RadarCrossSection : MonoBehaviour
{
	[Serializable]
	public struct RadarReturn
	{
		public Vector3 normal;

		public float returnValue;
	}

	public const float RCS_MULTIPLIER = 100f;

	public const float DEFAULT_RCS = 40f;

	public Transform referenceTf;

	[Header("Configured Values")]
	public WeaponManager weaponManager;

	[Header("Generated Values")]
	public List<RadarReturn> returns = new List<RadarReturn>();

	public float size;

	public float overrideMultiplier = 1f;

	private Dictionary<GameObject, int> origLayers;

	private void Awake()
	{
		if (!referenceTf)
		{
			referenceTf = base.transform;
		}
	}

	public float GetCrossSection(Vector3 inDirection)
	{
		if (!referenceTf)
		{
			referenceTf = base.transform;
		}
		inDirection = -referenceTf.InverseTransformDirection(inDirection).normalized;
		float num = 0f;
		float num2 = 0f;
		float num3 = returns.Count;
		for (int i = 0; (float)i < num3; i++)
		{
			float num4 = Vector3.Dot(inDirection, returns[i].normal);
			if (num4 > 0f)
			{
				num4 = Mathf.Pow(num4, 15f);
				num += returns[i].returnValue * num4;
				num2 += num4;
			}
		}
		float num5 = 100f * overrideMultiplier * size * num / num2;
		if ((bool)weaponManager)
		{
			num5 += weaponManager.GetAdditionalRadarCrossSection();
		}
		return num5;
	}

	public float GetAverageCrossSection()
	{
		float num = 0f;
		if (returns != null && returns.Count > 0)
		{
			for (int i = 0; i < returns.Count; i++)
			{
				num += returns[i].returnValue;
			}
			num /= (float)returns.Count;
		}
		return 100f * overrideMultiplier * size * num;
	}

	[ContextMenu("Debug Average RCS")]
	public void DebugAvgRCS()
	{
		Debug.Log(GetAverageCrossSection());
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)Camera.current && returns != null && returns.Count > 0)
		{
			float crossSection = GetCrossSection(base.transform.position - Camera.current.transform.position);
			Gizmos.color = new Color(1f, 1f, 1f, 0.43f);
			Gizmos.DrawSphere(base.transform.position, crossSection);
		}
	}

    [ContextMenu("Generate RCS")]
    public void GenerateRCS()
    {
#if UNITY_EDITOR

        UnityEditor.Undo.RecordObject(this, "Generate RCS");

        returns = new List<RadarReturn>();

        Vector3 origPos = transform.position;
        transform.position = Vector3.zero;

        //Get the RT
        RenderTexture rt = RenderTexture.GetTemporary(256, 256, 16);

        //Create the camera
        Camera c = new GameObject().AddComponent<Camera>();
        c.cullingMask = 1 << 26;
        c.targetTexture = rt;
        c.orthographic = true;
        c.clearFlags = CameraClearFlags.Color;
        c.backgroundColor = Color.black;
        c.allowHDR = false;
        c.SetReplacementShader(Shader.Find("Custom/Radar"), "RenderType");

        //Get the size of the vessel and set layers
        Bounds modelBounds = new Bounds();
        origLayers = new Dictionary<GameObject, int>();
        var hiddenObjects = GetComponentsInChildren<HideForRCS>();
        foreach (var ho in hiddenObjects)
        {
            ho.gameObject.SetActive(false);
        }
        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.gameObject.layer == 0 || mr.gameObject.layer == 8)
            {
                origLayers.Add(mr.gameObject, mr.gameObject.layer);
                modelBounds.Encapsulate(mr.bounds);
                mr.gameObject.layer = 26;
            }
        }
        size = Mathf.Max(Mathf.Max(modelBounds.extents.x, modelBounds.extents.y), modelBounds.extents.z);
        c.orthographicSize = size;
        c.nearClipPlane = size;
        c.farClipPlane = size * 3;


        //Do the rendering
        Texture2D readTex = new Texture2D(256, 256);
        RenderRCS(Vector3.forward, Vector3.up, modelBounds.center, 45, 359.9f, c, readTex, size * 2);
        RenderRCS(Quaternion.AngleAxis(45, Vector3.right) * Vector3.forward, Vector3.right, modelBounds.center, 45, 91, c, readTex, size * 2);
        RenderRCS(Quaternion.AngleAxis(45 + 180, Vector3.right) * Vector3.forward, Vector3.right, modelBounds.center, 45, 91, c, readTex, size * 2);

        RenderRCS(Quaternion.AngleAxis(45, Vector3.forward) * Vector3.right, Vector3.forward, modelBounds.center, 45, 1, c, readTex, size * 2);
        RenderRCS(Quaternion.AngleAxis(90 + 45, Vector3.forward) * Vector3.right, Vector3.forward, modelBounds.center, 45, 1, c, readTex, size * 2);
        RenderRCS(Quaternion.AngleAxis(180 + 45, Vector3.forward) * Vector3.right, Vector3.forward, modelBounds.center, 45, 1, c, readTex, size * 2);
        RenderRCS(Quaternion.AngleAxis(270 + 45, Vector3.forward) * Vector3.right, Vector3.forward, modelBounds.center, 45, 1, c, readTex, size * 2);

        //Closing out
        c.targetTexture = null;
        DestroyImmediate(c.gameObject);
        DestroyImmediate(readTex);
        RenderTexture.ReleaseTemporary(rt);
        foreach (var go in origLayers.Keys)
        {
            go.layer = origLayers[go];
        }
        foreach (var ho in hiddenObjects)
        {
            ho.gameObject.SetActive(true);
        }
        transform.position = origPos;

#endif
    }

    private void RenderRCS(Vector3 startVector, Vector3 axis, Vector3 centerPt, float angleInterval, float angleLimit, Camera c, Texture2D tex, float dist)
	{
		startVector.Normalize();
		for (float num = 0f; num < angleLimit; num += angleInterval)
		{
			Vector3 vector = Quaternion.AngleAxis(num, axis) * startVector;
			Vector3 vector2 = centerPt + -vector * dist;
			c.transform.position = vector2;
			c.transform.rotation = Quaternion.LookRotation(vector);
			Debug.DrawLine(vector2, vector2 + vector * dist, Color.cyan, 10f);
			c.Render();
			RenderTexture.active = c.targetTexture;
			tex.ReadPixels(new Rect(0f, 0f, 256f, 256f), 0, 0, recalculateMipMaps: false);
			Color[] pixels = tex.GetPixels();
			float num2 = 0f;
			for (int i = 0; i < pixels.Length; i++)
			{
				num2 += pixels[i].r;
			}
			num2 /= (float)pixels.Length;
			returns.Add(new RadarReturn
			{
				normal = -vector,
				returnValue = num2
			});
		}
	}
}
