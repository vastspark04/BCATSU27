using UnityEngine;

public class TutLineTarget : MonoBehaviour
{
	[SerializeField]
	private string _targetName;

	public string targetName
	{
		get
		{
			if (string.IsNullOrEmpty(_targetName))
			{
				return base.gameObject.name;
			}
			return _targetName;
		}
	}
}
