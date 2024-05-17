
using System;
using UnityEngine;

public abstract class OVRTouchpad
    {
        // Token: 0x06000C5A RID: 3162 RVA: 0x00002549 File Offset: 0x00000749
        public static void Create()
        {
        }

        // Token: 0x06000C5B RID: 3163 RVA: 0x00049E86 File Offset: 0x00048086
        public static void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OVRTouchpad.moveAmountMouse = Input.mousePosition;
                return;
            }
            if (Input.GetMouseButtonUp(0))
            {
                OVRTouchpad.moveAmountMouse -= Input.mousePosition;
                OVRTouchpad.HandleInputMouse(ref OVRTouchpad.moveAmountMouse);
            }
        }

        // Token: 0x06000C5C RID: 3164 RVA: 0x00002549 File Offset: 0x00000749
        public static void OnDisable()
        {
        }

        // Token: 0x06000C5D RID: 3165 RVA: 0x00049EC4 File Offset: 0x000480C4
        private static void HandleInputMouse(ref Vector3 move)
        {
            if (OVRTouchpad.touchPadCallbacks == null)
            {
                return;
            }
            OVRTouchpad.OVRTouchpadCallback<OVRTouchpad.TouchEvent> ovrtouchpadCallback = OVRTouchpad.touchPadCallbacks as OVRTouchpad.OVRTouchpadCallback<OVRTouchpad.TouchEvent>;
            if (move.magnitude < OVRTouchpad.minMovMagnitudeMouse)
            {
                ovrtouchpadCallback(OVRTouchpad.TouchEvent.SingleTap);
                return;
            }
            move.Normalize();
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            {
                if (move.x > 0f)
                {
                    ovrtouchpadCallback(OVRTouchpad.TouchEvent.Left);
                    return;
                }
                ovrtouchpadCallback(OVRTouchpad.TouchEvent.Right);
                return;
            }
            else
            {
                if (move.y > 0f)
                {
                    ovrtouchpadCallback(OVRTouchpad.TouchEvent.Down);
                    return;
                }
                ovrtouchpadCallback(OVRTouchpad.TouchEvent.Up);
                return;
            }
        }

        // Token: 0x06000C5E RID: 3166 RVA: 0x00049F50 File Offset: 0x00048150
        public static void AddListener(OVRTouchpad.OVRTouchpadCallback<OVRTouchpad.TouchEvent> handler)
        {
            OVRTouchpad.touchPadCallbacks = (OVRTouchpad.OVRTouchpadCallback<OVRTouchpad.TouchEvent>)Delegate.Combine((OVRTouchpad.OVRTouchpadCallback<OVRTouchpad.TouchEvent>)OVRTouchpad.touchPadCallbacks, handler);
        }

        // Token: 0x04000E58 RID: 3672
        private static Vector3 moveAmountMouse;

        // Token: 0x04000E59 RID: 3673
        private static float minMovMagnitudeMouse = 25f;

        // Token: 0x04000E5A RID: 3674
        public static Delegate touchPadCallbacks = null;

        // Token: 0x04000E5B RID: 3675
        private static OVRTouchpadHelper touchpadHelper = new GameObject("OVRTouchpadHelper").AddComponent<OVRTouchpadHelper>();

        // Token: 0x02000252 RID: 594
        public enum TouchEvent
        {
            // Token: 0x04000E5D RID: 3677
            SingleTap,
            // Token: 0x04000E5E RID: 3678
            DoubleTap,
            // Token: 0x04000E5F RID: 3679
            Left,
            // Token: 0x04000E60 RID: 3680
            Right,
            // Token: 0x04000E61 RID: 3681
            Up,
            // Token: 0x04000E62 RID: 3682
            Down
        }

        // Token: 0x02000253 RID: 595
        // (Invoke) Token: 0x06000C61 RID: 3169
        public delegate void OVRTouchpadCallback<TouchEvent>(TouchEvent arg);
    }


