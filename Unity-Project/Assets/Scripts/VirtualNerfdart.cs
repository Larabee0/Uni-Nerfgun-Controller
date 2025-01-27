using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// little script for moving and destroying nerfgun projectile.
/// </summary>
public class VirtualNerfdart : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private Rigidbody rb;
    
    /// <summary>
    /// When spanwed, we it a kick and call destroy for 30 seconds from now.
    /// </summary>
    void Start()
    {
        rb.AddForce(transform.forward*force, ForceMode.Impulse);
        Destroy(gameObject,30f);
    }

    /// <summary>
    /// if we hit something call destroy in 1 second
    /// </summary>
    /// <param name="collision">Unity Collision Data</param>
    private void OnCollisionEnter(Collision collision)
    {
        rb.useGravity = true;
        Destroy(gameObject,1f);
    }
}
