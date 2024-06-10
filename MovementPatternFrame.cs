using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class MovementPatternFrame
    {
        public Dictionary<JointType, CameraSpacePoint> JointPositions { get; set; }
        public Dictionary<JointType, CameraSpacePoint> BoneVectors { get; set; }
        public DateTime Timestamp { get; set; }

        public MovementPatternFrame()
        {
            JointPositions = new Dictionary<JointType, CameraSpacePoint>();
            BoneVectors = new Dictionary<JointType, CameraSpacePoint>();
        }

        public MovementPatternFrame(Dictionary<JointType, CameraSpacePoint> JointPositions, DateTime timestamp)
        {
            this.JointPositions = JointPositions;
            this.Timestamp = timestamp;
            BoneVectors = new Dictionary<JointType, CameraSpacePoint>();
            CalculateBoneVectors();
        }

        public CameraSpacePoint GetJointPosition(JointType jointType)
        {
            return JointPositions[jointType];
        }

        public void CalculateBoneVectors()
        {
            // Example for upper arm bones (you can add more bones as needed)
            AddBoneVector(JointType.ShoulderRight, JointType.ElbowRight);
            AddBoneVector(JointType.ElbowRight, JointType.WristRight);
            AddBoneVector(JointType.WristRight, JointType.HandRight);
            // Add more bones as needed
        }

        private void AddBoneVector(JointType joint1, JointType joint2)
        {
            if (JointPositions.ContainsKey(joint1) && JointPositions.ContainsKey(joint2))
            {
                CameraSpacePoint point1 = JointPositions[joint1];
                CameraSpacePoint point2 = JointPositions[joint2];

                BoneVectors[joint1] = new CameraSpacePoint
                {
                    X = point2.X - point1.X,
                    Y = point2.Y - point1.Y,
                    Z = point2.Z - point1.Z
                };
            }
        }

        public void ScaleBoneVectors(float scale)
        {
            var keys = new List<JointType>(BoneVectors.Keys); // Utwórz kopię kluczy

            foreach (var key in keys)
            {
                BoneVectors[key] = new CameraSpacePoint
                {
                    X = BoneVectors[key].X * scale,
                    Y = BoneVectors[key].Y * scale,
                    Z = BoneVectors[key].Z * scale
                };
            }
        }

    }
}