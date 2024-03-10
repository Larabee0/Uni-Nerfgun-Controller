using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class for controlling the gun in the virtual environment.
/// This can be controlled by the keyboard and mouse in the editor.
/// Otherwise this is designed to work with the physicalNerfgun interafaced from <see cref="GunCommunication"/>
/// </summary>
public class VirtualNerfgun : MonoBehaviour
{
    [SerializeField] GunCommunication physicalNerfgun;
    [SerializeField] private GameObject dartPrefab;
    [SerializeField] private Transform dartSpawnPoint;
    [SerializeField] private MeshFilter laserPointerMesh;
    [SerializeField] private float virtualGunCoolDown=0.5f;
    [SerializeField] private bool enableLaserPointer;

    private Mesh mesh;
    private readonly Vector3[] meshVertices = new Vector3[2];
    private int burstShotsFired = 0;
    private int totalShotsFired;
    public int TotalShotsFired => totalShotsFired;
    private Coroutine virtualCoolDownProcess;

    /// <summary>
    /// Intialise the laser pointer mesh
    /// </summary>
    private void Start()
    {
        mesh = new Mesh();
        mesh.SetVertices(meshVertices);
        mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
        laserPointerMesh.mesh = mesh;
    }

    /// <summary>
    /// The physical Nerfgun has an event for when the gun fires.
    /// Subscribe to it.
    /// </summary>
    private void OnEnable()
    {
        physicalNerfgun.OnNerfgunFired += FireGun;
    }

    /// <summary>
    /// When not in physicalMode (physical mode is when the gun fires for real)
    /// The event will not be triggered and it is the responsiblility of the program
    /// to determine the burst state & limit the shots.
    /// 
    /// The laser pointer mesh needs to be updated every frame as well.
    /// </summary>
    private void Update()
    {
#if UNITY_EDITOR
        if(Input.GetMouseButtonDown(0))
        {
            FireGun();
        }
#endif
        if (!physicalNerfgun.PhysicalGunMode)
        {
            // S2 is the main trigger switch of the physical gun
            /// The Gun must have S2 be pressed and <see cref="NerfgunState.BurstLockOut(int)"/> must return true
            // and fire the gun if the coolDownProcess must have finished in order to fire.
            if (physicalNerfgun.State.S2 && physicalNerfgun.State.BurstLockOut(burstShotsFired) && virtualCoolDownProcess == null)
            {
                FireGun();
                virtualCoolDownProcess = StartCoroutine(VirtualCoolDown());
            }
            else if (!physicalNerfgun.State.S2) // when S2 is realsed reset the shots to 0
            {
                burstShotsFired = 0;
            }
        }
        UpdateLaserPointer();
    }

    /// <summary>
    /// Cool down coroutine for the virtual nerfgun, waits the duration <see cref="virtualCoolDownProcess"/>
    /// then sets virtualCoolDownProcess to null.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator VirtualCoolDown()
    {
        yield return new WaitForSeconds(virtualGunCoolDown);

        virtualCoolDownProcess = null;
    }

    /// <summary>
    /// Hopefully fair self explanatory..
    /// burstShotsFired is only used in virtualMode
    /// totalShotsFired is used for the stat calculations <see cref="GameArbiter"/>
    /// </summary>
    private void FireGun()
    {
        burstShotsFired++;
        totalShotsFired++;
        Instantiate(dartPrefab, dartSpawnPoint.transform.position, dartSpawnPoint.transform.rotation);
    }

    /// <summary>
    /// This draws and corrects the perspective for the line.
    /// The line is determined by making a raycast, the lines ends at the raycast hit point
    /// or 100 units along the ray if nothing is hit.
    /// 
    /// The start point is know and the end point is the rayCast hitPoint, this is simply stuck into a 2 vertex mesh
    /// set to line topology to draw a line.
    /// </summary>
    private void UpdateLaserPointer()
    {
        if (enableLaserPointer)
        {
            Ray ray = new(dartSpawnPoint.transform.position, dartSpawnPoint.transform.forward);
            meshVertices[0] = dartSpawnPoint.InverseTransformPoint(ray.origin);
            meshVertices[1] = ray.GetPoint(100f);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            {
                meshVertices[1] = hitInfo.point;
            }
            meshVertices[1] = dartSpawnPoint.InverseTransformPoint(meshVertices[1]);
            mesh.SetVertices(meshVertices);
            mesh.RecalculateBounds();
        }
    }

    /// <summary>
    /// Make sure to unsubscribe form the fired event.
    /// </summary>
    private void OnDisable()
    {
        physicalNerfgun.OnNerfgunFired -= FireGun;
    }
}
