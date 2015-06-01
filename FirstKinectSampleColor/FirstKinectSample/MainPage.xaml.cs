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
//Kinect 開発用の namespace を追加
using WindowsPreview.Kinect;
//ビットマップデータの格納用オブジェクトを利用しやすくするためにnamespaceを追加
using Windows.UI.Xaml.Media.Imaging;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FirstKinectSample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Kinect センサー用の変数を追加
        private KinectSensor kinect;

        //RGBカメラを利用するためのColorFrameReader用の変数を追加
        private ColorFrameReader colorReader = null;

        //RGBカメラの情報(横幅や、ピクセルごとのビット数など)を格納するためのオブジェクトを格納する変数を追加
        private FrameDescription colorFrameDescription = null;

        //画像データの格納用の変数を追加
        //WriteableBitmap:書き込みおよび更新が可能なビットマップデータの格納用オブジェクト
        private WriteableBitmap bitmap = null;


        public MainPage()
        {
            //Kinect センサー オブジェクトを取得します。
            this.kinect = KinectSensor.GetDefault();

            //ColorFrameReaderオブジェクトをcolorReader内に格納します。
            this.colorReader = this.kinect.ColorFrameSource.OpenReader();

            //フレーム到着時のイベントハンドラを登録します。フレームは30(回/秒)到着するので、ここで登録したイベントハンドラ内の処理が30(回/秒)行われます。
            this.colorReader.FrameArrived += colorReader_FrameArrived;

            //取得する画像の形式をBGRA(RGBと透明度を表すAlphaの情報を含む形式)フォーマットとして定義します
            colorFrameDescription = this.kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            //表示領域へ表示させるbitmapデータの形式(幅・高さ)を指定します。
            this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);


            //Kinect センサーを起動します。
            this.kinect.Open();
            
            this.InitializeComponent();

            //画面上のImageタグの画像ソースを、先ほど生成したWritableBitmap形式の変数bitmapに設定します。
            //これにより、bitmapが更新されるたびに画面の表示も変更されます。
            theImage.Source = this.bitmap;
        }

        //フレームを取得するたびに実行されるイベントハンドラ。このイベントハンドラ内で画面描画を行います。
        void colorReader_FrameArrived(ColorFrameReader sender, ColorFrameArrivedEventArgs args)
        {
            //正常にColorFrameが取得できているかを格納する変数を宣言し、falseを代入します。
            bool colorFrameProcessed = false;

            //イベントハンドラの引数であるargs内にはフレームへの参照が格納されています。
            //その参照から実際のデータを取り出すために、AcquireFrameを実行します。
            using(ColorFrame colorframe = args.FrameReference.AcquireFrame())
            {
                //実際にデータが取得できている場合には処理を進めます。
                if(colorframe!=null)
                {
                    //取得したフレームの情報を取りだします。
                    FrameDescription frameDescription = colorframe.FrameDescription;

                    //もしMainPage()コンストラクタ内で指定した形式(幅・高さ)でデータが取得できている場合には処理を進めます。
                    if(frameDescription.Width == this.bitmap.PixelWidth && frameDescription.Height == this.bitmap.PixelHeight)
                    {
                        //カラーフレームのイメージフォーマットがBGRAであればそのまま生のデータを渡します
                        if(colorframe.RawColorImageFormat == ColorImageFormat.Bgra)
                        {
                            colorframe.CopyRawFrameDataToBuffer(this.bitmap.PixelBuffer);
                        }
                        //もしBGRA形式で無い場合には、BGRA形式へ変換後データを渡します
                        else
                        {
                            colorframe.CopyConvertedFrameDataToBuffer(this.bitmap.PixelBuffer, ColorImageFormat.Bgra);
                        }
                        colorFrameProcessed = true;
                    }
                }
            }
            if(colorFrameProcessed)
            {
                //bitmap全体を描画(再描画)します
                this.bitmap.Invalidate();
            }
        }
    }
}
