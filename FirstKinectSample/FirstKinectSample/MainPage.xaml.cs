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

        public MainPage()
        {
            //Kinect センサー オブジェクトを取得します。
            this.kinect = KinectSensor.GetDefault();

            //Kinect センサーを起動します。
            this.kinect.Open();
            
            this.InitializeComponent();
        }
    }
}
