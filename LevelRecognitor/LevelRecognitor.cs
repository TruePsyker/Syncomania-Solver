using System;
using System.Text;
using System.Drawing;

namespace SyncomaniaSolver
{
    public class LevelRecognitor
    {
        struct Vector3
        {
            public float x;
            public float y;
            public float z;

            public Vector3( float value )
            {
                x = value;
                y = value;
                z = value;
            }

            public Vector3( float x, float y, float z )
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public void Set( float value )
            {
                x = value;
                y = value;
                z = value;
            }

            public static explicit operator float( Vector3 value )
            {
                return value.x + value.y + value.z;
            }

            public static Vector3 operator*( Vector3 left, Vector3 right )
            {
                return new Vector3{ x = left.x * right.x, y = left.y * right.y, z = left.z * right.z };
            }

            public float Dot( Vector3 right )
            {
                return x * right.x + y * right.y + z * right.z;
            }

            public static Vector3 Min( Vector3 left, float right )
            {
                return new Vector3{ x = Math.Min(left.x, right), y = Math.Min(left.y, right), z = Math.Min(left.z, right) };
            }

            public static Vector3 Max( Vector3 left, Vector3 right )
            {
                return new Vector3{ x = Math.Max(left.x, right.x), y = Math.Max(left.y, right.y), z = Math.Max(left.z, right.z) };
            }
        }

        struct Symbol
        {
            public int symbolIdx;
            public Vector3 colorWeight;
            public int maskIdx;
        }

        public const int LevelSize = 11;

        const int dataSizeX = 400;
        const int dataSizeY = 400;

        static Symbol[] symbols = new Symbol[10] { new Symbol { symbolIdx = 0, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 0 }, // Block
                                                   new Symbol { symbolIdx = 1, colorWeight = new Vector3( -1, 1, -1 ), maskIdx = 0 }, // Exit
                                                   new Symbol { symbolIdx = 2, colorWeight = new Vector3( 1, -1, -1 ), maskIdx = 0 }, // Trap
                                                   new Symbol { symbolIdx = 3, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 1 }, // Actor
                                                   new Symbol { symbolIdx = 4, colorWeight = new Vector3( 1, -1, -1 ), maskIdx = 1 }, // AntiActor
                                                   new Symbol { symbolIdx = 5, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 6 }, // Box
                                                   new Symbol { symbolIdx = 6, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 2 }, // Pusher left
                                                   new Symbol { symbolIdx = 7, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 3 }, // Pusher up
                                                   new Symbol { symbolIdx = 8, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 4 }, // Pusher right
                                                   new Symbol { symbolIdx = 9, colorWeight = new Vector3( 1, 1, 1 ), maskIdx = 5 },}; // Pusher down

        public LevelRecognitor( string fileName )
        {
            var bmp = new Bitmap( fileName );
            if ( bmp == null )
                return;

            var dest = new Bitmap( dataSizeX, dataSizeY );
            Graphics g = Graphics.FromImage( dest );

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.DrawImage( bmp, new Rectangle( 0, 0, dataSizeX, dataSizeY ), 0, 0, bmp.Width, bmp.Width, GraphicsUnit.Pixel );

            Vector3[,] inputData = new Vector3[dataSizeY, dataSizeX];
            for ( int y = 0; y < dataSizeY; y++ )
            {
                for ( int x = 0; x < dataSizeX; x++ )
                {
                    inputData[y, x] = NormalizeInput( dest.GetPixel( x, y ) );
                }
            }

            // TODO: Store ready-for-use float data in project
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var masksBmp = new Bitmap( myAssembly.GetManifestResourceStream( "LevelRecognitor.masks_34.png" ) );
            var frameSize = masksBmp.Width;
            var masks = new float[masksBmp.Height / frameSize][,];
            for ( int idx = 0; idx < masks.Length; idx++ )
            {
                masks[idx] = new float[frameSize, frameSize];
                for ( int y = 0; y < frameSize; y++ )
                {
                    for ( int x = 0; x < frameSize; x++ )
                    {
                        masks[idx][y, x] = masksBmp.GetPixel( x, y + idx * frameSize ).R / 127.5f - 1.0f; // [0, 255] to [-1.0f, 1.0f]
                    }
                }
            }

            var weights = new Vector3[symbols.Length][,];
            var biases = new float[symbols.Length];
            for ( int idx = 0; idx < weights.Length; idx++ )
            {
                weights[idx] = new Vector3[frameSize, frameSize];
                for ( int y = 0; y < frameSize; y++ )
                {
                    for ( int x = 0; x < frameSize; x++ )
                    {
                        var maskValue = masks[symbols[idx].maskIdx][y, x];
                        weights[idx][y, x] = Vector3.Min( symbols[idx].colorWeight, maskValue );
                        if ( maskValue > 0.5f )
                            biases[idx] += maskValue;
                    }
                }
            }

            var sizeX = dataSizeX - frameSize;
            var sizeY = dataSizeY - frameSize;

            var perMaskOffset = sizeX * sizeY;

            var result = new float[symbols.Length][,];
            for ( int i = 0; i < result.Length; i++ )
                result[i] = new float[sizeY, sizeX];

            for ( int symbolIdx = 0; symbolIdx < symbols.Length; symbolIdx++ )
            {
                var symWeights = weights[symbolIdx];
                var symResults = result[symbolIdx];
                for ( int frameY = 0; frameY < sizeY; frameY++ )
                {
                    for ( int frameX = 0; frameX < sizeX; frameX++ )
                    {
                        float weight = 0.0f;

                        for ( int y = 0; y < frameSize; y++ ) // Matmul (input,masks)
                        {
                            for ( int x = 0; x < frameSize; x++ )
                            {
                                weight += inputData[y + frameY, x + frameX].Dot( symWeights[y, x] );
                            }
                        }

                        symResults[frameY, frameX] = weight;
                    }
                }
                Console.WriteLine( symbolIdx.ToString() );
            }

            var output = new Bitmap( dataSizeX - frameSize, sizeY * symbols.Length, System.Drawing.Imaging.PixelFormat.Format32bppRgb );
            var hbmp = output.LockBits( new Rectangle( 0, 0, output.Width, output.Height ), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb );

            for ( int symbolIdx = 0; symbolIdx < symbols.Length; symbolIdx++ )
            {
                var symBiases = biases[symbolIdx];
                for ( int frameY = 0, baseOffsetY = symbolIdx * perMaskOffset * 4; frameY < sizeY; frameY++, baseOffsetY += sizeX * 4 )
                {
                    for ( int frameX = 0, baseOffset = baseOffsetY; frameX < sizeX; frameX++, baseOffset+=4 )
                    {
                        var weight = result[symbolIdx][frameY, frameX];
                        weight /= symBiases;
                        weight = Math.Min( Math.Max( 0, weight ), 1);
                        weight *= 255.0f;
                        int compW = (int)weight; compW |= compW << 8; compW |= compW << 16;

                        System.Runtime.InteropServices.Marshal.WriteInt32( hbmp.Scan0, baseOffset, compW );
                    }
                }
            }

            output.UnlockBits( hbmp );
            output.Save( "result.png", System.Drawing.Imaging.ImageFormat.Png );
        }

        private Vector3 NormalizeInput( Color input )
        {
            return new Vector3 { x = Math.Max( input.R - 106, 0.0f ) / 149.0f,
                                 y = Math.Max( input.G - 106, 0.0f ) / 149.0f,
                                 z = Math.Max( input.B - 106, 0.0f ) / 149.0f };
        }

        public bool Success { get; private set; }

        public string Output { get; private set; }
    }
}
