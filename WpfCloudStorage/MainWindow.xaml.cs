using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace WpfCloudStorage
{
    /// <summary>
    /// Interaction logic for MainScreen.xaml
    /// 
    /// It is assumed that the pixels comprising the image are stored 
    /// from left to right, and top to bottom, 
    ///     - as unsigned short values - for 16-bit images.
    ///     - as bytes - for 8-bit images.
    ///     
    /// The image may be rectangular - a dialog is displayed prompting the 
    ///  user to enter the width and height.
    ///  
    /// Since the image size may be bigger than the displayed area, the image 
    ///  object is within a canvas object, which is again within a scrollviewer.
    /// 
    /// The algorithm is briefly as follows:
    ///  1. Read the raw image file and store the pixel values into the 
    ///     appropriate arrays - either byte or ushorts.
    ///     
    ///  2. Create a BitmapSource object of the required dimensions. In WPF, 
    ///     it is possible to specify the pixel format as Gray8 or Gray16. 
    ///     Just set the Image object on the main form to the BitmapSource.    ///     
    /// </summary>
    /// 
    public partial class MainWindow : Window
	{  
        ushort[] pix16;
        int stride;
        BitmapSource bmps;
        
        public List<string> FileTypes { get; set; } = new List<string> {"PNG", "JPG", "WMP"};

        public MainWindow()
		{
			InitializeComponent();
            DataContext = this;

			Loaded += MainWindow_Loaded;
        }

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
            tbWidth.Text  = "800";
            tbHeight.Text = "600";

            cbSaveAs.IsEnabled = true;

            canvas.Width = img.Width = Convert.ToInt32(tbWidth.Text);
            canvas.Height = img.Height = Convert.ToInt32(tbHeight.Text);

            tbFileName.Text = "Lena16_800x600-1.raw";
            tbAccessKey.Text = "your key";
            tbConteinerName.Text = "uploadimage";

            cbSaveAs.ItemsSource = FileTypes;
            bnUpLoad16.IsEnabled = false;
        }

		private void bnOpen16_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Raw Files(*.raw)|*.raw";

			Nullable<bool> result = ofd.ShowDialog();
			string fileName = "";

			if (result == true)
			{
				fileName = ofd.FileName;
				DisplayImage16(fileName);
			}
		}

        private void DisplayImage16(string fileName)
        {
            // Open a binary reader to read in the pixel data. 
            // We cannot use the usual image loading mechanisms since this is raw 
            // image data.
            try
            {
                int widthFrame = Convert.ToInt32(tbWidth.Text);
                int heightFrame = Convert.ToInt32(tbHeight.Text);

                if (img.Width != 0 && img.Height != 0)
                {
                    BinaryReader br = new BinaryReader(File.Open(fileName, FileMode.Open));
                    ushort pixShort;
                    int i;
                    long iTotalSize = br.BaseStream.Length;
                    int iNumberOfPixels = (int)(iTotalSize / 2);

                    pix16 = new ushort[iNumberOfPixels];

                    for (i = 0; i < iNumberOfPixels; ++i)
                    {
                        pixShort = (ushort)(br.ReadUInt16());
                        pix16[i] = pixShort;
                    }

                    br.Close();
                    
                    int bitsPerPixel = 16;
                    stride = (widthFrame * bitsPerPixel + 7) / 8;

                    // Single step creation of the image
                    bmps = BitmapSource.Create(widthFrame, heightFrame, 96, 96, PixelFormats.Gray16, null,
                        pix16, stride);
                    img.Source = bmps;

                    tbFileName.Text = fileName;

                    bnSaveAs.IsEnabled = true;
                    bnUpLoad16.IsEnabled = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
  
        private void img_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvas.Width = img.ActualWidth;
            canvas.Height = img.ActualHeight;
        }

		private async void bnLoad16_Click(object sender, RoutedEventArgs e)
		{
            int widthFrame = Convert.ToInt32(tbWidth.Text);
            int heightFrame = Convert.ToInt32(tbHeight.Text);
            
            if (widthFrame != 0 && heightFrame != 0)
            {
                pbBusy.IsIndeterminate = true;

                var pix16 = await DataService.Load16(tbFileName.Text.Trim(), tbAccessKey.Text.Trim());


                if (pix16.Count() != 0)
                {
                    int bitsPerPixel = 16;
                    stride = (widthFrame * bitsPerPixel + 7) / 8;

                    // Single step creation of the image
                    bmps = BitmapSource.Create(widthFrame, heightFrame, 96, 96, PixelFormats.Gray16, null,
                        pix16, stride);
                    img.Source = bmps;

                    bnSaveAs.IsEnabled = true;
                    bnUpLoad16.IsEnabled = true;
                }
				else 
                {   
                    MessageBox.Show($"File {tbFileName.Text.Trim()} was not found in the conteiner {tbConteinerName.Text}!");

                    img.Source = null;
                    bnSaveAs.IsEnabled = false;
                    bnUpLoad16.IsEnabled = false;
                }

                pbBusy.IsIndeterminate = false;
               
            }
            else
            {
                bnSaveAs.IsEnabled = false;;
            }
        }

        private void bnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            string fileType = (string)cbSaveAs.SelectedValue;

            BitmapEncoder encoder = null;

            switch (fileType)
            {
                case "PNG":
                    encoder = new PngBitmapEncoder();
                    break;
                case "JPG":
                    encoder = new JpegBitmapEncoder();
                    break;
                case "WMP":
                    encoder = new WmpBitmapEncoder();
                    break;
                default:
                    return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = $"{fileType} Images (.{fileType.ToLower()})|*.{fileType.ToLower()}";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();
            string targetPath = "";

            // Process save file dialog box results
            if (result == true)
            {
                // Save image
                targetPath = dlg.FileName;
                FileStream fs = new FileStream(targetPath, FileMode.Create);
                encoder.Frames.Add(BitmapFrame.Create(bmps));
                encoder.Save(fs);
                fs.Close();
            }
        }

        private void bnClear_Click(object sender, RoutedEventArgs e)
		{
            img.Source = null;
            bnSaveAs.IsEnabled = false;
            bnUpLoad16.IsEnabled = false;
        }

		private async void bnUpLoad16_Click(object sender, RoutedEventArgs e)
		{
            if (bmps == null) 
            {
                MessageBox.Show($"Pls open (Open button) a raw file to upload locally!");
            }
            else if (string.IsNullOrEmpty(tbConteinerName.Text))
            {
                MessageBox.Show($"Pls enter a target Azure contener name");
            }
            else 
            {
                pbBusy.IsIndeterminate = true;

                // 16gray
                byte[] targetPixels = new byte[2 * bmps.PixelHeight * bmps.PixelWidth];
                bmps.CopyPixels(targetPixels, (bmps.PixelWidth * 16 + 7) / 8, 0);

                bool res = await DataService.UpLoad16(tbFileName.Text.Trim(), tbAccessKey.Text.Trim(), tbConteinerName.Text.Trim(), targetPixels);

                pbBusy.IsIndeterminate = false;

                if (res == true)
				{
                    MessageBox.Show($"File {tbFileName.Text.Trim()} has been saved successfully in the container {tbConteinerName.Text.Trim()}!");
                }
            }
        }
	}	
}
