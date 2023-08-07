
using DlibDotNet;

namespace FaceLib
{

    public class FaceEncoding
    {
        private Matrix<float> matrix;

        public string Id { get; set; }
        public float[] Data { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public Matrix<float> Matrix
        {
            get
            {
                if (matrix == null)
                {
                    matrix = new Matrix<float>(Data, Row, Column);
                }
                return matrix;
            }
            set => matrix = value;
        }
        public FaceEncoding()
        {
        }
        public FaceEncoding(Matrix<float> faceEncoding, string id = null)
        {
            Id = id;
            Data = faceEncoding.ToArray();
            Row = faceEncoding.Rows;
            Column = faceEncoding.Columns;
            Matrix = faceEncoding;
        }
    }
}
