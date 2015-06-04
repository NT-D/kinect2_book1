using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using System.Diagnostics;//[出力]ウィンドウを使用して情報を出力するために追加

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace KinectBody
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Kinect センサー用の変数
        /// </summary>
        private KinectSensor kinect = null;

        /// <summary>
        /// bodyフレームを取得する Reader のための変数
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Body のデーターを格納する配列
        /// </summary>
        private Body[] bodies;

        public MainPage()
        {
            //Kinect センサー オブジェクトを取得します。
            this.kinect = KinectSensor.GetDefault();

            //センサーが認識する体の総数分の配列を作成します。
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];

            //body frame 用の Reader を取得します。
            this.bodyFrameReader = this.kinect.BodyFrameSource.OpenReader();

            //body frame 到着時のイベントハンドラを登録
            this.bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            //Kinect センサーを起動します。
            this.kinect.Open();

            //画面上のコンポーネントを初期化します。
            this.InitializeComponent();
        }

        //bodyframe が到着した際のイベントハンドラ
        void bodyFrameReader_FrameArrived(BodyFrameReader sender, BodyFrameArrivedEventArgs args)
        {
            //bodyframe への参照から実際のデータを取得します。
            using (BodyFrame bodyframe = args.FrameReference.AcquireFrame())
            {
                if (bodyframe != null)
                {
                    //保持するデータを最新のものに更新する
                    bodyframe.GetAndRefreshBodyData(this.bodies);

                    //body情報をコンソールに出力するメソッドを呼び出します。
                    this.writeJointsData();
                }
            }
        }

        //body情報をコンソールに出力するメソッドです。
        private void writeJointsData()
        {
            int bodycount = 1;
            //複数の体をトラックしている際に、一つ一つの体の情報をトラックします。
            foreach (Body body in bodies)
            {
                //体が正常にトラックされている場合には次の処理に進みます。
                if (body.IsTracked)
                {
                    //複数の体がトラックされている際に、何番目の体かを表示します。
                    //同じ体を追跡したい場合には、bodies[<追跡したい番号>]のようにインデックスを使ってアクセスする必要があります。
                    Debug.WriteLine(bodycount.ToString() + "番目の体");
                    bodycount++;

                    //体のjointsをすべて処理していきます。
                    foreach (Joint joint in body.Joints.Values)
                    {
                        //トラックされている状態に基づいて表示メッセージを切り替えます。
                        //トラック状態に関する詳細は下記ページを参照してください。
                        //https://msdn.microsoft.com/ja-jp/library/windowspreview.kinect.trackingstate.aspx
                        switch (joint.TrackingState)
                        {
                            case TrackingState.Tracked:
                                Debug.WriteLine(joint.JointType + "は正しく計測されており、" + "X:" + joint.Position.X + ", Y:" + joint.Position.Y + ", Z:" + joint.Position.Z);
                                break;
                            case TrackingState.Inferred:
                                Debug.WriteLine(joint.JointType + "は位置を推測しており、その値は" + "X:" + joint.Position.X + ", Y:" + joint.Position.Y + ", Z:" + joint.Position.Z);
                                break;
                            case TrackingState.NotTracked:
                                Debug.WriteLine(joint.JointType + "は位置を測定できませんでした。");
                                break;
                        }
                    }
                }
            }
        }
    }
}
