using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System.Collections.Generic;
    public class MovementPatternFrame
    {
        public Dictionary<JointType, CameraSpacePoint> JointPositions { get; set; }

        public MovementPatternFrame()
        {
            JointPositions = new Dictionary<JointType, CameraSpacePoint>();
        }

        public MovementPatternFrame(Dictionary<JointType, CameraSpacePoint> JointPositions)
        {
            this.JointPositions = JointPositions;
        }
    }
}