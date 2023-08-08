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

namespace FaceLibWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BitmapSource Preview
        {
            get { return (BitmapSource)GetValue(PreviewProperty); }
            set { SetValue(PreviewProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preview.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviewProperty =
            DependencyProperty.Register("Preview", typeof(BitmapSource), typeof(MainWindow), new PropertyMetadata(null));

        public bool FaceDetected
        {
            get { return (bool)GetValue(FaceDetectedProperty); }
            set { SetValue(FaceDetectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FaceDetected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FaceDetectedProperty =
            DependencyProperty.Register("FaceDetected", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
