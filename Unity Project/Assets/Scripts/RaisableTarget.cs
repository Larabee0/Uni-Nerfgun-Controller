using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// This script controller the raising and sinking of a target
/// </summary>
public class RaisableTarget : MonoBehaviour
{
    public bool risen = false;

    [SerializeField] private float raiseSpeed = 1;
    [SerializeField,Tooltip("Don't set these in the editor")] private float2 raisePoints;
    [SerializeField] private Target target = null;

    public GameArbiter TargetGameArbiter { get => target.gameArbiter; set => target.gameArbiter = value; }

    private Coroutine raiseProcess = null;
    private Coroutine sinkProcess = null;

    // make sure to we are hidden when we spawn,
    // it seems awake is called as part of instantiate
    private void Awake()
    {
        Hide();
    }

    /// <summary>
    /// Called by the target. If the we are rising, stop raising right now and start sinking.
    /// </summary>
    public void OnHit()
    {
        if (raiseProcess != null)
        {
            StopCoroutine(raiseProcess);
            raiseProcess = null;
        }

        Sink();
    }

    /// <summary>
    /// If the sinkProcess is null, start the sink coroutine
    /// </summary>
    public void Sink()
    {
        sinkProcess ??= StartCoroutine(SinkCoroutine());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="raisePoints"></param>
    public void Raise(float3 raisePoints)
    {
        if (raiseProcess == null)
        {
            this.raisePoints.y = raisePoints.y;
            raisePoints.y = this.raisePoints.x;
            transform.position = raisePoints;
            risen = true;
            raiseProcess = StartCoroutine(RaiseCoroutine());
        }
    }

    /// <summary>
    /// Moves the barrier up from its spawn position to the height defined by raisePoints.y
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator RaiseCoroutine()
    {
        SetChildrenActive(true);
        for (float i = transform.position.y; i <= raisePoints.y; i += Time.deltaTime * raiseSpeed)
        {
            Vector3 pos = transform.position;
            pos.y = i;
            transform.position = pos;
            yield return null;

        }
        raiseProcess = null;
    }

    /// <summary>
    /// Moves the barrier down from its raisedPosition to the height defined by raisePoints.x.
    /// 
    /// Basically the same as RaiseCoroutine but the for loop runs backwards,
    /// and we hide and set sinkProcess to null at the end.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator SinkCoroutine()
    {
        for (float i = transform.position.y; i >= raisePoints.x; i -= Time.deltaTime * raiseSpeed)
        {
            Vector3 pos = transform.position;
            pos.y = i;
            transform.position = pos;
            yield return null;

        }
        sinkProcess = null;
        Hide();
    }

    /// <summary>
    /// Sets risen to false then hides all children
    /// </summary>
    private void Hide()
    {
        risen = false;
        SetChildrenActive(false);
    }

    /// <summary>
    /// Sets the children of this gameObjecto the active value
    /// </summary>
    /// <param name="active">Child Active State</param>
    private void SetChildrenActive(bool active)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(active);
        }
    }

}
