using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class SlicePositionData
{
    [SerializeField]
    public List<Vector2Int> Positions;

    public Color BaseColor;

    public bool CanMakeShape(List<SlicePositionData> slices, out List<SlicePositionData> requiredSlices)
    {
        // No slice that involves a piece not in this set can be used
        // We must be able to make this shape completely
        // Make a conglomerate of all remaining spaces
        List<SlicePositionData> possibleUsefulSlices = new List<SlicePositionData>(slices);

        for (int ii = possibleUsefulSlices.Count - 1; ii >= 0; ii--)
        {
            foreach (Vector2Int position in possibleUsefulSlices[ii].Positions)
            {
                if (!this.Positions.Contains(position))
                {
                    possibleUsefulSlices.RemoveAt(ii);
                    break;
                }
            }
        }

        if (possibleUsefulSlices.Count == 0)
        {
            requiredSlices = null;
            return false;
        }

        HashSet<Vector2Int> positionsNeededRemaining = new HashSet<Vector2Int>(this.Positions);
        foreach (SlicePositionData remainingUsefulSlice in possibleUsefulSlices)
        {
            foreach (Vector2Int coordinate in remainingUsefulSlice.Positions)
            {
                positionsNeededRemaining.Remove(coordinate);
            }
        }

        if (positionsNeededRemaining.Count > 0)
        {
            // If we couldn't paint all the solutions this can't be valid
            requiredSlices = null;
            return false;
        }

        // Now that we know it's possible, make the most efficient arrangement
        // ...wip
        requiredSlices = possibleUsefulSlices;
        return true;
    }
}
