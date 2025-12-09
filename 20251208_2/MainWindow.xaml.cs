using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OpenCvSharp;
using System.Windows.Threading;
using System.Printing;
using Microsoft.Win32;
using OpenCvSharp.WpfExtensions;

namespace _20251208_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window     // OpenCV 써서 더 명확하게 알려줘야함
    {
        private Mat _currentImage;

        private VideoCapture _capture;
        private DispatcherTimer _timer;
        private bool _isCameraRunning = false;
        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();     // 30FPS
            _timer.Interval = TimeSpan.FromMicroseconds(33);
            _timer.Tick += Timer_Tick;
        }

        private enum ProcessMode
        {
            Original,
            Gray,
            Canny,
            Binary
        }
        // 현재 선택된 콤보박스 값 조회
        private ProcessMode GetCurrentMode()    // 자료형이 ProcessMode 를 반환하는 함수
        {
            ComboBoxItem item = CmbMode.SelectedItem as ComboBoxItem;   // CmbMode 콤보박스 이름

            if (item != null) 
            {
                string mode = item.Content.ToString();

                if(mode == "Gray")
                {
                    return ProcessMode.Gray;
                }
                if (mode == "Canny Edge")
                {
                    return ProcessMode.Canny;
                }
                if (mode == "Binary(Threshold)")
                {
                    return ProcessMode.Binary;
                }
            }
            return ProcessMode.Original;
        }

        // 현재 모드에 맞게 영상에서 나온 이미지를 처리하여 Mat 형식으로 반환
        private Mat ApplyProcess(Mat src)
        {
            ProcessMode mode = GetCurrentMode();

            if (mode == ProcessMode.Original)
            {
                return src.Clone();
            }

            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            
            if (mode == ProcessMode.Gray)
            {                
                return gray;
            }
            if (mode == ProcessMode.Canny)
            {
                Mat canny = new Mat();
                Cv2.Canny(gray, canny, 50, 150);
                gray.Dispose();         // gray 삭제
                return canny;
            }
            if (mode == ProcessMode.Binary)
            {
                Mat binary = new Mat();
                Cv2.Threshold(gray, binary, 100, 255, ThresholdTypes.Binary);
                gray.Dispose();         // gray 삭제
                return binary;
            }
            gray.Dispose();         // gray 삭제
            return src.Clone();     // 혹시나 모를 조건 안맞을 경우 리턴값
        }

        // 이미지 불러오기
        private void BtnLoadImage_Click(object sender, EventArgs e)
        {
            StopCamera();
            // 카메라에 이미지 가지고 올 예정
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                if(_currentImage != null)
                {
                    _currentImage.Dispose();
                }
                _currentImage = Cv2.ImRead(dialog.FileName);

                if (_currentImage.Empty())
                {
                    MessageBox.Show("이미지를 불러올 수 없습니다.");
                    return;
                }
                ImgOriginal.Source = BitmapSourceConverter.ToBitmapSource(_currentImage);    
            }
        }
        private void BtnStartCamera_Click(object sender, EventArgs e) 
        {
            try
            {
                if (_isCameraRunning)
                {
                    return;
                }
                _capture = new VideoCapture(0);
                // 0 기본캠
                // 1 외부캠

                if (!_capture.IsOpened())
                {
                    // 연결이 안되면
                    
                    _capture.Release();
                    _capture.Dispose();
                    _capture = null;
                    return;
                }
                // 연결이 잘 되었다!
                _isCameraRunning = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("카메라 시작 중 오류:" + ex.Message);
            }

            
        }
        private void BtnStopCamera_Click(object sender, EventArgs e)
        {
            StopCamera();
        }

        private void StopCamera()
        {
            if (!_isCameraRunning)
            {
                return;
            }
            _timer.Stop();
            if (_capture != null)
            {
                _capture.Release();
                _capture.Dispose();
                _capture = null;
            }
            _isCameraRunning = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if(_capture == null || !_capture.IsOpened())
                return;     // 한줄이면 {} 안써도 됨
            
            Mat frame = new Mat();
            bool ok = _capture.Read(frame);

            if(!ok || frame.Empty())
            {
                frame.Dispose();
                return;
            }
            // 여기서 현재 프레임을 _currentImage에 저장해 줌
            if (_currentImage != null)
            {
                _currentImage.Dispose();
                _currentImage = null;
            }
            _currentImage = frame.Clone();  // 현재 프레임 복사해서 저장

            ImgOriginal.Source = BitmapSourceConverter.ToBitmapSource(frame);

            using (Mat processed = ApplyProcess(frame))     // (안에 있는게 다 끝나면) {이거 안에 있는거 실행시켜줘} 순서 보장 (델리게이트)
            {
                ImgProcessed.Source = BitmapSourceConverter.ToBitmapSource(processed);
            }
            frame.Dispose();
        }

        //private void CmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (_currentImage != null && !_currentImage.Empty())
        //    {
        //        using (Mat processed = ApplyProcess(_currentImage))
        //        {
        //            ImgProcessed.Source = BitmapSourceConverter.ToBitmapSource(processed);
        //        }
        //    }
        //}
        private void CmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentImage != null && !_currentImage.Empty())
            {
                UpdateProcessedImage();
            }
        }
        private void UpdateProcessedImage()
        {
            if (_currentImage == null || _currentImage.Empty())
                return;

            using (Mat processed = ApplyProcess(_currentImage))
            {
                ImgProcessed.Source = BitmapSourceConverter.ToBitmapSource(processed);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            StopCamera();

            if (_currentImage != null)
            {
                _currentImage.Dispose();
                _currentImage = null;
            }
        }
    }
}