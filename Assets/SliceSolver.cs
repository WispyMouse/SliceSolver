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

        for (int ii = 0; ii < allFundamentals.Count; ii++)
        {
            SlicePositionData preprogrammedSet = allFundamentals[ii];
            preprogrammedSet.BaseColor = GetCandidateColor(ii);
            SliceVisualizer newVisualizer = Instantiate(this.SliceVisualizerPF, this.transform);
            newVisualizer.VisualizeList(preprogrammedSet, this.CoordinatePositionMultiplier);
            newVisualizer.gameObject.SetActive(true);
        }

        if (CanMakeAllShapes(slices, allFundamentals, out Dictionary<SlicePositionData, List<SlicePositionData>> sliceSolutions))
        {
            foreach (SlicePositionData toSolve in slices)
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
}
