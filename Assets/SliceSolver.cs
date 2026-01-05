using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SliceSolver : MonoBehaviour
{
    public float CoordinatePositionMultiplier = 50f;
    public SliceVisualizer SliceVisualizerPF;

    public Transform IntegratedSolutionsRoot;

    public List<Color> CandidateColors = new List<Color>();

    public void StartSolvingForSlices(List<SlicePositionData> slices)
    {
        // Given a list of all visuals to achieve, determine the least number of slices we would require to visualize everything in this system
        // We can have slices using the same coordinates; if any coordinate in a slice combination is used, it will be on.
        // There are no negatives permitted

        // First, let's establish the most fundamental building blocks
        HashSet<Vector2Int> fundamentals = new HashSet<Vector2Int>();
        foreach (SlicePositionData sliceToVisualize in slices)
        {
            foreach (Vector2Int coordinate in sliceToVisualize.Positions)
            {
                fundamentals.Add(coordinate);
            }
        }

        // TEST: Let's make all of the fundamentals. This should surely succeed
        List<SlicePositionData> allFundamentals = new List<SlicePositionData>();
        foreach (Vector2Int fundamental in fundamentals)
        {
            allFundamentals.Add(new SlicePositionData() { Positions = new List<Vector2Int>() { fundamental } });
        }

        List<SlicePositionData> reducedPairedList = ReduceToPairedPositions(allFundamentals, slices);

        if (CanMakeAllShapes(slices, reducedPairedList, out Dictionary<SlicePositionData, List<SlicePositionData>> sliceSolutions))
        {
            PresentResults(reducedPairedList, slices, sliceSolutions);
        }
    }

    public bool CanMakeAllShapes(List<SlicePositionData> slicesToMake, List<SlicePositionData> parts, out Dictionary<SlicePositionData, List<SlicePositionData>> sliceToSolutions)
    {
        sliceToSolutions = new Dictionary<SlicePositionData, List<SlicePositionData>>();

        foreach (SlicePositionData sliceToMake in slicesToMake)
        {
            if (sliceToMake.CanMakeShape(parts, out List<SlicePositionData> solutionPieces))
            {
                sliceToSolutions.Add(sliceToMake, solutionPieces);
                continue;
            }

            sliceToSolutions = null;
            return false;
        }

        return true;
    }

    Color GetCandidateColor(int index)
    {
        if (this.CandidateColors.Count == 0)
        {
            return Color.white;
        }

        return this.CandidateColors[Mathf.Max(0, index % this.CandidateColors.Count)];
    }

    public void PresentResults(List<SlicePositionData> solutionSlices, List<SlicePositionData> slicesToSolve, Dictionary<SlicePositionData, List<SlicePositionData>> sliceSolutions)
    {
        for (int ii = 0; ii < solutionSlices.Count; ii++)
        {
            SlicePositionData solutionSet = solutionSlices[ii];
            solutionSet.BaseColor = GetCandidateColor(ii);
            SliceVisualizer newVisualizer = Instantiate(this.SliceVisualizerPF, this.transform);
            newVisualizer.VisualizeList(solutionSet, this.CoordinatePositionMultiplier);
            newVisualizer.gameObject.SetActive(true);
        }

        foreach (SlicePositionData toSolve in slicesToSolve)
        {
            SliceVisualizer newVisualizer = Instantiate(this.SliceVisualizerPF, IntegratedSolutionsRoot);
            newVisualizer.VisualizeList(toSolve, this.CoordinatePositionMultiplier);
            newVisualizer.gameObject.SetActive(true);

            foreach (SlicePositionData solutionPiece in sliceSolutions[toSolve])
            {
                newVisualizer.IntegrateSolution(solutionPiece);
            }
        }
    }

    /// <summary>
    // Looking at every fundamental and every position to solve, are there any elements that are *always* tied together?
    // If we can identify any pieces that are always linked, we can reduce the amount of fundamental pieces
    // This filter is helpful for reconciling things like the "-" in the middle of an "8", which are two pieces always found together
    /// </summary>
    public List<SlicePositionData> ReduceToPairedPositions(List<SlicePositionData> allFundamentals, List<SlicePositionData> toSolve)
    {
        List<SlicePositionData> currentLinkedPieces = new List<SlicePositionData>();
        foreach (SlicePositionData fundamental in allFundamentals)
        {
            bool alwaysPresentSeeded = false;
            List<Vector2Int> alwaysPresentCoordinates = new List<Vector2Int>();

            foreach (SlicePositionData curToSolve in toSolve)
            {
                if (!curToSolve.ContainsAll(fundamental))
                {
                    continue;
                }

                if (!alwaysPresentSeeded)
                {
                    // If this is our first matching slice, add everything present
                    alwaysPresentSeeded = true;
                    alwaysPresentCoordinates = new List<Vector2Int>(curToSolve.Positions);
                }
                else
                {
                    // Otherwise, remove everything *not* present
                    for (int ii = alwaysPresentCoordinates.Count - 1; ii >= 0; ii--)
                    {
                        if (!curToSolve.Positions.Contains(alwaysPresentCoordinates[ii]))
                        {
                            alwaysPresentCoordinates.RemoveAt(ii);
                        }
                    }
                }
            }

            if (alwaysPresentCoordinates.Count == 0)
            {
                Debug.LogError($"Somehow, a fundamental isn't present in anything. That's odd!");
                continue;
            }

            // Whatever coordinates remain, these must be tied coordinates
            SlicePositionData linked = new SlicePositionData() { BaseColor = fundamental.BaseColor, Positions = alwaysPresentCoordinates };
            if (!linked.IsAlreadyInList(currentLinkedPieces))
            {
                currentLinkedPieces.Add(linked);
            }
        }

        return currentLinkedPieces;
    }
}
