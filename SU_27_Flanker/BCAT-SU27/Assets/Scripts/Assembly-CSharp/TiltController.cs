using System;
using System.Collections;
using UnityEngine;

public class TiltController : MonoBehaviour, IQSVehicleComponent
{
	public Battery battery;

	public float powerDrain = 5f;

	public float startingTilt;

	public float maxTilt;

	public float tiltSpeed;

	public EngineEffects[] engineEffects;

	[Header("Tilt Animation (optional)")]
	public Animator tiltAnimation;

	public string clipName;

	public int animLayer = -1;

	public bool reverseAnimDirection;

	private int animNameID;

	private float tilt;

	public bool softClose = true;

	public float softCloseThresh = 5f;

	public float softCloseInputThreshold = 0.7f;

	private bool softClosing;

	private Coroutine softCloseRoutine;

	private bool loadedTilt;

	public float currentTilt => tilt;

	public event Action<float> OnTiltChanged;

	private void Start()
	{
		if (!loadedTilt)
		{
			tilt = startingTilt;
		}
		this.OnTiltChanged?.Invoke(tilt);
		if (tiltAnimation != null)
		{
			animNameID = Animator.StringToHash(clipName);
		}
		UpdateTilts(checkBatt: false);
	}

	public void PadInput(Vector3 pad)
	{
		if (!battery || battery.Drain(0.01f * Time.fixedDeltaTime))
		{
			float num = 0.3f;
			if (Mathf.Abs(pad.y) > num)
			{
				tilt += Mathf.Sign(pad.y) * tiltSpeed * Time.deltaTime;
				tilt = Mathf.Clamp(tilt, 0f, maxTilt);
				UpdateTilts();
				this.OnTiltChanged?.Invoke(tilt);
			}
		}
	}

	private IEnumerator SoftCloseRoutine()
	{
		softClosing = true;
		while (tilt < maxTilt && softClosing)
		{
			tilt = Mathf.MoveTowards(tilt, maxTilt, 0.5f * tiltSpeed * Time.deltaTime);
			UpdateTilts();
			yield return null;
		}
		softClosing = false;
	}

	public void PadInputScaled(Vector3 pad)
	{
		if ((bool)battery && !battery.Drain(0.01f * Time.fixedDeltaTime))
		{
			return;
		}
		float num = 0.3f;
		if (Mathf.Abs(pad.y) > num)
		{
			if (softClose && ((tilt > maxTilt - softCloseThresh && pad.y < 0f && pad.y > -1f + softCloseInputThreshold) || (tilt < softCloseThresh && pad.y > 0f && pad.y < 1f - softCloseInputThreshold)))
			{
				return;
			}
			if (softClosing)
			{
				softClosing = false;
				if (softCloseRoutine != null)
				{
					StopCoroutine(softCloseRoutine);
				}
			}
			tilt += pad.y * tiltSpeed * Time.deltaTime;
			tilt = Mathf.Clamp(tilt, 0f, maxTilt);
			UpdateTilts();
			this.OnTiltChanged?.Invoke(tilt);
		}
		else if (softClose && !softClosing && tilt > maxTilt - softCloseThresh)
		{
			softCloseRoutine = StartCoroutine(SoftCloseRoutine());
		}
	}

	private void UpdateTilts(bool checkBatt = true)
	{
		if (checkBatt && (bool)battery && !battery.Drain(powerDrain * Time.deltaTime))
		{
			return;
		}
		for (int i = 0; i < engineEffects.Length; i++)
		{
			engineEffects[i].SetTilt(tilt);
		}
		if ((bool)tiltAnimation)
		{
			float num = tilt / maxTilt;
			if (reverseAnimDirection)
			{
				num = 1f - num;
			}
			tiltAnimation.Play(animNameID, animLayer, num);
		}
	}

	public void SetTiltImmediate(float t)
	{
		tilt = t;
		for (int i = 0; i < engineEffects.Length; i++)
		{
			engineEffects[i].SetTilt(tilt);
		}
		if ((bool)tiltAnimation)
		{
			float num = tilt / maxTilt;
			if (reverseAnimDirection)
			{
				num = 1f - num;
			}
			tiltAnimation.Play(animNameID, animLayer, num);
		}
		this.OnTiltChanged?.Invoke(t);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_TiltController");
		configNode.SetValue("tilt", tilt);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_TiltController";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			SetTiltImmediate(ConfigNodeUtils.ParseFloat(node.GetValue("tilt")));
			loadedTilt = true;
		}
	}
}
