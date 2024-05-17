using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VTNetworking
{
	public class VTNMessageQueue
	{
		public class PooledBuffer
		{
			public VTNMessageHeaders messageHeader;

			public byte[] buffer;

			public int contentLength;

			public bool checkedOut;

			public int size => buffer.Length;
		}

		private byte[] headerBuffer = new byte[1];

		private Queue<PooledBuffer> messageQueue = new Queue<PooledBuffer>();

		private List<PooledBuffer> bufferPool = new List<PooledBuffer>();

		private int totalBufferPoolSize;

		private int totalBufferPoolCount;

		public int MessageQueueCount => messageQueue.Count;

		public bool Client_GetQueuedMessage(out PooledBuffer pb)
		{
			if (messageQueue.Count > 0)
			{
				pb = messageQueue.Dequeue();
				return true;
			}
			pb = null;
			return false;
		}

		public bool HasQueuedMessage()
		{
			return messageQueue.Count > 0;
		}

		public void Client_OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
		{
			int num = size - 1;
			PooledBuffer pooledBuffer = GetPooledBuffer(num);
			Marshal.Copy(data, headerBuffer, 0, 1);
			Marshal.Copy(data + 1, pooledBuffer.buffer, 0, num);
			pooledBuffer.messageHeader = (VTNMessageHeaders)headerBuffer[0];
			pooledBuffer.contentLength = num;
			messageQueue.Enqueue(pooledBuffer);
		}

		private PooledBuffer GetPooledBuffer(int minSize)
		{
			int num = 0;
			if (bufferPool.Count > 0)
			{
				num = BinarySearchStartIndex(minSize);
				for (int i = num; i < bufferPool.Count; i++)
				{
					if (bufferPool[i].size > minSize && !bufferPool[i].checkedOut)
					{
						bufferPool[i].checkedOut = true;
						return bufferPool[i];
					}
				}
			}
			PooledBuffer pooledBuffer = new PooledBuffer();
			pooledBuffer.buffer = new byte[minSize];
			totalBufferPoolSize += minSize;
			totalBufferPoolCount++;
			Debug.Log($"VTNMessageQueue growing bufferPool size.  Buffer count = {totalBufferPoolCount}, total size = {(float)totalBufferPoolSize / 1000f}kb");
			bool flag = false;
			if (bufferPool.Count > 0)
			{
				for (int j = num; j < bufferPool.Count; j++)
				{
					if (bufferPool[j].size > minSize)
					{
						bufferPool.Insert(j, pooledBuffer);
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				bufferPool.Add(pooledBuffer);
			}
			pooledBuffer.checkedOut = true;
			return pooledBuffer;
		}

		private int BinarySearchStartIndex(int minSize)
		{
			int num = bufferPool.Count / 2;
			int num2 = 0;
			int num3 = bufferPool.Count - 1;
			while (true)
			{
				if (bufferPool[num].size == minSize)
				{
					return num;
				}
				if (num3 - num2 < 2)
				{
					break;
				}
				if (bufferPool[num].size < minSize)
				{
					num2 = num;
					num = (num2 + num3) / 2;
				}
				else
				{
					num3 = num;
					num = (num2 + num3) / 2;
				}
			}
			return (bufferPool[num2].size >= minSize) ? num2 : num3;
		}

		public void ReturnPooledBuffer(PooledBuffer pb)
		{
			pb.checkedOut = false;
		}

		private int PBSort(PooledBuffer a, PooledBuffer b)
		{
			return a.size.CompareTo(b.size);
		}
	}
}
