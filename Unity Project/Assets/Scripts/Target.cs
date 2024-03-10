using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic target script.
/// when something hits it, it works out how accurate the player was in hitting it, logs the stats
/// then maybe destroys itself <see cref="destroyOnHit"/>
/// </summary>
public class Target : MonoBehaviour
{
    public GameArbiter gameArbiter;
    private float startTime = 0.0f;
    [SerializeField, Tooltip("how far from the a hit is still considered")] private float minDistance = 0.7f;
    [SerializeField] private bool destroyOnHit = false;

    [Header("Optional")]
    public TargetPlane plane; // target plane we are part of
    [SerializeField] private RaisableTarget raisableTarget; // raisable barrier we are attached to.

    // on enable so this works with the raisable barriers which disable when fully down
    private void OnEnable()
    {
        startTime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// When the physics system notifys something hit us we work out how accurate it was using some maths
    /// inform the game arbiter of this information and then destroy ourselves, lower the target or do nothing.
    /// </summary>
    /// <param name="collision">Unity Collision input</param>
    private void OnCollisionEnter(Collision collision)
    {
        float dst = Vector3.Distance(transform.position, collision.contacts[0].point);
        if (dst > minDistance) // too far off? no hit for you.
        {
            return;
        }

        // basic accuracy % closer to the center of hte target the more towards 1 InverseLerp returns
        // multiply by 100 and it works good enough as % accuracy.
        float accuracy = Mathf.InverseLerp(minDistance, 0, dst) * 100;

        // upTime, how long the target has been enabled/spawned/raised for.
        // shooting a target sooner to the startTime suggest faster reaction so more points.
        float upTime = Time.realtimeSinceStartup - startTime;
        gameArbiter.OnHit(accuracy, upTime);

        if (destroyOnHit)
        {
            DestroySelf();
        }

        // Intellisense says Unity objects should not use null propagation, otherwise I would.
        if (raisableTarget != null)
        {
            raisableTarget.OnHit();
        }
    }

    /// <summary>
    /// Used to destroy the target.
    /// If target has a plane assigned, make sure we remove ourselves it
    /// </summary>
    public void DestroySelf()
    {
        plane?.RemoveTarget(this);
        Destroy(gameObject);
    }
}
