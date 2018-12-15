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
using System.Globalization;
using System.Xml;

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
        //Marca la sala actual.
        string salaActual = "1.0";
        int paginaActualSala = 0;
        int[] maxPaginas = { 2 , 1 , 0};

        public MainWindow()
        {
            InitializeComponent();
            //añadimos todas los botones con imagenes
            buttons = new List<KinectTileButton> { imageButton1,imageButton2,imageButton3, imageButton4,
                imageButton5,imageButton6,imageButton7,imageButton8,imageButton9,imageButton10,
                imageButton11,imageButton12,imageButton13,imageButton14,imageButton15,imageButton16,
                imageButton17,imageButton18,imageButton19,imageButton20,imageButton21,
                imageButton22,imageButton23,imageButton24,imageButton25,imageButton26,
                imageButton27,imageButton28,imageButton29,imageButton30,imageButton31,
                imageButton32,imageButton33,imageButton34,imageButton35,imageButton36,
                imageButton37
            };

            Loaded += OnLoaded;
            // initialize control modes
            controlManager.addControlMode(new OneHandMode(controlManager)); // id 0
            controlManager.addControlMode(new TwoHandMode(controlManager)); // id 1



        }

        private void InformacionObra(String idObra)
        {
            XmlDocument xDoc = new XmlDocument();

            //La ruta del documento XML permite rutas relativas 
            //respecto del ejecutable!

            xDoc.Load("../../obras.xml");

            XmlNodeList obras = xDoc.GetElementsByTagName("obras");

            XmlNodeList lista = ((XmlElement)obras[0]).GetElementsByTagName("obra");

            foreach (XmlElement nodo in lista)

            {

                if(nodo.Attributes["id"].Value == idObra)
                {
           
                    autor.Content = nodo.GetElementsByTagName("autor")[0].InnerText;
                    titulo.Content = nodo.GetElementsByTagName("titulo")[0].InnerText;
                    
                    break;
                }
               

                


            }

        }
    
        private void CambiarSala(string sala, int pagina)
        {
            String salaBuscada = sala;
            salaBuscada += ".";
            salaBuscada += pagina.ToString();
            if(salaBuscada != salaActual)
            {
                foreach (KinectTileButton target in buttons)
                {
                    if(target.Uid == salaBuscada)
                    {
                        target.Visibility = Visibility.Visible;
                    }
                    else if(target.Uid != "elegido")
                    {
                        target.Visibility = Visibility.Hidden;
                    }
                }
                salaActual = salaBuscada;
                paginaActualSala = pagina;
            }
            
        }

        //Se le pasa -1 para disminuir una pagina y +1 para aumentar una pagina
        private void CambiarPagina(int numPaginasPasadas)
        {
            int siguientePagina = paginaActualSala + numPaginasPasadas;
            if(siguientePagina >= 0)
            {
                string sala = StringInfo.GetNextTextElement(salaActual, 0);
                if (sala == "1" && siguientePagina <= maxPaginas[0] )
                {
                    CambiarSala("1",siguientePagina);
                }
                if (sala == "2" && siguientePagina <= maxPaginas[1])
                {
                    CambiarSala("2", siguientePagina);
                }
                if (sala == "3" && siguientePagina <= maxPaginas[2])
                {
                    CambiarSala("3", siguientePagina);
                }
            }
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
        double primeraProfundidad = 0;
        double profundidad = 0;
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

                if(stopDraw)
                    if (isHandOver(oldPoint, buttons))
                    {
                        stopDraw = false;
                        primeraProfundidad = getprofundidad();
                        etiqueta.Content = primeraProfundidad.ToString();

                        if(selected.Uid != "elegido")
                        {
                            stopDraw = true;

                            var nuevo = ClonarBoton(selected);
                            Canvas.SetLeft(nuevo, newPoint.X - selected.Width / 2);
                            Canvas.SetTop(nuevo, newPoint.Y - selected.Height / 2);

                            this.canvasKinect.Children.Add(nuevo);
                            selected = nuevo;
                            buttons.Insert(0, nuevo);

                        }

                    }
       


                if (!stopDraw && selected.Uid == "elegido")
                {
                    InformacionObra(selected.Name);
                    Canvas.SetLeft(selected, newPoint.X-selected.Width/2);
                    Canvas.SetTop(selected, newPoint.Y-selected.Height/2);
                    profundidad = getprofundidad();
                    etiqueta2.Content = profundidad.ToString();
                    double widthNuevo = selected.Width - (primeraProfundidad - profundidad) * 1.5;
                    double heightNuevo = selected.Height - (primeraProfundidad - profundidad) * 1.5;

                    if (widthNuevo > 0 && heightNuevo > 0)
                    {
                        selected.Width = widthNuevo;
                        selected.Height = heightNuevo;
                    }

                }
            }
            else
            {
                kinectRegion.Tag = "";
                if(selected != null)
                {
                    double margenIzquierdo = Canvas.GetLeft(selected);
                    double margenSuperior = Canvas.GetTop(selected);

                    if (margenSuperior < 0)
                    {
                        Canvas.SetTop(selected, 0);
                    }
                    if (margenSuperior > 800 - selected.Height)
                    {
                        Canvas.SetTop(selected, 800-selected.Height);
                    }
                    if (margenIzquierdo < 198)
                    {
                        Canvas.SetLeft(selected, 198);
                    }
                    if (margenIzquierdo > 1190-selected.Width)
                    {
                        Canvas.SetLeft(selected, 1190-selected.Width);
                    }
                }
                selected = null;
                stopDraw = true;
                primeraProfundidad = 0;
                profundidad = 0;
            }
        }

         public  double getprofundidad()
        {
            double z = 0;
            foreach(var user in _userInfos)
            {
                int userID = user.SkeletonTrackingId;
                if (userID == 0)
                {
                    continue;
                }
                foreach (var hand in user.HandPointers)
                {
                    // && hand.HandEventType == InteractionHandEventType.Grip
                    if (!hand.IsPrimaryForUser && hand.RawY < 800 && hand.RawX < 1520)
                    {
                        z = hand.RawZ;
                    }
                   

                }
            }
            
            return z;
        }

        private KinectTileButton ClonarBoton(KinectTileButton original)
        {
            KinectTileButton nuevo = new KinectTileButton();
            Brush imagen = original.Background;
            nuevo.Background = imagen;
            nuevo.Width = selected.Width;
            nuevo.Height = selected.Height;
            nuevo.Name = selected.Name;
            nuevo.Uid = "elegido";
            return nuevo;

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
                System.Windows.Point targetTopLeft = new System.Windows.Point(Canvas.GetLeft(target), Canvas.GetTop(target));
            
                if (handX > targetTopLeft.X &&
                    handX < targetTopLeft.X + target.Width &&
                    handY > targetTopLeft.Y &&
                    handY < targetTopLeft.Y + target.Height && target.IsVisible)
                {
                    selected = target;
                    return true;
                }
            }
            return false;
        }

        private void UiButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void Sala1Click(object sender, RoutedEventArgs e)
        {
            CambiarSala("1",0);
        }

        private void Sala2Click(object sender, RoutedEventArgs e)
        {
            CambiarSala("2",0);
        }
        private void Sala3Click(object sender, RoutedEventArgs e)
        {
            CambiarSala("3",0);
        }

        private void ImageButton50_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ReordenarCanvas(object sender, RoutedEventArgs e)
        {

        }

        private void PaginaAnterior(object sender, RoutedEventArgs e)
        {
            CambiarPagina(-1);
        }

        private void PaginaSiguiente(object sender, RoutedEventArgs e)
        {
            CambiarPagina(1);
        }

        private void LimpiarCanvas(object sender, RoutedEventArgs e)
        {
            List<KinectTileButton> borrados = new List<KinectTileButton>();
            foreach (KinectTileButton target in buttons)
            {
             

                if (target.Uid == "elegido")
                {
                    borrados.Add(target);
                    this.canvasKinect.Children.Remove(target);



                }
            }

            foreach (KinectTileButton target in borrados)
            {
                buttons.Remove(target);
            }
        }
    }

}
