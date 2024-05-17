using System;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions.Tweens
{
	// Token: 0x0200066A RID: 1642
	public struct FloatTween : ITweenValue
	{
		// Token: 0x170006DC RID: 1756
		// (get) Token: 0x06003DC5 RID: 15813 RVA: 0x0016E573 File Offset: 0x0016C773
		// (set) Token: 0x06003DC6 RID: 15814 RVA: 0x0016E57B File Offset: 0x0016C77B
		public float startFloat
		{
			get
			{
				return this.m_StartFloat;
			}
			set
			{
				this.m_StartFloat = value;
			}
		}

		// Token: 0x170006DD RID: 1757
		// (get) Token: 0x06003DC7 RID: 15815 RVA: 0x0016E584 File Offset: 0x0016C784
		// (set) Token: 0x06003DC8 RID: 15816 RVA: 0x0016E58C File Offset: 0x0016C78C
		public float targetFloat
		{
			get
			{
				return this.m_TargetFloat;
			}
			set
			{
				this.m_TargetFloat = value;
			}
		}

		// Token: 0x170006DE RID: 1758
		// (get) Token: 0x06003DC9 RID: 15817 RVA: 0x0016E595 File Offset: 0x0016C795
		// (set) Token: 0x06003DCA RID: 15818 RVA: 0x0016E59D File Offset: 0x0016C79D
		public float duration
		{
			get
			{
				return this.m_Duration;
			}
			set
			{
				this.m_Duration = value;
			}
		}

		// Token: 0x170006DF RID: 1759
		// (get) Token: 0x06003DCB RID: 15819 RVA: 0x0016E5A6 File Offset: 0x0016C7A6
		// (set) Token: 0x06003DCC RID: 15820 RVA: 0x0016E5AE File Offset: 0x0016C7AE
		public bool ignoreTimeScale
		{
			get
			{
				return this.m_IgnoreTimeScale;
			}
			set
			{
				this.m_IgnoreTimeScale = value;
			}
		}

		// Token: 0x06003DCD RID: 15821 RVA: 0x0016E5B7 File Offset: 0x0016C7B7
		public void TweenValue(float floatPercentage)
		{
			if (!this.ValidTarget())
			{
				return;
			}
			this.m_Target.Invoke(Mathf.Lerp(this.m_StartFloat, this.m_TargetFloat, floatPercentage));
		}

		// Token: 0x06003DCE RID: 15822 RVA: 0x0016E5DF File Offset: 0x0016C7DF
		public void AddOnChangedCallback(UnityAction<float> callback)
		{
			if (this.m_Target == null)
			{
				this.m_Target = new FloatTween.FloatTweenCallback();
			}
			this.m_Target.AddListener(callback);
		}

		// Token: 0x06003DCF RID: 15823 RVA: 0x0016E600 File Offset: 0x0016C800
		public void AddOnFinishCallback(UnityAction callback)
		{
			if (this.m_Finish == null)
			{
				this.m_Finish = new FloatTween.FloatFinishCallback();
			}
			this.m_Finish.AddListener(callback);
		}

		// Token: 0x06003DD0 RID: 15824 RVA: 0x0016E621 File Offset: 0x0016C821
		public bool GetIgnoreTimescale()
		{
			return this.m_IgnoreTimeScale;
		}

		// Token: 0x06003DD1 RID: 15825 RVA: 0x0016E629 File Offset: 0x0016C829
		public float GetDuration()
		{
			return this.m_Duration;
		}

		// Token: 0x06003DD2 RID: 15826 RVA: 0x0016E631 File Offset: 0x0016C831
		public bool ValidTarget()
		{
			return this.m_Target != null;
		}

		// Token: 0x06003DD3 RID: 15827 RVA: 0x0016E63C File Offset: 0x0016C83C
		public void Finished()
		{
			if (this.m_Finish != null)
			{
				this.m_Finish.Invoke();
			}
		}

		// Token: 0x04003A05 RID: 14853
		private float m_StartFloat;

		// Token: 0x04003A06 RID: 14854
		private float m_TargetFloat;

		// Token: 0x04003A07 RID: 14855
		private float m_Duration;

		// Token: 0x04003A08 RID: 14856
		private bool m_IgnoreTimeScale;

		// Token: 0x04003A09 RID: 14857
		private FloatTween.FloatTweenCallback m_Target;

		// Token: 0x04003A0A RID: 14858
		private FloatTween.FloatFinishCallback m_Finish;

		// Token: 0x02000CA2 RID: 3234
		public class FloatTweenCallback : UnityEvent<float>
		{
		}

		// Token: 0x02000CA3 RID: 3235
		public class FloatFinishCallback : UnityEvent
		{
		}
	}
}
