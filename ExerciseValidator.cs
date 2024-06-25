using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class ExerciseValidator
    {
        private DateTime exercicseStartTimestamp;
        private int frameCouter;
        private List<string> validationResults;

        public ExerciseValidator(DateTime startTimestamp, int frameCounter, List<string> results)
        {
            this.exercicseStartTimestamp = startTimestamp;
            this.frameCouter = frameCounter;
            this.validationResults = results;
        }

        public ExerciseSummary GenerateExerciseSummary()
        {
            // Obliczanie czasu ćwiczenia
            DateTime exerciseEndTime = DateTime.UtcNow;
            double exerciseDurationMinutes = (exerciseEndTime - exercicseStartTimestamp).TotalMinutes;

            // Obliczanie liczby błędnych ramek dla każdego jointa
            Dictionary<JointType, int> jointErrorCounts = new Dictionary<JointType, int>();
            foreach (var validationResult in validationResults)
            {
                string jointTypeString = validationResult.Split(':')[1].Split(',')[0].Trim();
                JointType jointType = (JointType)Enum.Parse(typeof(JointType), jointTypeString);

                if (jointErrorCounts.ContainsKey(jointType))
                {
                    jointErrorCounts[jointType]++;
                }
                else
                {
                    jointErrorCounts[jointType] = 1;
                }
            }

            // Obliczanie oceny dla każdego jointa
            Dictionary<string, double> jointScores = jointErrorCounts.ToDictionary(
                pair => pair.Key.ToString(),
                pair => 100.0 * (frameCouter - pair.Value) / frameCouter
            );

            // Obliczanie całkowitej oceny
            double totalErrors = jointErrorCounts.Values.Sum();
            double scorePercentage = 100.0 * (frameCouter - totalErrors) / frameCouter;

            // Tworzenie obiektu podsumowania ćwiczenia
            var summary = new ExerciseSummary
            {
                Date = exerciseEndTime.ToString("o"), // Format ISO 8601
                ExerciseDurationMinutes = exerciseDurationMinutes,
                ScorePercentage = scorePercentage,
                JointScores = jointScores
            };

            return summary;
        }
    }
}
