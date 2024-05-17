using UnityEngine;

public abstract class MissileGuidanceUnit : MonoBehaviour
{
	private bool _guidanceEnabled;

	private Missile _missile;

	public bool guidanceEnabled => _guidanceEnabled;

	public Missile missile => _missile;

	public void BeginGuidance(Missile m)
	{
		_guidanceEnabled = true;
		_missile = m;
		OnBeginGuidance();
	}

	protected virtual void OnBeginGuidance()
	{
	}

	public virtual Vector3 GetGuidedPoint()
	{
		return Vector3.zero;
	}

	public virtual void SaveToQuicksaveNode(ConfigNode qsNode)
	{
		qsNode.SetValue("_guidanceEnabled", _guidanceEnabled);
	}

	public virtual void LoadFromQuicksaveNode(Missile m, ConfigNode qsNode)
	{
		_guidanceEnabled = qsNode.GetValue<bool>("_guidanceEnabled");
		_missile = m;
	}
}
