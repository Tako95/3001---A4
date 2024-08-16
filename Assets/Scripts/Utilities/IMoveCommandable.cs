using UnityEngine;
public interface IMoveCommandable
{
    /// <summary>
    /// Pathfind to position. Optionally queue new waypoints instead of resetting them
    /// </summary>
    /// <param name="position"></param>
    /// <param name="shouldQueue"></param>
    /// <returns></returns>
    public bool MoveTo(Vector3 position, bool shouldQueue = false);

    /// <summary>
    /// Command to stop where it is
    /// </summary>
    public void Stop();
}