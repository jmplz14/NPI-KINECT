using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Interaction;
using Microsoft.Kinect.Toolkit.Controls;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using System.Collections.ObjectModel;

namespace KinectAntonio
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensorChooser sensorChooser;
        private InteractionStream _interactionStream;
        private Skeleton[] _skeletons; //the skeletons
        private UserInfo[] _userInfos; //the information about the interactive users
        private KinectSensor _sensor;
        private ControlManager controlManager = new ControlManager();
        List<KinectTileButton> buttons;
        KinectTileButton selected;

        public MainWindow()
        {
            InitializeComponent();
            //añadimos todas los botones con imagenes
            buttons = new List<KinectTileButton> { imageButton1,imageButton2 };

            Loaded += OnLoaded;
            // initialize control modes
            controlManager.addControlMode(new OneHandMode(controlManager)); // id 0
            controlManager.addControlMode(new TwoHandMode(controlManager)); // id 1
        }


        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Left;
            this.Top = desktopWorkingArea.Top;
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

        }


        // Event handler Kinect controller change. Handles Sensor configs on start and stop.
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            bool error = false;
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    error = true;
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    var parameters = new TransformSmoothParameters
                    {
                        Smoothing = 0.7f,
                        Correction = 0.3f,
                        Prediction = 1.0f,
                        JitterRadius = 1.0f,
                        MaxDeviationRadius = 1.0f,
                    };

                    _sensor = args.NewSensor;
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    _sensor.SkeletonStream.Enable(parameters);
                    _skeletons = new Skeleton[_sensor.SkeletonStream.FrameSkeletonArrayLength];
                    _userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];

                    try
                    {
                        _sensor.DepthStream.Range = DepthRange.Default;
                        _sensor.SkeletonStream.EnableTrackingInNearRange = false;
                        _sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                    }
                    catch (InvalidOperationException)
                    {
                        _sensor.DepthStream.Range = DepthRange.Default;
                        _sensor.SkeletonStream.EnableTrackingInNearRange = false;
                        error = true;
                    }

                    _interactionStream = new InteractionStream(_sensor, new DummyInteractionClient());
                    _interactionStream.InteractionFrameReady += InteractionStreamOnInteractionFrameReady;

                    _sensor.DepthFrameReady += SensorOnDepthFrameReady;
                    _sensor.SkeletonFrameReady += SensorOnSkeletonFrameReady;
                }
                catch (InvalidOperationException)
                {
                    error = true;
                }
            }

            if (!error)
            {
                kinectRegion.KinectSensor = _sensor;
            }
        }


        private void SensorOnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs skeletonFrameReadyEventArgs)
        {
            using (SkeletonFrame skeletonFrame = skeletonFrameReadyEventArgs.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }

                try
                {
                    skeletonFrame.CopySkeletonDataTo(_skeletons);
                    var accelerometerReading = _sensor.AccelerometerGetCurrentReading();
                    _interactionStream.ProcessSkeleton(_skeletons, accelerometerReading, skeletonFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // If exception skip frame
                }
            }
        }

        private void SensorOnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            using (DepthImageFrame depthFrame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                try
                {
                    _interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // If exception skip frame
                }
            }
        }

        public Point oldPoint;
        public Point newPoint;
        public Point3D oldDepth;
        public Point3D newDepth;
        bool stopDraw;
        double primeraDistancia = 0;
        double distancia = 0;
        private void InteractionStreamOnInteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            TrackClosestSkeleton();

            using (var iaf = e.OpenInteractionFrame())
            {
                if (iaf == null)
                    return;

                iaf.CopyInteractionDataTo(_userInfos);
            }

            // for activating and disabling draw
            //pinta o no pinta
            //es decir cuando esta abierta la mana (!isDrawActive)
            if (controlManager.isDrawActive(_userInfos, _skeletons))
            {
                newPoint = controlManager.getCursorLocation(kinectRegion);
                
                if (oldDepth == null || stopDraw == true)
                {
                    oldDepth = newDepth;

                }
                if (oldPoint == null || stopDraw == true)
                {
                    oldPoint = newPoint;
                }
                
                newDepth = controlManager.getHandLocation();
                
                if (isHandOver(oldPoint, buttons))
                {
                    stopDraw = false;
                    primeraDistancia = getDistancia();
                }

                if (!stopDraw)
                {
                    Thickness thickness = new Thickness();
                    thickness.Top = newPoint.Y-20;
                    thickness.Left = newPoint.X-20;
                    selected.Margin = thickness;
                    distancia = getDistancia();
                    selected.Width += (primeraDistancia - distancia)*1.5;
                    selected.Height += (primeraDistancia - distancia)*1.5;
                }
                /* Como dibuja
                DrawCanvas.Paint(oldPoint, newPoint, inkCanvas, color, thickness, tool, oldDepth, newDepth);
                oldPoint = newPoint;
                oldDepth = newDepth;
                */


                kinectRegion.Tag = "draw";
            }
            else
            {
                kinectRegion.Tag = "";
                stopDraw = true;
                primeraDistancia = 0;
                distancia = 0;
            }
        }

         public  double getDistancia()
        {
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            foreach(var user in _userInfos)
            {
                int userID = user.SkeletonTrackingId;
                if (userID == 0)
                {
                    continue;
                }
                foreach (var hand in user.HandPointers)
                {
                    if (hand.IsPrimaryForUser && hand.HandEventType == InteractionHandEventType.Grip)
                    {
                        x1 = hand.RawX;
                        y1 = hand.RawY;
                    }
                    else { 
                        x2 = hand.RawX;
                        y2 = hand.RawY;
                    }

                }
            }
            
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        private void TrackClosestSkeleton()
        {
            if (this._sensor != null && this._sensor.SkeletonStream != null)
            {
                if (!this._sensor.SkeletonStream.AppChoosesSkeletons)
                {
                    this._sensor.SkeletonStream.AppChoosesSkeletons = true; // Ensure AppChoosesSkeletons is set
                }

                float closestDistance = 10000f; // Start with a far enough distance
                int closestID = 0;

                foreach (Skeleton skeleton in this._skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked))
                {
                    if (skeleton.Position.Z < closestDistance)
                    {
                        closestID = skeleton.TrackingId;
                        closestDistance = skeleton.Position.Z;
                    }
                }

                if (closestID > 0)
                {
                    this._sensor.SkeletonStream.ChooseSkeletons(closestID); // Track this skeleton
                }
            }
        }

        //detect if hand is overlapping over any button
        private bool isHandOver(Point punto, List<KinectTileButton> buttonslist)
        {
            var handX = punto.X;
            var handY = punto.Y;



            foreach (KinectTileButton target in buttonslist)
            {
                if (handX > target.Margin.Left &&
                    handX < target.Margin.Left + target.Width &&
                    handY > target.Margin.Top &&
                    handY < target.Margin.Top + target.Height)
                {
                    selected = target;
                    return true;
                }
            }
            return false;
        }
    }

}
