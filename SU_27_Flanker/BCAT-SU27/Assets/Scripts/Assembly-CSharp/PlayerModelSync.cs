using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;

public class PlayerModelSync : VTNetSyncRPCOnly
{
	public Transform referenceTf;

	public PilotColorSetup pilotColors;

	public float sendInterval = 0.2f;

	private static Dictionary<ulong, PlayerModelSync> playerModelDict = new Dictionary<ulong, PlayerModelSync>();

	[Header("Local")]
	public GameObject[] localObjects;

	public GameObject[] destroyOnRemote;

	public Transform localHead;

	public Transform localHandL;

	public Transform localHandR;

	public GloveAnimation localGloveL;

	public GloveAnimation localGloveR;

	public HelmetController localHelmet;

	[Header("Remote")]
	public GameObject[] remoteObjects;

	public GameObject[] destroyOnLocal;

	public float lerpRate = 5f;

	public Transform remoteHead;

	public Transform remoteHandL;

	public Transform remoteHandR;

	public GloveAnimation remoteGloveL;

	public GloveAnimation remoteGloveR;

	public Transform anchorTf;

	public float anchorRadius;

	public AnimationToggle remoteHelmetAnimator;

	private Vector3 remoteHeadPos;

	private Vector3 remoteHandLPos;

	private Vector3 remoteHandRPos;

	private Quaternion remoteHeadRot;

	private Quaternion remoteHandLRot;

	private Quaternion remoteHandRRot;

	private int remotePoseL;

	private int remotePoseR;

	private bool interactingRight;

	private VRInteractable rhInt;

	private bool interactingLeft;

	private VRInteractable lhInt;

	private const string sendMethodName = "R";

	private const string skeletalSendMethodName = "S";

	private void OnDrawGizmos()
	{
		if ((bool)anchorTf)
		{
			Gizmos.DrawWireSphere(anchorTf.position, anchorRadius);
		}
	}

	public static PlayerModelSync GetPlayerModel(ulong id)
	{
		if (playerModelDict.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (playerModelDict.ContainsKey(base.netEntity.ownerID))
		{
			playerModelDict[base.netEntity.ownerID] = this;
		}
		else
		{
			playerModelDict.Add(base.netEntity.ownerID, this);
		}
		if (localObjects != null)
		{
			localObjects.SetActive(base.isMine);
		}
		if (remoteObjects != null)
		{
			remoteObjects.SetActive(!base.isMine);
		}
		GameObject[] array;
		if (base.isMine)
		{
			array = destroyOnLocal;
			foreach (GameObject gameObject in array)
			{
				if ((bool)gameObject)
				{
					Object.Destroy(gameObject);
				}
			}
			if ((bool)localHelmet)
			{
				localHelmet.OnVisorState += LocalHelmet_OnVisorState;
			}
			if ((bool)pilotColors)
			{
				pilotColors.UpdatePropertiesLocal();
				SendRPCBuffered("RPC_Colors", ToVector(PilotSaveManager.current.suitColor), ToVector(PilotSaveManager.current.vestColor), ToVector(PilotSaveManager.current.strapsColor), ToVector(PilotSaveManager.current.gSuitColor), ToVector(PilotSaveManager.current.skinColor));
			}
			RefreshHelm(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
			return;
		}
		array = destroyOnRemote;
		foreach (GameObject gameObject2 in array)
		{
			if ((bool)gameObject2)
			{
				Object.Destroy(gameObject2);
			}
		}
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		if (base.isMine)
		{
			StartCoroutine(SendRoutine());
		}
		else
		{
			StartCoroutine(RemoteUpdateRoutine());
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		RefreshHelm(obj);
	}

	private void LocalHelmet_OnVisorState(int state)
	{
		SendRPC("RPC_HelmState", state);
	}

	[VTRPC]
	private void RPC_HelmState(int st)
	{
		if ((bool)remoteHelmetAnimator)
		{
			if (st == 0)
			{
				remoteHelmetAnimator.Deploy();
			}
			else
			{
				remoteHelmetAnimator.Retract();
			}
		}
	}

	private void RefreshHelm(ulong target = 0uL)
	{
		if (base.isMine && (bool)localHelmet)
		{
			SendDirectedRPC(target, "RPC_HelmState", localHelmet.isVisorDown ? 1 : 0);
		}
	}

	private void SendPositions()
	{
		if ((bool)referenceTf)
		{
			Vector3 vector = ReferencePosition(localHead);
			Vector3 vector2 = ReferencePosition(localHandL);
			Vector3 vector3 = ReferencePosition(localHandR);
			Quaternion quaternion = ReferenceRotation(localHead);
			Quaternion quaternion2 = ReferenceRotation(localHandL);
			Quaternion quaternion3 = ReferenceRotation(localHandR);
			if (localGloveL.skeletonAnim)
			{
				EncodeSkeletalFingers(out var l, out var r);
				SendRPC("S", vector, quaternion, vector2, quaternion2, vector3, quaternion3, l, r);
			}
			else
			{
				SendRPC("R", vector, quaternion, vector2, quaternion2, vector3, quaternion3, localGloveL.currentGesture, localGloveR.currentGesture);
			}
		}
	}

	private void EncodeSkeletalFingers(out int l, out int r)
	{
		int num = 63;
		l = 0;
		l |= Mathf.RoundToInt(localGloveL.thumbCurl * (float)num) << 24;
		l |= Mathf.RoundToInt(localGloveL.indexCurl * (float)num) << 18;
		l |= Mathf.RoundToInt(localGloveL.middleCurl * (float)num) << 12;
		l |= Mathf.RoundToInt(localGloveL.ringCurl * (float)num) << 6;
		l |= Mathf.RoundToInt(localGloveL.pinkyCurl * (float)num);
		r = 0;
		r |= Mathf.RoundToInt(localGloveR.thumbCurl * (float)num) << 24;
		r |= Mathf.RoundToInt(localGloveR.indexCurl * (float)num) << 18;
		r |= Mathf.RoundToInt(localGloveR.middleCurl * (float)num) << 12;
		r |= Mathf.RoundToInt(localGloveR.ringCurl * (float)num) << 6;
		r |= Mathf.RoundToInt(localGloveR.pinkyCurl * (float)num);
	}

	private void DecodeSkeletalFingers(int l, int r)
	{
		int num = 63;
		float thumb = (float)((l >> 24) & num) / (float)num;
		float index = (float)((l >> 18) & num) / (float)num;
		float middle = (float)((l >> 12) & num) / (float)num;
		float ring = (float)((l >> 6) & num) / (float)num;
		float pinky = (float)(l & num) / (float)num;
		remoteGloveL.SetRemoteSkeletonFingers(thumb, index, middle, ring, pinky);
		float thumb2 = (float)((r >> 24) & num) / (float)num;
		float index2 = (float)((r >> 18) & num) / (float)num;
		float middle2 = (float)((r >> 12) & num) / (float)num;
		float ring2 = (float)((r >> 6) & num) / (float)num;
		float pinky2 = (float)(r & num) / (float)num;
		remoteGloveR.SetRemoteSkeletonFingers(thumb2, index2, middle2, ring2, pinky2);
	}

	private IEnumerator SendRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(sendInterval);
		while (base.enabled)
		{
			SendPositions();
			yield return wait;
		}
	}

	private IEnumerator RemoteUpdateRoutine()
	{
		while (base.enabled)
		{
			remoteHead.localPosition = Vector3.Lerp(remoteHead.localPosition, LocalPosition(remoteHeadPos), lerpRate * Time.deltaTime);
			remoteHead.localRotation = Quaternion.Slerp(remoteHead.localRotation, LocalRotation(remoteHeadRot), lerpRate * Time.deltaTime);
			remoteHandL.localPosition = Vector3.Lerp(remoteHandL.localPosition, LocalPosition(remoteHandLPos), lerpRate * Time.deltaTime);
			remoteHandL.localRotation = Quaternion.Slerp(remoteHandL.localRotation, LocalRotation(remoteHandLRot), lerpRate * Time.deltaTime);
			remoteHandR.localPosition = Vector3.Lerp(remoteHandR.localPosition, LocalPosition(remoteHandRPos), lerpRate * Time.deltaTime);
			remoteHandR.localRotation = Quaternion.Slerp(remoteHandR.localRotation, LocalRotation(remoteHandRRot), lerpRate * Time.deltaTime);
			remoteGloveL.SetRemoteGesture(remotePoseL);
			remoteGloveR.SetRemoteGesture(remotePoseR);
			if (interactingRight && !rhInt)
			{
				UpdateRightInteractable();
			}
			if (interactingLeft && !lhInt)
			{
				UpdateLeftInteractable();
			}
			yield return null;
		}
	}

	public void SetRemoteInteractable(bool rightHand, VRInteractable vrint)
	{
		if (rightHand)
		{
			rhInt = vrint;
			UpdateRightInteractable();
		}
		else
		{
			lhInt = vrint;
			UpdateLeftInteractable();
		}
	}

	private void UpdateRightInteractable()
	{
		VRIntGlovePoser component;
		if ((bool)rhInt && (bool)(component = rhInt.GetComponent<VRIntGlovePoser>()))
		{
			interactingRight = true;
			remoteGloveR.SetLockTransform(component.lockTransform);
			remoteGloveR.SetPoseInteractable(component.interactionPose);
		}
		else
		{
			interactingRight = false;
			remoteGloveR.ClearInteractPose();
		}
	}

	private void UpdateLeftInteractable()
	{
		VRIntGlovePoser component;
		if ((bool)lhInt && (bool)(component = lhInt.GetComponent<VRIntGlovePoser>()))
		{
			interactingLeft = true;
			remoteGloveL.SetLockTransform(component.leftLockTransform);
			remoteGloveL.SetPoseInteractable(component.interactionPose);
		}
		else
		{
			interactingLeft = false;
			remoteGloveL.ClearInteractPose();
		}
	}

	[VTRPC]
	private void R(Vector3 hp, Quaternion hr, Vector3 hlp, Quaternion hlr, Vector3 hrp, Quaternion hrr, int ggL, int ggR)
	{
		remoteHeadPos = hp;
		remoteHeadRot = hr;
		remoteHandLPos = hlp;
		remoteHandLRot = hlr;
		remoteHandRPos = hrp;
		remoteHandRRot = hrr;
		remotePoseL = ggL;
		remotePoseR = ggR;
	}

	[VTRPC]
	private void S(Vector3 hp, Quaternion hr, Vector3 hlp, Quaternion hlr, Vector3 hrp, Quaternion hrr, int sL, int sR)
	{
		remoteHeadPos = hp;
		remoteHeadRot = hr;
		remoteHandLPos = hlp;
		remoteHandLRot = hlr;
		remoteHandRPos = hrp;
		remoteHandRRot = hrr;
		DecodeSkeletalFingers(sL, sR);
	}

	private Vector3 LocalPosition(Vector3 rPos)
	{
		Vector3 vector = referenceTf.TransformPoint(rPos);
		if ((bool)anchorTf)
		{
			vector = anchorTf.position + Vector3.ClampMagnitude(vector - anchorTf.position, anchorRadius);
		}
		return remoteHead.parent.InverseTransformPoint(vector);
	}

	private Quaternion LocalRotation(Quaternion rRot)
	{
		Vector3 direction = rRot * Vector3.forward;
		Vector3 direction2 = rRot * Vector3.up;
		return Quaternion.LookRotation(remoteHead.parent.InverseTransformDirection(referenceTf.TransformDirection(direction)), remoteHead.parent.InverseTransformDirection(referenceTf.TransformDirection(direction2)));
	}

	private Vector3 ReferencePosition(Transform tf)
	{
		return referenceTf.InverseTransformPoint(tf.position);
	}

	private Quaternion ReferenceRotation(Transform tf)
	{
		return Quaternion.LookRotation(referenceTf.InverseTransformDirection(tf.forward), referenceTf.InverseTransformDirection(tf.up));
	}

	[VTRPC]
	private void RPC_Colors(Vector3 suitColor, Vector3 vestColor, Vector3 strapsColor, Vector3 gSuitColor, Vector3 skinColor)
	{
		if (!base.isMine && (bool)pilotColors)
		{
			PilotColorSetup.ColorScheme colorScheme = default(PilotColorSetup.ColorScheme);
			colorScheme.suitColor = ToColor(suitColor);
			colorScheme.vestColor = ToColor(vestColor);
			colorScheme.strapsColor = ToColor(strapsColor);
			colorScheme.gSuitColor = ToColor(gSuitColor);
			colorScheme.skinColor = ToColor(skinColor);
			PilotColorSetup.ColorScheme colors = colorScheme;
			pilotColors.UpdateProperties(colors);
		}
	}

	private Color ToColor(Vector3 v)
	{
		return new Color(v.x, v.y, v.z, 1f);
	}

	private Vector3 ToVector(Color c)
	{
		return new Vector3(c.r, c.g, c.b);
	}
}
