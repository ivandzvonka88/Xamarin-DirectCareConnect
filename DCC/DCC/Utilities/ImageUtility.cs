using System;

using System.IO;

using System.Drawing;
using System.Drawing.Imaging;
using DCC.Models;
namespace DCC
{

    public class picSize
    {
        public int phHt;
        public int phtnHt;
        public int phWd;
        public int phtnWd;
    }

    

    public class ImageUtility
    {
        static private Int32  MAXWIDTH = 1600;
        static private Int32 MAXHEIGHT = 1600;


    static public void ResizeImage(Stream ImageStream, ref MemoryStream ms)
        {
            Image UploadedImage = Image.FromStream(ImageStream);
            EncoderParameters myParams = new EncoderParameters(1);
            myParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)95);
            Int32 height = UploadedImage.Height;
            Int32 width = UploadedImage.Width;
            resize(ref width, ref height, MAXWIDTH, MAXHEIGHT);
            Bitmap Img = new Bitmap(width, height);
            Graphics h = Graphics.FromImage(Img);
            h.DrawImage(UploadedImage, 0, 0, width, height);
            Img.Save(ms, GetEncoderInfo("image/jpeg"), myParams);
            h.Dispose();
            Img.Dispose();
            UploadedImage.Dispose();
        }



        static private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            int j = 0;
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            while (j < encoders.Length)
            {
                if (encoders[j].MimeType == mimeType) return encoders[j];
                System.Threading.Interlocked.Increment(ref j);
            }
            return null;
        }


        static private void resize(ref int imageWidth, ref int imageHeight, int maxWidth, int maxHeight)
        {
            if (imageWidth >= maxWidth || imageHeight >= maxHeight)
            {
                // image size exceeds or is equal to what we allow
                if ((imageWidth > maxWidth))
                {
                    imageHeight = (int)(imageHeight * ((decimal)maxWidth / (decimal)imageWidth));
                    imageWidth = maxWidth;
                }
                if ((imageHeight > maxHeight))
                {
                    imageWidth = (int)(imageWidth * ((decimal)maxHeight / (decimal)imageHeight));
                    imageHeight = maxHeight;
                }
            }
            else
            {
                decimal ratioH = (decimal)maxHeight / (decimal)imageHeight;
                decimal ratioW = (decimal)maxWidth / (decimal)imageWidth;
                if (ratioW > ratioH)
                {
                    // limited by height
                    if ((decimal)imageHeight * (decimal)1.8 < (decimal)maxHeight)
                    {
                        imageHeight = (int)((decimal)imageHeight * 1.8M);
                        imageWidth = (int)((decimal)imageWidth * 1.8M);
                    }
                    else
                    {
                        imageHeight = maxHeight;
                        imageWidth = (int)((decimal)imageWidth * ratioH);
                    }
                }
                else
                {
                    // limited by width
                    if ((decimal)imageWidth * (decimal)1.8 < (decimal)maxWidth)
                    {
                        imageWidth = (int)((decimal)imageWidth * 1.8M);
                        imageHeight = (int)((decimal)imageHeight * 1.8M);
                    }
                    else
                    {
                        imageWidth = maxWidth;
                        imageHeight = (int)((decimal)imageHeight * ratioW);
                    }
                }

            }
        }

    }
}
