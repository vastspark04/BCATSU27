using System.Collections.Generic;
using System.Threading;

public class VTTerrainWorker
{
	private object _lock = new object();

	private bool started;

	private bool stop;

	private Thread workThread;

	private ITerrainJobServer manager;

	private FastNoise noise;

	private int _maxOutputCount;

	private Queue<VTTerrainJob> outputQueue = new Queue<VTTerrainJob>();

	public void Start(ITerrainJobServer m, int noiseSeed, int maxOutputCount)
	{
		if (!started)
		{
			manager = m;
			noise = new FastNoise(noiseSeed);
			_maxOutputCount = maxOutputCount;
			started = true;
			workThread = new Thread(JobLoop);
			workThread.Start();
		}
	}

	public void Stop()
	{
		lock (_lock)
		{
			stop = true;
		}
	}

	public void SetNoiseSeed(int s)
	{
		if (noise != null)
		{
			noise.SetSeed(s);
		}
	}

	private void JobLoop()
	{
		while (true)
		{
			bool flag = true;
			while (flag)
			{
				lock (_lock)
				{
					if (stop)
					{
						return;
					}
					flag = outputQueue.Count >= _maxOutputCount;
				}
				if (flag)
				{
					Thread.Sleep(10);
				}
			}
			VTTerrainJob vTTerrainJob = null;
			while (vTTerrainJob == null)
			{
				if (manager == null)
				{
					stop = true;
				}
				if (stop)
				{
					return;
				}
				vTTerrainJob = manager.RequestJob();
				if (vTTerrainJob == null)
				{
					Thread.Sleep(10);
				}
			}
			if (vTTerrainJob == null)
			{
				continue;
			}
			vTTerrainJob.noiseModule = noise;
			vTTerrainJob.DoJob();
			lock (_lock)
			{
				if (stop)
				{
					break;
				}
				outputQueue.Enqueue(vTTerrainJob);
			}
		}
	}

	public VTTerrainJob GetJobOutput()
	{
		VTTerrainJob result = null;
		lock (_lock)
		{
			if (outputQueue.Count > 0)
			{
				return outputQueue.Dequeue();
			}
			return result;
		}
	}
}
