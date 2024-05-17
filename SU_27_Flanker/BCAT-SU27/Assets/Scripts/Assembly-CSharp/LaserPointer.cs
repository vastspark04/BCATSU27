using System;
using UnityEngine;

// Token: 0x02000270 RID: 624
public class LaserPointer : OVRCursor
{
    // Token: 0x17000156 RID: 342
    // (get) Token: 0x06000CEE RID: 3310 RVA: 0x0004BF93 File Offset: 0x0004A193
    // (set) Token: 0x06000CED RID: 3309 RVA: 0x0004BF5F File Offset: 0x0004A15F
    public LaserPointer.LaserBeamBehavior laserBeamBehavior
    {
        get
        {
            return this._laserBeamBehavior;
        }
        set
        {
            this._laserBeamBehavior = value;
            if (this.laserBeamBehavior == LaserPointer.LaserBeamBehavior.Off || this.laserBeamBehavior == LaserPointer.LaserBeamBehavior.OnWhenHitTarget)
            {
                this.lineRenderer.enabled = false;
                return;
            }
            this.lineRenderer.enabled = true;
        }
    }

    // Token: 0x06000CEF RID: 3311 RVA: 0x0004BF9B File Offset: 0x0004A19B
    public virtual void Awake()
    {
        this.lineRenderer = base.GetComponent<LineRenderer>();
    }

    // Token: 0x06000CF0 RID: 3312 RVA: 0x0004BFA9 File Offset: 0x0004A1A9
    public virtual void Start()
    {
        if (this.cursorVisual)
        {
            this.cursorVisual.SetActive(false);
        }
    }

    // Token: 0x06000CF1 RID: 3313 RVA: 0x0004BFC4 File Offset: 0x0004A1C4
    public override void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal)
    {
        this._startPoint = start;
        this._endPoint = dest;
        this._hitTarget = true;
    }

    // Token: 0x06000CF2 RID: 3314 RVA: 0x0004BFDB File Offset: 0x0004A1DB
    public override void SetCursorRay(Transform t)
    {
        this._startPoint = t.position;
        this._forward = t.forward;
        this._hitTarget = false;
    }

    // Token: 0x06000CF3 RID: 3315 RVA: 0x0004BFFC File Offset: 0x0004A1FC
    public virtual void LateUpdate()
    {
        this.lineRenderer.SetPosition(0, this._startPoint);
        if (this._hitTarget)
        {
            this.lineRenderer.SetPosition(1, this._endPoint);
            this.UpdateLaserBeam(this._startPoint, this._endPoint);
            if (this.cursorVisual)
            {
                this.cursorVisual.transform.position = this._endPoint;
                this.cursorVisual.SetActive(true);
                return;
            }
        }
        else
        {
            this.UpdateLaserBeam(this._startPoint, this._startPoint + this.maxLength * this._forward);
            this.lineRenderer.SetPosition(1, this._startPoint + this.maxLength * this._forward);
            if (this.cursorVisual)
            {
                this.cursorVisual.SetActive(false);
            }
        }
    }

    // Token: 0x06000CF4 RID: 3316 RVA: 0x0004C0E4 File Offset: 0x0004A2E4
    public virtual void UpdateLaserBeam(Vector3 start, Vector3 end)
    {
        if (this.laserBeamBehavior == LaserPointer.LaserBeamBehavior.Off)
        {
            return;
        }
        if (this.laserBeamBehavior == LaserPointer.LaserBeamBehavior.On)
        {
            this.lineRenderer.SetPosition(0, start);
            this.lineRenderer.SetPosition(1, end);
            return;
        }
        if (this.laserBeamBehavior == LaserPointer.LaserBeamBehavior.OnWhenHitTarget)
        {
            if (this._hitTarget)
            {
                if (!this.lineRenderer.enabled)
                {
                    this.lineRenderer.enabled = true;
                    this.lineRenderer.SetPosition(0, start);
                    this.lineRenderer.SetPosition(1, end);
                    return;
                }
            }
            else if (this.lineRenderer.enabled)
            {
                this.lineRenderer.enabled = false;
            }
        }
    }

    // Token: 0x06000CF5 RID: 3317 RVA: 0x0004BFA9 File Offset: 0x0004A1A9
    public virtual void OnDisable()
    {
        if (this.cursorVisual)
        {
            this.cursorVisual.SetActive(false);
        }
    }

    // Token: 0x04000EF8 RID: 3832
    public GameObject cursorVisual;

    // Token: 0x04000EF9 RID: 3833
    public float maxLength = 10f;

    // Token: 0x04000EFA RID: 3834
    protected LaserPointer.LaserBeamBehavior _laserBeamBehavior;

    // Token: 0x04000EFB RID: 3835
    protected Vector3 _startPoint;

    // Token: 0x04000EFC RID: 3836
    protected Vector3 _forward;

    // Token: 0x04000EFD RID: 3837
    protected Vector3 _endPoint;

    // Token: 0x04000EFE RID: 3838
    protected bool _hitTarget;

    // Token: 0x04000EFF RID: 3839
    protected LineRenderer lineRenderer;

    // Token: 0x02000271 RID: 625
    public enum LaserBeamBehavior
    {
        // Token: 0x04000F01 RID: 3841
        On,
        // Token: 0x04000F02 RID: 3842
        Off,
        // Token: 0x04000F03 RID: 3843
        OnWhenHitTarget
    }
}

