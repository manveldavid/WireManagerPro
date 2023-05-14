using QRCodeEncoderLibrary;

namespace WireManager.Common
{
    internal class QRCodeSaver
	{
		public QREncoder encoder;
		public QRCodeSaver() 
		{ 
			encoder = new QREncoder();
		}

		public void SaveQRCodeAsPng(string data, string path, int moduleSize = 20) 
		{
			QRSavePngImage pngImage = new(encoder.Encode(data));
			pngImage.ModuleSize = moduleSize;
			pngImage.SaveQRCodeToPngFile(path);
		}
	}
}
