using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEngine;

public static class AsyncSerializer
{
	public class AsyncSerializerBehaviour : MonoBehaviour
	{
		private object lockObj = new object();

		private bool _isDone;

		private Thread thread;

		public AsyncOpStatus SerializeObject(object o, string outputPath)
		{
			AsyncOpStatus asyncOpStatus = new AsyncOpStatus();
			asyncOpStatus.isDone = false;
			asyncOpStatus.progress = 0f;
			StartCoroutine(UpdateStatusRoutine(asyncOpStatus));
			thread = new Thread(SerializeOnThread);
			thread.Start(new object[2] { o, outputPath });
			return asyncOpStatus;
		}

		private IEnumerator UpdateStatusRoutine(AsyncOpStatus status)
		{
			while (!status.isDone)
			{
				lock (lockObj)
				{
					status.isDone = _isDone;
				}
				yield return null;
			}
			Object.Destroy(base.gameObject);
		}

		private void SerializeOnThread(object objectAndPath)
		{
			object[] obj = (object[])objectAndPath;
			object graph = obj[0];
			string path = (string)obj[1];
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			using (FileStream stream = new FileStream(path, FileMode.Create))
			{
				using GZipStream serializationStream = new GZipStream(stream, CompressionMode.Compress);
				binaryFormatter.Serialize(serializationStream, graph);
			}
			lock (lockObj)
			{
				_isDone = true;
			}
		}
	}

	public class AsyncDeserializerBehaviour : MonoBehaviour
	{
		private object lockObj = new object();

		private bool _isDone;

		private object _output;

		private Thread thread;

		public AsyncOpStatusOutput DeserializeObject(string path)
		{
			AsyncOpStatusOutput asyncOpStatusOutput = new AsyncOpStatusOutput();
			asyncOpStatusOutput.isDone = false;
			asyncOpStatusOutput.progress = 0f;
			asyncOpStatusOutput.output = null;
			StartCoroutine(UpdateStatusRoutine(asyncOpStatusOutput));
			thread = new Thread(DeserializeOnThread);
			thread.Start(path);
			return asyncOpStatusOutput;
		}

		private IEnumerator UpdateStatusRoutine(AsyncOpStatusOutput status)
		{
			while (!status.isDone)
			{
				lock (lockObj)
				{
					status.isDone = _isDone;
					if (_isDone)
					{
						status.output = _output;
					}
				}
				yield return null;
			}
			Object.Destroy(base.gameObject);
		}

		private void DeserializeOnThread(object pathObj)
		{
			string path = (string)pathObj;
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			object output = null;
			using (FileStream serializationStream = new FileStream(path, FileMode.Open))
			{
				output = binaryFormatter.Deserialize(serializationStream);
			}
			lock (lockObj)
			{
				_isDone = true;
				_output = output;
			}
		}
	}

	public static AsyncOpStatus SerializeObject(object o, string outputPath)
	{
		return new GameObject("AsyncSerializer").AddComponent<AsyncSerializerBehaviour>().SerializeObject(o, outputPath);
	}

	public static AsyncOpStatusOutput DeserializeObject(string path)
	{
		return new GameObject("AsyncDeserializer").AddComponent<AsyncDeserializerBehaviour>().DeserializeObject(path);
	}
}
