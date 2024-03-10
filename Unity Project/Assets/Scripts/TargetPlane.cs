using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine;

/// <summary>
/// class used for calculating a random point on a given plane defined by 2 vector2s (float2)
/// This has several as it is also used for the raisable barriers and can randomly determine the height they raise to.
/// </summary>
[Serializable]
public class TargetPlane
{
    [SerializeField] private float2 cornerOne; // plane bottom left corner
    [SerializeField] private float2 cornerTwo; // plane top right corner
    [SerializeField] private Axis depthAxis = Axis.Z; // depth axis
    [SerializeField] private float depth; // depth value (min depth)
    [SerializeField] private bool randomDepth; // random depth toggle
    [SerializeField] private float depth2; // max depth if random depth enabled.

    [SerializeField] private int maxTargets; // maxmimum number of targets for this plane.
    public int MaxTargets => maxTargets;

    // targets assigned to this plane.
    private Queue<Target> targets = new();

    // last position this plane generated.
    [SerializeField] private float3 lastPosition;


    /// <summary>
    /// Gets a random point on the plane.
    /// </summary>
    /// <param name="random">Random number generator provided by <see cref="GameArbiter"/></param>
    /// <param name="minDst">How close the position can be to <see cref="lastPosition"/></param>
    /// <returns>Position in 3d space on the plane</returns>
    public float3 GetRandomPoint(ref Unity.Mathematics.Random random, float minDst = 1f)
    {

        // depth is static or between depth and depth2
        float localDepth = depth;
        if (randomDepth)
        {
            localDepth = random.NextFloat(depth, depth2);
        }

        // this is why Unity.Mathematics.Random, ability to generate a vector2 on one line.
        float2 pos = random.NextFloat2(cornerOne, cornerTwo);

        // get the world assumbed world position based on values generated.
        float3 worldPos = GetWorldPos(pos, localDepth);

        // if the new position is too close to the lase position, try again.
        while (math.distance(worldPos, lastPosition) < minDst)
        {
            pos = random.NextFloat2(cornerOne, cornerTwo);
            worldPos = GetWorldPos(pos, localDepth);
        }

        lastPosition = worldPos;

        return GetWorldPos(pos, localDepth);
    }

    /// <summary>
    /// Based on the value of <see cref="depthAxis"/> this turns the plane xy and local depth into a point in 3d space.
    /// </summary>
    /// <param name="pos">xy position on the plane</param>
    /// <param name="localDepth">depth at which the point is at on that plane</param>
    /// <returns>3d position</returns>
    private float3 GetWorldPos(float2 pos, float localDepth)
    {
        return depthAxis switch
        {
            Axis.Z => new float3(pos, localDepth),
            Axis.Y => new float3(pos.x, localDepth, pos.y),
            Axis.X => new float3(localDepth, pos.x, pos.y),
            _ => new float3(pos, localDepth),
        };
    }

    /// <summary>
    /// Only for static targets <see cref="GameArbiter.StaticTargetLoop"/>
    /// This adds the given target to a queue, removing the oldest target and destroying it.
    /// </summary>
    /// <param name="target">New target to add</param>
    public void AddTarget(Target target)
    {
        // only start destroying old targets when we go above maxTargets
        if (targets.Count > maxTargets)
        {
            Target oldTarget = targets.Dequeue();
            if (oldTarget != null)
            {
                oldTarget.DestroySelf();
            }
        }
        targets.Enqueue(target);
    }

    /// <summary>
    /// Remove the given target from the queue if it exists.
    /// </summary>
    /// <param name="target">Target to remove</param>
    public void RemoveTarget(Target target)
    {
        if (targets.Contains(target))
        {
            List<Target> targets = this.targets.ToList();
            targets.Remove(target);
            this.targets = new Queue<Target>(targets);
        }
    }

    public void Clear()
    {
        while(targets.Count > 0)
        {
            Target oldTarget = targets.Dequeue();
            if (oldTarget != null)
            {
                oldTarget.DestroySelf();
            }
        }
    }
}
