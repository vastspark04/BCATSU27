using System;

public class AsyncOpStatus
{
	private bool _done;

	public float progress;

	public bool isDone
	{
		get
		{
			return _done;
		}
		set
		{
			if (!_done && value)
			{
				_done = true;
				this.OnFinished?.Invoke();
			}
		}
	}

	public event Action OnFinished;
}
