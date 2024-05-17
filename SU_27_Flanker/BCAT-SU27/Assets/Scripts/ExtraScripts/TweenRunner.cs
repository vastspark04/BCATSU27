using System;
using System.Collections;

namespace UnityEngine.UI.Extensions.Tweens
{
	// Token: 0x0200066C RID: 1644
	internal class TweenRunner<T> where T : struct, ITweenValue
	{
		// Token: 0x06003DD9 RID: 15833 RVA: 0x0016E651 File Offset: 0x0016C851
		private static IEnumerator Start(T tweenInfo)
		{
			if (!tweenInfo.ValidTarget())
			{
				yield break;
			}
			float elapsedTime = 0f;
			while (elapsedTime < tweenInfo.duration)
			{
				elapsedTime += (tweenInfo.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
				float floatPercentage = Mathf.Clamp01(elapsedTime / tweenInfo.duration);
				tweenInfo.TweenValue(floatPercentage);
				yield return null;
			}
			tweenInfo.TweenValue(1f);
			tweenInfo.Finished();
			yield break;
		}

		// Token: 0x06003DDA RID: 15834 RVA: 0x0016E660 File Offset: 0x0016C860
		public void Init(MonoBehaviour coroutineContainer)
		{
			this.m_CoroutineContainer = coroutineContainer;
		}

		// Token: 0x06003DDB RID: 15835 RVA: 0x0016E66C File Offset: 0x0016C86C
		public void StartTween(T info)
		{
			if (this.m_CoroutineContainer == null)
			{
				Debug.LogWarning("Coroutine container not configured... did you forget to call Init?");
				return;
			}
			if (this.m_Tween != null)
			{
				this.m_CoroutineContainer.StopCoroutine(this.m_Tween);
				this.m_Tween = null;
			}
			if (!this.m_CoroutineContainer.gameObject.activeInHierarchy)
			{
				info.TweenValue(1f);
				return;
			}
			this.m_Tween = TweenRunner<T>.Start(info);
			this.m_CoroutineContainer.StartCoroutine(this.m_Tween);
		}

		// Token: 0x04003A0B RID: 14859
		protected MonoBehaviour m_CoroutineContainer;

		// Token: 0x04003A0C RID: 14860
		protected IEnumerator m_Tween;
	}
}
