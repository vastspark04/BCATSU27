using UnityEngine;

public class LightRig : EMUI
{
	public enum LightsType
	{
		Directional,
		Point,
		Spot
	}

	public float rotSens = 15f;

	public float offsetSens = 0.3f;

	public GameObject DirectionalLight;

	public GameObject PointLights;

	public GameObject SpotLights;

	public ColorPicker m_ColorPicker;

	[HideInInspector]
	public Light[] m_Lights;

	private LightsType curLightType;

	private GameObject curLightObject;

	private bool m_UILockInstigator;

	private bool m_AnimateLight;

	private void Start()
	{
		SetDirectionalLight();
	}

	public void SetPointLights()
	{
		ChangeLights(LightsType.Point);
	}

	public void SetSpotLights()
	{
		ChangeLights(LightsType.Spot);
	}

	public void SetDirectionalLight()
	{
		ChangeLights(LightsType.Directional);
	}

	public void ToggleLightAnimation()
	{
		if (m_AnimateLight)
		{
			m_AnimateLight = false;
		}
		else
		{
			m_AnimateLight = true;
		}
	}

	private void ChangeLights(LightsType lightTypes)
	{
		Object.Destroy(curLightObject);
		m_Lights = null;
		switch (lightTypes)
		{
		case LightsType.Directional:
			curLightType = LightsType.Directional;
			curLightObject = Object.Instantiate(DirectionalLight);
			curLightObject.transform.position = new Vector3(0f, 1.8f, 0f);
			break;
		case LightsType.Point:
			curLightType = LightsType.Point;
			curLightObject = Object.Instantiate(PointLights);
			break;
		case LightsType.Spot:
			curLightType = LightsType.Spot;
			curLightObject = Object.Instantiate(SpotLights);
			break;
		}
		m_Lights = curLightObject.GetComponentsInChildren<Light>();
		if ((bool)m_ColorPicker)
		{
			m_ColorPicker.SetCurrentColor();
			m_ColorPicker.SetCurrentIntensity();
		}
	}

	private void Update()
	{
		if (!(curLightObject != null))
		{
			return;
		}
		if (CheckGUI(2, ref m_UILockInstigator))
		{
			switch (curLightType)
			{
			case LightsType.Directional:
				curLightObject.transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotSens, Space.World);
				curLightObject.transform.Rotate(Vector3.left * Input.GetAxis("Mouse Y") * rotSens, Space.Self);
				break;
			case LightsType.Point:
				curLightObject.transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotSens, Space.World);
				curLightObject.transform.Translate(Vector3.up * Input.GetAxis("Mouse Y") * offsetSens, Space.World);
				break;
			case LightsType.Spot:
				curLightObject.transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotSens, Space.World);
				curLightObject.transform.Translate(Vector3.up * Input.GetAxis("Mouse Y") * offsetSens, Space.World);
				break;
			}
			Vector3 position = curLightObject.transform.position;
			position.y = Mathf.Clamp(position.y, 0f, 3.3f);
			curLightObject.transform.position = position;
		}
		if (Input.GetButtonDown("AnimateLight"))
		{
			ToggleLightAnimation();
		}
		if (m_AnimateLight)
		{
			curLightObject.transform.RotateAround(base.transform.position, Vector3.up, 0.6f);
		}
	}
}
