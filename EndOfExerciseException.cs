using System;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class EndOfExerciseException : Exception
    {
        public EndOfExerciseException(string message) : base(message)
        {
        }
    }

}