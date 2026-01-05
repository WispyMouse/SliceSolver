using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class SlicePositionData
{
    [SerializeField]
    public List<Vector2Int> Positions;

    public Color BaseColor;
    public bool UnbreakableFundamental { get; set; } = false;

    public SlicePositionData()
    {

    }

    public SlicePositionData(List<SlicePositionData> superSet)
    {
        this.Positions = new List<Vector2Int>();
        bool colorSet = false;
        foreach (SlicePositionData curData in superSet)
        {
            if (!colorSet)
            {
                colorSet = true;
                this.BaseColor = curData.BaseColor;
            }

            this.Positions.AddRange(curData.Positions);
        }
    }

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

        requiredSlices = possibleUsefulSlices;
        return true;
    }

    public bool CanMakeShape(SlicePositionData slice)
    {
        if (slice.Positions.Count != this.Positions.Count)
        {
            return false;
        }

        foreach (Vector2Int position in slice.Positions)
        {
            if (!this.Positions.Contains(position))
            {
                return false;
            }
        }

        return true;
    }

    public bool ContainsAll(SlicePositionData other)
    {
        if (this.Positions.Count == 0)
        {
            return false;
        }

        foreach (Vector2Int coordinate in other.Positions)
        {
            if (!this.Positions.Contains(coordinate))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsAlreadyInList(List<SlicePositionData> existing)
    {
        foreach (SlicePositionData curExisting in existing)
        {
            if (curExisting.Positions.Count != this.Positions.Count)
            {
                continue;
            }

            // If this position list has anything not in the target position list, we must not be identical
            if (this.Positions.Except(curExisting.Positions).Any())
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public override string ToString()
    {
        StringBuilder coordinateString = new StringBuilder();
        coordinateString.Append("{");

        foreach (Vector2Int coordinate in this.Positions)
        {
            coordinateString.Append($"({coordinate.x},{coordinate.y})");
        }

        coordinateString.Append("}");
        return coordinateString.ToString();
    }
}
