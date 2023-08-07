using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
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
        private List<FaceEncoding> faceEncodings = new List<FaceEncoding>();

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
        public static Array2D<RgbPixel> LoadImage(System.Drawing.Bitmap bitmap)
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

        public void LoadFaces(string facesFolderPath)
        {
            IEnumerable<FaceImage> faces = Directory.GetFiles(facesFolderPath).Select(x => new FaceImage { Id = Path.GetFileNameWithoutExtension(x), Path = x });
            LoadFaces(faces);
        }
        public void LoadFaces(IEnumerable<FaceImage> faces)
        {
            faceEncodings.Clear();
            foreach (FaceImage face in faces)
            {
                try
                {
                    using (Array2D<RgbPixel> frImage = Dlib.LoadImage<RgbPixel>(face.Path))
                    {
                        // Generate the face encoding for the face in the image
                        Matrix<float> faceEncoding = GetFaceEncodings(frImage).FirstOrDefault();
                        if (faceEncoding != null)
                            faceEncodings.Add(new FaceEncoding(faceEncoding, face.Id));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public System.Drawing.Bitmap GetFaceBitmap(Stream imageStream)
        {
            // Load the image into a bitmap
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(imageStream))
            {
                // Load the image using FaceRecognitionDotNet
                using (Array2D<RgbPixel> frImage = LoadImage(bitmap))
                {
                    // Use FaceRecognitionDotNet to find face locations
                    IEnumerable<Rectangle> faceLocations = GetFaceLocations(frImage);

                    if (faceLocations.Count() > 0)
                    {
                        // Get the first detected face location
                        Rectangle faceLocation = faceLocations.First();

                        // Extract the face region from the image
                        using (System.Drawing.Bitmap faceImage = bitmap.Clone(new System.Drawing.Rectangle(faceLocation.Left, faceLocation.Top, faceLocation.Right - faceLocation.Left, faceLocation.Bottom - faceLocation.Top), bitmap.PixelFormat))
                        {
                            return faceImage.ResizeBitmap(256, 256);
                        }
                    }

                    // No face detected, return null or handle accordingly
                    return null;
                }
            }
        }
        public FaceEncoding GetFaceEncoding(System.Drawing.Bitmap image)
        {
            // Convert the Bitmap to a FaceRecognitionDotNet.Image
            using (Array2D<RgbPixel> frImage = LoadImage(image))
            {
                // Generate the face encoding for the face in the image
                using (Matrix<float> faceEncoding = GetFaceEncodings(frImage).FirstOrDefault())
                {
                    if (faceEncoding != null)
                    {
                        return new FaceEncoding(faceEncoding);
                    }
                }
                return null;
            }
        }
        public Rectangle GetFaceLocation(System.Drawing.Bitmap bitmap)
        {
            // Load the image using FaceRecognitionDotNet
            using (Array2D<RgbPixel> targetImage = LoadImage(bitmap))
            {
                try
                {
                    IEnumerable<Rectangle> locations = GetFaceLocations(targetImage);
                    return locations.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return default(Rectangle);
                }
            }
        }
        public void AddOrUpdateFace(System.Drawing.Bitmap image, string id)
        {
            // Convert the Bitmap to a FaceRecognitionDotNet.Image
            using (Array2D<RgbPixel> frImage = LoadImage(image))
            {
                // Generate the face encoding for the face in the image
                Matrix<float> faceEncoding = GetFaceEncodings(frImage).FirstOrDefault();
                if (faceEncoding != null)
                {
                    // Add the face encoding and id to the list of saved encodings
                    if (faceEncodings.FirstOrDefault(x => x.Id == id) is FaceEncoding faceEncodingModel)
                    {
                        faceEncodings[faceEncodings.IndexOf(faceEncodingModel)] = new FaceEncoding(faceEncoding, id);
                    }
                    else
                    {
                        faceEncodings.Add(new FaceEncoding(faceEncoding, id));
                    }
                }
            }
        }
        public string GetIdFromFace(System.Drawing.Bitmap image)
        {
            string id = null;
            double minDifference = double.MaxValue;
            using (Array2D<RgbPixel> searchImage = LoadImage(image))
            {
                IEnumerable<Matrix<float>> searchEncodings = GetFaceEncodings(searchImage);
                if (searchEncodings.FirstOrDefault() is Matrix<float> searchEncoding)
                    foreach (FaceEncoding targetEncoding in faceEncodings)
                    {
                        try
                        {
                            using (Matrix<float> matrix = new FaceEncoding(searchEncoding).Matrix - targetEncoding.Matrix)
                            {
                                double difference = Dlib.Length(matrix);
                                if (minDifference > difference && difference < 0.6)
                                {
                                    minDifference = difference;
                                    id = targetEncoding.Id;
                                }
                            }
                        }
                        finally
                        {
                            GC.Collect();
                        }
                    }
                foreach (Matrix<float> encoding in searchEncodings) encoding.Dispose();
                return id;
            }
        }
        public string GetIdFromFace(FaceEncoding faceEncoding)
        {
            string id = null;
            double minDifference = double.MaxValue;
            foreach (FaceEncoding targetEncoding in faceEncodings)
            {
                try
                {
                    using (Matrix<float> matrix = faceEncoding.Matrix - targetEncoding.Matrix)
                    {
                        double difference = Dlib.Length(matrix);
                        if (minDifference > difference && difference < 0.6)
                        {
                            minDifference = difference;
                            id = targetEncoding.Id;
                        }
                    }
                }
                finally
                {
                    GC.Collect();
                }
            }
            return id;
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



        public IEnumerable<Rectangle> GetFaceLocations(Array2D<RgbPixel> img)
        {
            var faceLocations = new List<Rectangle>();

            // Detect faces in the image
            Rectangle[] faces = faceDetector.Operator(img);

            foreach (Rectangle face in faces)
            {
                // Add the face location to the list
                faceLocations.Add(face);
            }

            return faceLocations;
        }
        public void Dispose()
        {
            faceEncodings.Clear();
            faceDetector.Dispose();
            shapePredictor.Dispose();
            faceRecognition.Dispose();
        }
    }
}
