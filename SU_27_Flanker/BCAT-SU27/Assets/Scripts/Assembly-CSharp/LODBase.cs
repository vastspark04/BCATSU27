using UnityEngine;
using UnityEngine.Events;

public class LODBase : MonoBehaviour
{
	private FloatEvent OnUpdateDist;

	public float sqrDist { get; private set; }

	private void Awake()
	{
		OnUpdateDist = new FloatEvent();
	}

	private void Start()
	{
		if (!LODManager.instance)
		{
			base.enabled = false;
		}
		else
		{
			LODManager.instance.RegisterLODBase(this);
		}
	}

	public void UpdateDist(Transform tCamTf, float tCamFov, Transform sCamTf, float sCamFov)
	{
		if ((bool)VRHead.instance)
		{
			float num = (base.transform.position - VRHead.instance.transform.position).sqrMagnitude;
			bool flag = false;
			float num2 = 0f;
			if (tCamFov < 180f && Vector3.Angle(tCamTf.forward, base.transform.position - tCamTf.position) < tCamFov + 5f)
			{
				num2 = 1f + LODManager.instance.tcamDotFactor / tCamFov;
				flag = true;
			}
			if (sCamFov < 180f && Vector3.Angle(sCamTf.forward, base.transform.position - sCamTf.position) < sCamFov + 5f)
			{
				num2 = Mathf.Max(num2, 1f + LODManager.instance.tcamDotFactor / sCamFov);
				flag = true;
			}
			if (flag)
			{
				num /= num2;
			}
			sqrDist = num;
			OnUpdateDist.Invoke(num);
		}
	}

	public void AddListener(UnityAction<float> call)
	{
		OnUpdateDist.AddListener(call);
	}

	public void RemoveListener(UnityAction<float> call)
	{
		OnUpdateDist.RemoveListener(call);
	}
}
