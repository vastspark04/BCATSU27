using System;
using System.Collections;
using UnityEngine;

public class SwitchableMaterialEmission : MonoBehaviour, ISwitchableEmission
{
	public MeshRenderer[] renderers;

	public string propertyName = "_EmissionColor";

	public Color emissionColor;

	public float emissionMultiplier;

	public bool defaultOn;

	private MaterialPropertyBlock props;

	private int propID;

	[Header("Optional")]
	public Battery battery;

	public float battDrain;

	private bool powered = true;

	private bool emissionOn;

	private float eMult = 1f;

	public bool isOn => emissionOn;

	public event Action<bool> OnSetEmission;

	public void SetEmission(int e)
	{
		SetEmission(e > 0);
	}

	public void SetEmission(bool e)
	{
		if (e != emissionOn)
		{
			emissionOn = e;
			if (e)
			{
				SetColor(eMult * emissionColor);
			}
			else
			{
				SetColor(Color.black);
			}
			this.OnSetEmission?.Invoke(e);
		}
	}

	public void SetEmissionMultiplier(float e)
	{
		eMult = e;
		if (emissionOn)
		{
			SetEmission(e: false);
			SetEmission(e: true);
		}
	}

	private void Awake()
	{
		props = new MaterialPropertyBlock();
		propID = Shader.PropertyToID(propertyName);
	}

	private void Start()
	{
		SetEmissionMultiplier(emissionMultiplier);
		SetEmission(!defaultOn);
		SetEmission(defaultOn);
	}

	private void SetColor(Color c)
	{
		props.SetColor(propID, c);
		for (int i = 0; i < renderers.Length; i++)
		{
			if ((bool)renderers[i])
			{
				renderers[i].SetPropertyBlock(props);
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)battery)
		{
			StartCoroutine(BatteryCheckRoutine());
		}
	}

	private IEnumerator BatteryCheckRoutine()
	{
		while (base.enabled)
		{
			if ((bool)battery)
			{
				bool flag = battery.Drain(battDrain * Time.deltaTime);
				if (flag != powered)
				{
					powered = flag;
					if (powered && emissionOn)
					{
						SetColor(eMult * emissionColor);
					}
					else
					{
						SetColor(Color.black);
					}
				}
			}
			yield return null;
		}
	}
}
