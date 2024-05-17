using UnityEngine;

public class EditorPauser : MonoBehaviour
{
	public bool gradualResume = true;

	private bool doGradualResume;

	public void PauseEditor()
	{
	}

	private void Update()
	{
		if (doGradualResume)
		{
			if (Time.timeScale < 1f)
			{
				Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, 0.2f * Time.deltaTime);
			}
			else
			{
				doGradualResume = false;
			}
		}
	}
}
