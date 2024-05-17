using UnityEngine;

public abstract class CustomTutorialObjective : MonoBehaviour
{
	public string objectiveID;

	public Transform linePointTransform;

	public virtual bool GetIsCompleted()
	{
		return false;
	}

	public virtual void OnStartObjective()
	{
	}
}
