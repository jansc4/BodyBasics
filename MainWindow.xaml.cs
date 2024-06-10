//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;
        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodiesCopy = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        
        
        /// <summary>
        /// Recording on/off
        /// </summary>
        private bool isRecording = false;
        
        /// <summary>
        /// Exercise on/off
        /// </summary>
        private bool isExercise = false;
        
        /// <summary>
        /// List of Body Frames from recording
        /// </summary>
        
        private List<MovementPatternFrame> recordedFrames = new List<MovementPatternFrame>();
        
       /// <summary>
       /// Joints recorded info file
       /// </summary>
        
        private string outputFilePath = "recordedJoints.csv";

        private DateTime exercicseStartTimestamp;
        
        private MovementPatternFrame closestPatternFrame;
        
        private List<string> validationResults = new List<string>();
        
        private int currentFrameIndex = 0;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
        /// <summary>
        /// Start recording exercise movment pattern
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 5; i > 0; i--)
            {
                StatusText = $"Recording will start in {i} seconds";
                await Task.Delay(1000);
            }
    
            StatusText = "Recording started!";
            isRecording = true;
    
            // Zakładamy, że nagrywanie będzie trwało przez określony czas
            await Task.Delay(15000);  // Czas nagrywania w milisekundach (5 sekund)
    
            isRecording = false;
            StatusText = "Recording stopped!";
        }


        /// <summary>
        /// Stop recording exercise movment pattern
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            isRecording = false;
            StatusText = "Recording stopped!";

            // Dodaj kod do zapisu nagranych klatek lub ich przetworzenia
            // if (recordedFrames.Count > 0)
            // {
            //     // Przykładowe zachowanie: zapisanie liczby nagranych klatek
            //     MessageBox.Show($"Recorded frames: {recordedFrames.Count}");
            // }
            // else
            // {
            //     MessageBox.Show("No frames recorded.");
            // }
        }
        /// <summary>
        /// Start exercise
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StartExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 5; i > 0; i--)
            {
                StatusText = $"Exercise will start in {i} seconds";
                await Task.Delay(1000);
            }
            isExercise = true;
            StatusText = "Exercise started";
            exercicseStartTimestamp = DateTime.Now;
        }
        /// <summary>
        /// Stop exercise
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            isExercise = false;
            StatusText = "Exercise ended";
            SaveValidationResults();
        }

        /// <summary>
        /// Calculate ratio between user's arm length and saved pattern
        /// </summary>
        /// <param name="patternFrames"></param>
        /// <param name="userFrames"></param>
        /// <returns>
        /// float
        /// </returns>
        public float PatternScale(List<MovementPatternFrame> patternFrames, List<Body> userFrames)
        {
            // Skala domyślna
            float scale = 1;
            float averagePatternLength = 0;
            List<float> patternLengths = new List<float>();

            // Pomiar długości średniej pomiędzy wybranymi jointami dla wzorca
            foreach (var frame in patternFrames)
            {
                CameraSpacePoint position1 = frame.GetJointPosition(JointType.ShoulderRight);
                CameraSpacePoint position2 = frame.GetJointPosition(JointType.ElbowRight);

                float length = (float)Math.Sqrt(
                    Math.Pow(position2.X - position1.X, 2) +
                    Math.Pow(position2.Y - position1.Y, 2) +
                    Math.Pow(position2.Z - position1.Z, 2)
                );

                patternLengths.Add(length);
            }

            // Obliczanie średniej długości dla wzorca
            if (patternLengths.Count > 0)
            {
                averagePatternLength = 0;
                foreach (float length in patternLengths)
                {
                    averagePatternLength += length;
                }
                averagePatternLength /= patternLengths.Count;
            }

            // Pomiar długości średniej pomiędzy wybranymi jointami dla użytkownika
            float averageUserLength = 0;
            List<float> userLengths = new List<float>();

            foreach (var body in userFrames)
            {
                if (body.Joints.ContainsKey(JointType.ShoulderRight) && body.Joints.ContainsKey(JointType.ElbowRight))
                {
                    CameraSpacePoint position1 = body.Joints[JointType.ShoulderRight].Position;
                    CameraSpacePoint position2 = body.Joints[JointType.ElbowRight].Position;

                    float length = (float)Math.Sqrt(
                        Math.Pow(position2.X - position1.X, 2) +
                        Math.Pow(position2.Y - position1.Y, 2) +
                        Math.Pow(position2.Z - position1.Z, 2)
                    );

                    userLengths.Add(length);
                }
            }

            // Obliczanie średniej długości dla użytkownika
            if (userLengths.Count > 0)
            {
                averageUserLength = 0;
                foreach (float length in userLengths)
                {
                    averageUserLength += length;
                }
                averageUserLength /= userLengths.Count;
            }

            // Obliczanie skali jako stosunek średniej długości użytkownika do średniej długości wzorca
            if (averagePatternLength != 0)
            {
                scale = averageUserLength / averagePatternLength;
            }

            return scale;
        }
        /// <summary>
        /// Scale loaded movment pattern
        /// </summary>
        /// <param name="scale"></param>
        public void ScaleMovmentPattern(float scale)
        {
            List<MovementPatternFrame> scaledRecordedPattern = new List<MovementPatternFrame>();
            foreach (var frame in recordedFrames)
            {
                
            }
        }
        
        /// <summary>
        /// Load saves pattern from file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>
        /// List<MovementPatternFrame>
        /// </returns>
        public List<MovementPatternFrame> GetRecordedFrames(string filePath)
        {
            List<MovementPatternFrame> patternFrames = new List<MovementPatternFrame>();

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                Dictionary<JointType, CameraSpacePoint> currentFrameJoints = new Dictionary<JointType, CameraSpacePoint>();
                DateTime currentTimestamp = DateTime.MinValue;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "#")
                    {
                        if (currentFrameJoints.Count > 0)
                        {
                            patternFrames.Add(new MovementPatternFrame(currentFrameJoints, currentTimestamp));
                            currentFrameJoints = new Dictionary<JointType, CameraSpacePoint>();
                        }
                    }
                    else
                    {
                        string[] jointData = line.Split(';');
                        JointType jointType = (JointType)Enum.Parse(typeof(JointType), jointData[0]);
                        //string[] positionData = jointData[1].Split(',');
                        CameraSpacePoint position = new CameraSpacePoint
                        {
                            X = float.Parse(jointData[1]),
                            Y = float.Parse(jointData[2]),
                            Z = float.Parse(jointData[3])
                        };
                        currentTimestamp = DateTime.Parse(jointData[4]);

                        currentFrameJoints[jointType] = position;
                    }
                }

                if (currentFrameJoints.Count > 0)
                {
                    patternFrames.Add(new MovementPatternFrame(currentFrameJoints, currentTimestamp));
                }
            }

            return patternFrames;
        }





        /// <summary>
        /// Save joints information to csv file
        /// </summary>
        /// <param name="body"></param>
        private void SaveJointDataToFile(Body body)
        {
            using (StreamWriter sw = new StreamWriter(outputFilePath, true))
            {
                DateTime timestamp = DateTime.Now;

                foreach (var joint in body.Joints)
                {
                    JointType jointType = joint.Key;
                    CameraSpacePoint position = joint.Value.Position;
                    sw.WriteLine($"{jointType};{position.X};{position.Y};{position.Z};{timestamp}");
                }
                sw.WriteLine("#");
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;

                    if (isRecording)
                    {
                        foreach (Body body in this.bodies)
                        {
                            if (body != null && body.IsTracked)
                            {
                                SaveJointDataToFile(body);
                            }
                        }
                    }
                }
            }

            if (dataReceived)
            {
                if (isExercise)
                {
                    if (recordedFrames == null || recordedFrames.Count == 0)
                    {
                        recordedFrames = GetRecordedFrames(outputFilePath);
                        
                    }
                    // Oblicz skalę na podstawie wzorców ruchu i ramek użytkownika
                    float scale = PatternScale(recordedFrames, this.bodies.ToList());
                    // Get the current timestamp
                    DateTime currentTimestamp = DateTime.Now;

                    // Find the closest recorded frame to the current time
                    closestPatternFrame = FindClosestFrame(currentTimestamp, recordedFrames);

                    if (closestPatternFrame != null)
                    {
                        // Scale the recorded frame
                        //closestPatternFrame.ScaleBoneVectors(scale);

                        
                    }
                }

                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 2;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                
                                
                                
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);
                            //this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            //this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                            if (isExercise)
                            {
                                ValidateMovement(body, closestPatternFrame, dc);
                                
                            }
                            
                        
                        }
                    }

                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private MovementPatternFrame FindClosestFrame(DateTime currentTimestamp, List<MovementPatternFrame> patternFrames)
        {
            /*
            MovementPatternFrame closestFrame = null;
            double minDifference = double.MaxValue;
            double timeDiff = (currentTimestamp - exercicseStartTimestamp).TotalMilliseconds;
            DateTime patternStartTime = patternFrames.Min(frame => frame.Timestamp);

            foreach (var frame in patternFrames)
            {
                double difference = Math.Abs((frame.Timestamp - patternStartTime).TotalMilliseconds - timeDiff);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    closestFrame = frame;
                }
            }
            */
            // Sprawdzanie, czy lista nie jest pusta
            if (patternFrames == null || patternFrames.Count == 0)
            {
                return null;
            }

            // Pobranie ramki o aktualnym indeksie
            MovementPatternFrame nextFrame = patternFrames[currentFrameIndex];

            // Zwiększenie indeksu do następnej ramki
            currentFrameIndex++;

            // Resetowanie indeksu, jeśli osiągnie koniec listy
            if (currentFrameIndex >= patternFrames.Count)
            {
                currentFrameIndex = 0;
            }

            return nextFrame;
        }




        private void ValidateMovement(Body userBody, MovementPatternFrame patternFrame, DrawingContext drawingContext)
        {

            foreach (var jointType in patternFrame.BoneVectors.Keys)
            {
                JointType correspondingJoint = GetCorrespondingJoint(jointType);
                if (userBody.Joints.ContainsKey(jointType) && userBody.Joints.ContainsKey(correspondingJoint))
                {
                    CameraSpacePoint userStartPosition = userBody.Joints[jointType].Position;
                    CameraSpacePoint userEndPosition = userBody.Joints[correspondingJoint].Position;

                    CameraSpacePoint userVector = new CameraSpacePoint
                    {
                        X = userEndPosition.X - userStartPosition.X,
                        Y = userEndPosition.Y - userStartPosition.Y,
                        Z = userEndPosition.Z - userStartPosition.Z
                    };

                    CameraSpacePoint patternVector = patternFrame.BoneVectors[jointType];

                    float tolerance = 0.05f; //5cm
                    // Rysowanie wektora użytkownika
                    /*DepthSpacePoint userStartDepthPoint = this.coordinateMapper.MapCameraPointToDepthSpace(userStartPosition);
                    DepthSpacePoint userEndDepthPoint = this.coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint
                    {
                        X = userStartPosition.X + userVector.X,
                        Y = userStartPosition.Y + userVector.Y,
                        Z = userStartPosition.Z + userVector.Z
                    });

                    Pen userPen = new Pen(Brushes.Red, 3);
                    drawingContext.DrawLine(userPen, new Point(userStartDepthPoint.X, userStartDepthPoint.Y), new Point(userEndDepthPoint.X, userEndDepthPoint.Y));

                    // Rysowanie wektora wzorcowego
                    DepthSpacePoint patternEndDepthPoint = this.coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint
                    {
                        X = userStartPosition.X + patternVector.X,
                        Y = userStartPosition.Y + patternVector.Y,
                        Z = userStartPosition.Z + patternVector.Z
                    });

                    Pen patternPen = new Pen(Brushes.Green, 3);
                    drawingContext.DrawLine(patternPen, new Point(userStartDepthPoint.X, userStartDepthPoint.Y), new Point(patternEndDepthPoint.X, patternEndDepthPoint.Y));
                    */

                    // Rysowanie strzałki
                    DrawArrow(userEndPosition, userVector, patternVector, drawingContext);
                    if (Math.Abs(userVector.X - patternVector.X) > tolerance || Math.Abs(userVector.Y - patternVector.Y) > tolerance || Math.Abs(userVector.Z - patternVector.Z) > tolerance)
                    {
                        // Dodanie wyniku walidacji do listy
                        validationResults.Add($"JointType: {jointType}, UserVector: ({userVector.X}, {userVector.Y}, {userVector.Z}), PatternVector: ({patternVector.X}, {patternVector.Y}, {patternVector.Z})");

                        
                    }
                }
            }

            
            
        }

        // Metoda do zapisu wyników walidacji do pliku
        private void SaveValidationResults()
        {
            string filePath = "ValidationResults.txt";

            try
            {
                // Zapisz wyniki do pliku
                File.WriteAllLines(filePath, validationResults);
                Console.WriteLine($"Validation results saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving validation results: {ex.Message}");
            }
        }

        private void DrawArrow(CameraSpacePoint startPosition, CameraSpacePoint userVector, CameraSpacePoint patternVector, DrawingContext drawingContext)
        {
            // Rysowanie strzałki, która pokazuje kierunek, w którym użytkownik powinien się przesunąć
            // Możemy użyć różnicy między userVector a patternVector do wyznaczenia kierunku

            // Przykład rysowania strzałki na podstawie różnicy wektorów
            CameraSpacePoint correctionVector = new CameraSpacePoint
            {
                X = patternVector.X - userVector.X,
                Y = patternVector.Y - userVector.Y,
                Z = patternVector.Z - userVector.Z
            };

            // Mapowanie pozycji do przestrzeni głębokości
            DepthSpacePoint startDepthPoint = this.coordinateMapper.MapCameraPointToDepthSpace(startPosition);
            DepthSpacePoint endDepthPoint = this.coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint
            {
                X = startPosition.X + correctionVector.X,
                Y = startPosition.Y + correctionVector.Y,
                Z = startPosition.Z + correctionVector.Z
            });

            // Utwórz pióro do rysowania linii
            Pen drawPen = new Pen(Brushes.Red, 3);

            // Rysowanie strzałki na ekranie
            drawingContext.DrawLine(drawPen, new Point(startDepthPoint.X, startDepthPoint.Y), new Point(endDepthPoint.X, endDepthPoint.Y));

            // Rysowanie końcówki strzałki
            double arrowSize = 5; // Rozmiar końcówki strzałki

            Point arrowTip = new Point(endDepthPoint.X, endDepthPoint.Y);
            Point arrowBase1 = new Point(endDepthPoint.X - arrowSize, endDepthPoint.Y - arrowSize);
            Point arrowBase2 = new Point(endDepthPoint.X + arrowSize, endDepthPoint.Y - arrowSize);

            drawingContext.DrawLine(drawPen, arrowTip, arrowBase1);
            drawingContext.DrawLine(drawPen, arrowTip, arrowBase2);
        }


        private JointType GetCorrespondingJoint(JointType jointType)
        {
            switch (jointType)
            {
                case JointType.ShoulderRight:
                    return JointType.ElbowRight;
                case JointType.ElbowRight:
                    return JointType.WristRight;
                case JointType.WristRight:
                    return JointType.HandRight;
                // Add more joints as needed
                default:
                    return jointType;
            }
        }

       



        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
