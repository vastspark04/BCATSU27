using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODManager : MonoBehaviour
{
	private Queue<LODBase> lodBases = new Queue<LODBase>();

	public Camera tcam;

	public int objectsPerFrame = 10;

	public float tcamDotFactor = 40f;

	public static LODManager instance { get; private set; }

	private void Awake()
	{
		instance = this;
		StartCoroutine(LODRoutine());
	}

	public void RegisterLODBase(LODBase lodBase)
	{
		lodBases.Enqueue(lodBase);
	}

	private IEnumerator LODRoutine()
	{
		yield return null;
		int objCount = 0;
		while (base.enabled)
		{
			while (lodBases.Count == 0)
			{
				yield return null;
			}
			LODBase lODBase = lodBases.Dequeue();
			if ((bool)lODBase)
			{
				Transform tCamTf = null;
				float tCamFov = 999f;
				Transform sCamTf = null;
				float sCamFov = 999f;
				if ((bool)tcam && tcam.enabled)
				{
					tCamTf = tcam.transform;
					tCamFov = tcam.fieldOfView;
				}
				if ((bool)FlybyCameraMFDPage.instance && FlybyCameraMFDPage.instance.isCamEnabled)
				{
					sCamTf = FlybyCameraMFDPage.instance.flybyCam.transform;
					sCamFov = FlybyCameraMFDPage.instance.flybyCam.fieldOfView;
				}
				lODBase.UpdateDist(tCamTf, tCamFov, sCamTf, sCamFov);
				lodBases.Enqueue(lODBase);
				objCount++;
				if (objCount > objectsPerFrame || objCount > lodBases.Count)
				{
					objCount = 0;
					yield return null;
					yield return new WaitForEndOfFrame();
				}
			}
		}
	}
}
