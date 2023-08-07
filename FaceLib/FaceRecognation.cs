using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DlibDotNet;
using DlibDotNet.Dnn;

using Dlib = DlibDotNet.Dlib;

namespace FaceLib
{


    public class FaceRecognition : IDisposable
    {
        private FrontalFaceDetector faceDetector;
        private ShapePredictor shapePredictor;
        private LossMetric faceRecognition;

        private FaceRecognition(string modelsPath)
        {
            // Load the face detector
            faceDetector = Dlib.GetFrontalFaceDetector();

            // Load the shape predictor
            shapePredictor = ShapePredictor.Deserialize(modelsPath + "/shape_predictor_68_face_landmarks.dat");

            // Load the face recognition model
            faceRecognition = LossMetric.Deserialize(modelsPath + "/dlib_face_recognition_resnet_model_v1.dat");
        }

        public static FaceRecognition Create(string modelsPath)
        {
            return new FaceRecognition(modelsPath);
        }
        public static Array2D<RgbPixel> LoadImage(Bitmap bitmap)
        {
            // Lock the bitmap data
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            // Create a byte array to hold the pixel data
            byte[] pixelData = new byte[bitmapData.Stride * bitmapData.Height];

            // Copy the pixel data from the bitmap to the byte array
            Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

            // Unlock the bitmap data
            bitmap.UnlockBits(bitmapData);

            // Convert the Bitmap to an Array2D<RgbPixel>
            Array2D<RgbPixel> img = Dlib.LoadImageData<RgbPixel>(pixelData, (uint)bitmap.Height, (uint)bitmap.Width, (uint)bitmapData.Stride);

            return img;
        }


        public IEnumerable<Matrix<float>> GetFaceEncodings(Array2D<RgbPixel> img)
        {
            List<Matrix<float>> faceEncodings = new List<Matrix<float>>();

            // Detect faces in the image
            var faces = faceDetector.Operator(img);

            foreach (var face in faces)
            {
                // Find the facial landmarks for each face
                FullObjectDetection shape = shapePredictor.Detect(img, face);

                // Extract the aligned face chip
                Array2D<RgbPixel> chip = Dlib.ExtractImageChip<RgbPixel>(img, Dlib.GetFaceChipDetails(shape, 150, 0.25));

                // Compute the 128D vector that describes the face
                OutputLabels<Matrix<float>> faceDescriptor = faceRecognition.Operator(new Matrix<RgbPixel>(chip));

                // Add the face encoding to the list
                faceEncodings.Add(faceDescriptor.First());
            }

            return faceEncodings;
        }



        public IEnumerable<DlibDotNet.Rectangle> GetFaceLocations(Array2D<RgbPixel> img)
        {
            var faceLocations = new List<DlibDotNet.Rectangle>();

            // Detect faces in the image
            var faces = faceDetector.Operator(img);

            foreach (var face in faces)
            {
                // Add the face location to the list
                faceLocations.Add(face);
            }

            return faceLocations;
        }
        public void Dispose()
        {
            faceDetector.Dispose();
            shapePredictor.Dispose();
            faceRecognition.Dispose();
        }
    }
}
