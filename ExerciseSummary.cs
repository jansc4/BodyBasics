using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class ExerciseSummary
    {
        public string Date { get; set; }
        public double ExerciseDurationMinutes { get; set; }
        public double ScorePercentage { get; set; }
        public Dictionary<string, double> JointScores { get; set; }
    }
}