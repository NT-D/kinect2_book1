//------------------------------------------------------------------------------
// <copyright file="MainPage.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WindowsPreview.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Diagnostics;
using System.Windows;


namespace Microsoft.Samples.Kinect.FaceBasics
{
    /// <summary>
    /// MainPage のロジックの部分です。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {

#if WIN81ORLATER
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
#else
        private ResourceLoader resourceLoader = new ResourceLoader("Resources");
#endif

        /// <summary>
        ///  Kinect センサー本体を扱うためのフィールドです。
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// アプリが適切に動作しているものかを表示するための情報を扱います。
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// 顔の回転角をとるためのフィールド値です。
        /// </summary>
        private const double FaceRotationIncrementInDegrees = 5.0;

        /// <summary>
        /// Body フレームを Kinect から取り込む際に使用するフィールドです。
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// 複数人の Body 情報を格納する配列です。
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Kinect センサーで取得可能な人数を扱うフィールドです。
        /// </summary>
        private int bodyCount;

        /// <summary>
        /// Face フレームの配列が格納されるフィールドです。
        /// </summary>
        private FaceFrameSource faceFrameSource = null;
        /// <summary>
        /// Face  フレームを Kinect から取得する際に使用するフィールドです。
        /// </summary>
        private FaceFrameReader faceFrameReader = null;

        /// <summary>
        /// Face フレームの結果を格納します。
        /// </summary>
        private FaceFrameResult faceFrameResult = null;

        /// <summary>
        /// Kinect V2 センサー の RGB カメラで取得した際のフレームの幅
        /// 今回は 1920 が入ります。
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Kinect センサー V2 の RGB カメラで取得した際のフレームの幅
        /// 今回は 1080 が入ります。
        /// </summary>
        private int displayHeight;

        public MainPage() 
        {

            //Kinect センサー V2 ののオブジェクトを取得します。
            this.kinectSensor = KinectSensor.GetDefault();

            //カラーフレームに関する情報が格納されたオブジェクトを取得します。
            FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            // カラーフレームの幅、高さが格納されます。今回は、幅が 1920、高さが 1080 が入ります。
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // Body フレームを取得するためのオブジェクトを作成します。
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // フレーム情報が Kinect で取得されたことを示すイベント "FrameArrived" が発生した際に
            // "Reader_BodyFrameArrived" の処理が実行されるようにイベントハンドラを登録します。
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            // Kinect センサーで取得できる Body の最大人数の値を格納します。
            this.bodyCount = this.kinectSensor.BodyFrameSource.BodyCount;

            // 取得した、各 Body フレームの情報を配列で保持します。
            this.bodies = new Body[this.bodyCount];

            // 必要な Face フレームの情報を特定します。
            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;

            //Face フレームのデータを取得するための設定をおこないます。
            this.faceFrameSource = new FaceFrameSource(this.kinectSensor, 0, faceFrameFeatures);
            this.faceFrameReader = this.faceFrameSource.OpenReader();

            // Kinect Sensor の処理を開始します。
            this.kinectSensor.Open();

            // アプリの起動に必要な初期化処理を実行します。
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged を用いて、プロパティの変更を画面コントロールに通知します。 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 本メソッドでは顔がどの程度回転しているかを示している四元数を、オイラー角に変換します。
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // 顔の回転状況を示した 四元数を、オイラー角に変換します。
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // 各軸に対する回転度合を求めます。
            // Pitch : X軸角、
            // Yaw   : Y軸角、
            // Roll  : Z軸角、 
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }


        /// <summary>
        /// MainPage がロードされてきた際に、実行されるメソッドです。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.faceFrameReader.FrameArrived += this.Reader_FaceFrameArrived;

            if (this.bodyFrameReader != null)
            {
                // 到着したフレームのうち、Body フレームが到着した場合に Reader_BodyFrameArrived の
                // 関数が実行されるように設定します。
                this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
            }
        }

        /// <summary>
        /// Face フレームが到着した際に実行されるハンドラーです。
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // Face フレームに適切な情報が格納されているかを検証します。
                    if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        // 適切な Face フレームの情報が格納されている場合、その情報を faceFrameResult 格納し、保持するようにします。
                        this.faceFrameResult = faceFrame.FaceFrameResult;
                    }
                    else
                    {
                        // 正しい情報が入っていない場合、Null 値を入れる。
                        this.faceFrameResult = null;
                    }
                }
            }
        }

        /// <summary>
        /// センサーから到着した、Body フレームを処理します。
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    // 現在取得している Body 配列について最新の状態に更新します。
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // Face フレームがトラックされているかを確認します。
                    if (this.faceFrameSource.IsTrackingIdValid)
                    {
                        // Face 情報が格納されているか確認します。
                        if (this.faceFrameResult != null)
                        {
                            // Face フレームの情報を、出力ウインドウに表示させるメソッドを呼び出します。
                            this.DrawFaceFrameResults(this.faceFrameResult);
                        }
                    }
                    else
                    {
                        // Face フレームと Body フレームの TrackingId を対応させます。 
                        for (int i = 0; i < this.bodyCount; i++)
                        {
                            if (this.bodies[i].IsTracked)
                            {
                                this.faceFrameSource.TrackingId = this.bodies[i].TrackingId;
                                break;
                            }
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Face フレームの結果を、出力ウインドウに表示します。
        /// </summary>
        /// <param name="faceIndex">the index of the face frame corresponding to a specific body in the FOV</param>
        /// <param name="faceResult">container of all face frame results</param>
        /// <param name="drawingContext">drawing context to render to</param>
        private void DrawFaceFrameResults(FaceFrameResult faceResult)
        {
            Debug.WriteLine("");

            // 設定した Face フレームのプロパティの情報を出力します。
            if (faceResult.FaceProperties != null)
            {
                foreach (var item in faceResult.FaceProperties)
                {
                    if (item.Value == DetectionResult.Maybe)
                    {
                        Debug.WriteLine(item.Key.ToString() + " : " + DetectionResult.No);
                    }
                    else
                    {
                        Debug.WriteLine(item.Key.ToString() + " : " + item.Value.ToString());
                    }
                }
            }

            // 顔の回転角を、オイラー角として、表示します。
            if (!faceResult.FaceRotationQuaternion.Equals(null))
            {
                int pitch, yaw, roll;
                ExtractFaceRotationInDegrees(faceResult.FaceRotationQuaternion, out pitch, out yaw, out roll);
                Debug.WriteLine("FaceYaw : " + yaw);
                Debug.WriteLine("FacePitch : " + pitch);
                Debug.WriteLine("FacenRoll : " + roll);
            }
        }

        /// <summary>
        /// RGB カメラで撮影されたフレームの中に、Face フレームの情報が存在するか検証するためのメソッドです。
        /// </summary>
        /// <param name="faceResult">the face frame result containing face box and points</param>
        /// <returns>success or failure</returns>
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (!faceBox.Equals(null) )
                {
                    // スクリーンの上で撮影された顔の情報が RGB カメラで撮影した映像の範囲内に存在するか確認します。
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= this.displayWidth &&
                                  faceBox.Bottom <= this.displayHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (var pointF in facePoints.Values)
                            {
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < this.displayWidth &&
                                                        pointF.Y < this.displayHeight;
                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return isFaceValid;
        }

        /// <summary>
        /// 現在の Kinect センサーの状態を表示するためのプロパティです。
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
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Kinect センサーの状態が変わったときに呼び出されます。
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.StatusText = this.kinectSensor.IsAvailable ? resourceLoader.GetString("RunningStatusText")
                                                            : resourceLoader.GetString("SensorNotAvailableStatusText");
        }

       
    }
}
