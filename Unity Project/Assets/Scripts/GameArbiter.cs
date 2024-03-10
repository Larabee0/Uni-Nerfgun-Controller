using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
//using UnityEngine.UI;

/// <summary>
/// This runs the game and keeps track of and displays the player stats.
/// </summary>
public class GameArbiter : MonoBehaviour
{
    // hehehe, everything is private (apart form one public method)
    //[SerializeField] private Text wideView;
    //[SerializeField] private Text scopeView;

    [SerializeField] private VirtualNerfgun virtualNerfgun;
    [Space(20)]
    [Header("Targets")]
    [Header("Static Targets")]
    [SerializeField] private float staticTargetStartDelay = 10f;
    public float newStaticTargetTime = 3f;
    [SerializeField] private Target staticTargetPrefab;
    [SerializeField] private TargetPlane[] targetPlanes;
    [Header("Raiseable Targets")]
    [SerializeField] private float raisableTargetStartDelay = 15f;
    public float newRaisableTargetTime = 6f;
    [SerializeField] private TargetPlane raiseAbleTargetPlane;
    [SerializeField] private RaisableTarget raisableTargetPrefab;
    [SerializeField] private List<RaisableTarget> spawnedRaisableTargets;

    [Space(20)]
    [Header("Random Options")]
    [SerializeField] private bool randomSeed = false;
    [SerializeField] private uint seed = 1;
    // why are there 3 randoms, System.Random, UnityEngine.Random Unity.Mathematics.Random..pain
    private Unity.Mathematics.Random random;

    [Space(20)]
    [Header("Playe Stats")]
    [SerializeField] private int totalHits = 0;
    // hit accuracy stats
    // x = worst accuracy, y = avg accuracy, z = best accuracy, w = averaging value
    [SerializeField] private float4 accuracy;

    // barrier up time stats
    // x = shortest up time, y = avg up time, z = longest up time, w = averaging value
    [SerializeField] private float4 upTime;

    [SerializeField] private int score;
    private int TotalShotsFired => virtualNerfgun.TotalShotsFired;

    private Coroutine staticTargets;
    private Coroutine raisableTargets;

    private float totalTime = 0f;
    private float timeRemaining = 0f;
    private bool gameEnded = true;

    public void RestartGame()
    {
        timeRemaining = totalTime= UIController.Instance.GameTime;
        score = 0;
        totalHits = 0;
        accuracy = new(100, 0, 0, 0);
        upTime = new(0, 0, 1000, 0);
        ExternalEndGame();
        if (randomSeed || seed == 0)
        {
            seed = (uint)UnityEngine.Random.Range(0, int.MaxValue);
        }
        random = new Unity.Mathematics.Random(seed);
        staticTargets =StartCoroutine(StaticTargetLoop());
        raisableTargets = StartCoroutine(RaisableTargetLoop());
        gameEnded = false;
    }

    public void ExternalEndGame()
    {
        gameEnded = true;
        if (staticTargets != null)
        {
            StopCoroutine(staticTargets);
            for (int i = 0; i < targetPlanes.Length; i++)
            {
                targetPlanes[i].Clear();
            }
            staticTargets = null;
        }
        if (raisableTargets != null)
        {
            StopCoroutine(raisableTargets);
            spawnedRaisableTargets.ForEach(target => Destroy(target.gameObject));
            raisableTargets = null;
        }
    }

    private void EndGame()
    {
        StatRecord record = new()
        {
            scorePerSecond = (float)score / (float)totalTime,
            totalScore = score,
            time = totalTime,
            accuracy = (float)totalHits / (float)TotalShotsFired * 100f,
            hits = totalHits,
            shots = TotalShotsFired,
            averageTargetUpTime = upTime.y
        };
        gameEnded = true;
        UIController.Instance.ResetGame();
        UIController.Instance.ShowStats(record);
    }

    /// <summary>
    /// This is kind of horrendous.
    /// Only contained in here are string formatting for the player stats that are just spat out to a text box on screen
    /// </summary>
    private void Update()
    {
        if (gameEnded)
        {
            return;
        }
        timeRemaining -= Time.deltaTime;

        UIController.Instance.DisplayedTime = timeRemaining;
        UIController.Instance.DisplayedScore = score.ToString("D3");
        if (timeRemaining < 0)
        {
            EndGame();
        }
        return;
        // shooting accuracy
        // abs accuracy - how many shots did hte player fire vs how many hits to targets did they make
        // all other accuracy is how "on target" they were of the those shots that hit.
        string absAccuracy = string.Format("{0}% - Abs Accuracy", ((float)totalHits / (float)TotalShotsFired * 100f).ToString("0.00"));
        string avgAccuracy = string.Format("{0}% - Avg Target Accuracy", accuracy.y.ToString("0.00"));
        string bestAccuracy = string.Format("{0}% - Best Hit", accuracy.z.ToString("0.00"));
        string worstAccuracy = string.Format("{0}% - Worst Hit", accuracy.x.ToString("0.00"));
        if (totalHits == 0) // stops incorrect stat reporting before the player makes any hits
        {
            worstAccuracy = string.Format("{0}% - Worst Hit", 0.0f.ToString("0.00")); // 0.00 formatter - force this float to have at most 2 decimal places for display
        }
        if (TotalShotsFired == 0) // prevents floatNaN due to divide by 0 before any shots are fired.
        {
            absAccuracy = string.Format("{0}% - Abs Accuracy", 0.0f.ToString("0.00"));
        }

        // total game time
        // TimeSpan provides a convientant way to format time.
        TimeSpan time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
        // D2/D3 formatter force the int to have 2 or 3 digits => 1 becomes 001
        string totalTime = string.Format("{0}:{1}.{2} - Time", time.Minutes.ToString("D2"), time.Seconds.ToString("D2"), time.Milliseconds.ToString("D3"));


        // barrier up times
        TimeSpan avgUpTime = TimeSpan.FromSeconds(upTime.y);
        string avgbarrierUpTime = string.Format("{0}:{1}.{2} - Avg Target Up Time", avgUpTime.Minutes.ToString("D2"), avgUpTime.Seconds.ToString("D2"), avgUpTime.Milliseconds.ToString("D3"));

        TimeSpan longestUpTime = TimeSpan.FromSeconds(upTime.z);
        if (totalHits == 0) // when no hits are made upTime.z is 1000
        {
            longestUpTime = TimeSpan.FromSeconds(0);
        }
        string longestBarrierUpTime = string.Format("{0}:{1}.{2} - Best Target Up Time", longestUpTime.Minutes.ToString("D2"), longestUpTime.Seconds.ToString("D2"), longestUpTime.Milliseconds.ToString("D3"));
        TimeSpan shortestUpTime = TimeSpan.FromSeconds(upTime.x);
        string shortestBarrierUpTime = string.Format("{0}:{1}.{2} - Worst Target Up Time", shortestUpTime.Minutes.ToString("D2"), shortestUpTime.Seconds.ToString("D2"), shortestUpTime.Milliseconds.ToString("D3"));

        //wideView.text = string.Format("{8} - Score\n{4}\n{5}\n{6}\n{7}\n{0}\n{1}\n{2}\n{3}", totalTime, avgbarrierUpTime, longestBarrierUpTime, shortestBarrierUpTime, absAccuracy, avgAccuracy, bestAccuracy, worstAccuracy, score);
    }

    /// <summary>
    /// Continous coroutine to put up random static targets on the far walls of the environment.
    /// This picks a random plane on which to spawn a target, then asks that plane to pick it a random position.
    /// It spawns a new target at that position and adds it to the planes target queue.
    /// If the plane has the max number of targets already it removes the one at the front of the queue.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator StaticTargetLoop()
    {
        yield return new WaitForSeconds(staticTargetStartDelay);
        while (true)
        {
            // pick plane
            int targetPlaneIndex = random.NextInt(0, targetPlanes.Length);
            TargetPlane targetPlane = targetPlanes[targetPlaneIndex];

            // pick random position, spawn target and set.
            float3 position = targetPlane.GetRandomPoint(ref random);
            Target newTarget = Instantiate(staticTargetPrefab, position, Quaternion.identity);
            newTarget.gameArbiter = this;
            targetPlane.AddTarget(newTarget);

            yield return new WaitForSeconds(newStaticTargetTime);
        }
    }

    /// <summary>
    /// Main loop for raising and lowering barriers over time.
    /// It picks a barrier at random from the list and tries to raise it.
    /// If the barrier it picks has already been raised it tells it to sink.
    /// 
    /// This works slightly differently to the above method as the raisable barriers are persistant.
    /// The target plane doesn't keep track of them, we do that locally in a list.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator RaisableTargetLoop()
    {
        // spawn all the rasiable barriers now before the main loop starts.
        for (int i = 0; i < raiseAbleTargetPlane.MaxTargets; i++)
        {
            spawnedRaisableTargets.Add(Instantiate(raisableTargetPrefab));
            spawnedRaisableTargets[^1].TargetGameArbiter = this; // funny indexer lets us access the last item in the list.
        }


        yield return new WaitForSeconds(raisableTargetStartDelay);
        while (true)
        {
            // pick raisable target. I really want to inline "raisableTargetIndex".
            int raisableTargetIndex = random.NextInt(0, spawnedRaisableTargets.Count);
            RaisableTarget target = spawnedRaisableTargets[raisableTargetIndex];

            // logic to determine if we should Raise or Sink the target, we don't need to pick a position if we Sink it
            if (target.risen)
            {
                target.Sink();
            }
            else
            {
                float3 pos = raiseAbleTargetPlane.GetRandomPoint(ref random, 2f);
                target.Raise(pos);
            }


            yield return new WaitForSeconds(newRaisableTargetTime);
        }
    }

    /// <summary>
    /// Called by <see cref="Target.OnCollisionEnter"/> to update the accuracy and upTime stats.
    /// </summary>
    /// <param name="accuracy"> how accurate hte player was in hitting the target (%) </param>
    /// <param name="upTime"> Number of seconds the target has been active for </param>
    public void OnHit(float accuracy, float upTime)
    {
        totalHits++;
        this.accuracy.w += accuracy; // w = averaging value
        this.accuracy.y = this.accuracy.w / totalHits; // y = avg accuracy
        this.accuracy.x = accuracy < this.accuracy.x ? accuracy : this.accuracy.x; // x = worst accuracy
        this.accuracy.z = accuracy > this.accuracy.z ? accuracy : this.accuracy.z; // z = best accuracy

        this.upTime.w += upTime; //  w = averaging value
        this.upTime.y = this.upTime.w / totalHits; // y = avg up time
        this.upTime.x = upTime > this.upTime.x ? upTime : this.upTime.x; // x = shortest up time
        this.upTime.z = upTime < this.upTime.z ? upTime : this.upTime.z; // z = longest up time,

        score += (int)(accuracy - upTime);
    }
}
