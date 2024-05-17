using System.Collections;
using UnityEngine;

namespace BDBugTest
{
    public class ParticleRigTestScene : MonoBehaviour
    {
        public float testLoopDistance = 300f;

        public float testSpeed = 250f;

        private IEnumerator Start()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            while (base.enabled)
            {
                rb.velocity = new Vector3(testSpeed, 0f, 0f);
                rb.position = new Vector3(0f - testLoopDistance, 2f, 0f);
                while (rb.position.x < testLoopDistance)
                {
                    rb.velocity = new Vector3(testSpeed, testSpeed / 10f * Mathf.Sin(8f * Time.time), 0f);
                    yield return null;
                }
                rb.velocity = new Vector3(0f - testSpeed, 0f, 0f);
                rb.position = new Vector3(testLoopDistance, -2f, 0f);
                while (rb.position.x > 0f - testLoopDistance)
                {
                    yield return null;
                }
            }
        }
    }
}
