using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

using DlibDotNet;

namespace FaceLib
{
    public static class Extentions
    {
        public static Bitmap ResizeBitmap(this Bitmap bitmap, int maxWidth, int maxHeight)
        {
            int newWidth, newHeight;

            // Calculate the new dimensions while maintaining aspect ratio
            if (bitmap.Width > bitmap.Height)
            {
                newWidth = maxWidth;
                newHeight = (int)(bitmap.Height * ((float)newWidth / bitmap.Width));
            }
            else
            {
                newHeight = maxHeight;
                newWidth = (int)(bitmap.Width * ((float)newHeight / bitmap.Height));
            }

            // Create a new bitmap with the desired dimensions
            Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);

            // Resize the original bitmap to the new dimensions
            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }

            return resizedBitmap;
        }
        public static void SaveToFile(this List<FaceEncoding> faceMats, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var faceMat in faceMats)
                {
                    string encryptedData = EncryptData(faceMat);
                    writer.WriteLine($"{faceMat.Id}|{faceMat.Row}|{faceMat.Column}|{encryptedData}");
                }
            }
        }

        public static List<FaceEncoding> LoadFromFile(this List<FaceEncoding> faceMats, string filePath)
        {

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 4)
                    {
                        int id = int.Parse(parts[0]);
                        int row = int.Parse(parts[1]);
                        int col = int.Parse(parts[2]);
                        string encryptedData = parts[3];

                        float[] data = DecryptData(encryptedData);

                        Matrix<float> matrix = new Matrix<float>(data, row, col);

                        FaceEncoding faceMat = new FaceEncoding(matrix, id);
                        faceMats.Add(faceMat);
                    }
                }
            }

            return faceMats;
        }

        private static string EncryptData(FaceEncoding faceMat)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(string.Join(",", faceMat.Data));
            return Convert.ToBase64String(plainBytes);
        }

        private static float[] DecryptData(string encryptedData)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
            string decryptedString = Encoding.UTF8.GetString(encryptedBytes);
            return Array.ConvertAll(decryptedString.Split(','), float.Parse);
        }
    }
}
