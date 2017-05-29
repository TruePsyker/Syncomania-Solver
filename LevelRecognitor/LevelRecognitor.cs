using System;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

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

            public float Affinity( Vector3 right )
            {
                var dist = ( right.x - x ) * ( right.x - x ) + ( right.y - y ) * ( right.y - y ) + ( right.z - z ) * ( right.z - z );

                return Math.Max( 0.0f, 1.0f - 5.0f * dist );
            }
        }

        enum EColorIndex
        {
            Blue = 0,
            Red,
            Green,
            White,
            COUNT
        }

        struct Symbol
        {
            public MapTile.TileType tileType;
            public MovingObject.ObjectType objectType;
            public EColorIndex colorIdx;
            public int maskIdx;
        }

        public const int LevelExtents = 11;
        public const int LevelExtents2 = LevelExtents * LevelExtents;

        const int dataSizeX = 400;
        const int dataSizeY = 400;

        const float WeightThreshold = 0.72f;

        static Vector3 Blue = new Vector3( 0.467f, 0.56f, 0.6f );
        static Vector3 Red = new Vector3( 1.0f, 0.212f, 0.144f );
        static Vector3 Green = new Vector3( 0.481f, 1.0f, 0.0f );
        static Vector3 White = new Vector3( 1.0f, 1.0f, 1.0f );
        static Vector3[] Colors = new Vector3[(int)EColorIndex.COUNT] { Blue, Red, Green, White };

        static Symbol[] symbols = new Symbol[10] { new Symbol { tileType = MapTile.TileType.Block, colorIdx = EColorIndex.Blue, maskIdx = 0 }, // Block
                                                   new Symbol { tileType = MapTile.TileType.Exit, colorIdx = EColorIndex.Green, maskIdx = 0 }, // Exit
                                                   new Symbol { tileType = MapTile.TileType.Trap, colorIdx = EColorIndex.Red, maskIdx = 0 }, // Trap
                                                   new Symbol { objectType = MovingObject.ObjectType.Actor, colorIdx = EColorIndex.White, maskIdx = 1 }, // Actor
                                                   new Symbol { objectType = MovingObject.ObjectType.AntiActor, colorIdx = EColorIndex.Red, maskIdx = 1 }, // AntiActor
                                                   new Symbol { objectType = MovingObject.ObjectType.Box, colorIdx = EColorIndex.Blue, maskIdx = 6 }, // Box
                                                   new Symbol { tileType = MapTile.TileType.PusherLeft, colorIdx = EColorIndex.White, maskIdx = 2 }, // Pusher left
                                                   new Symbol { tileType = MapTile.TileType.PusherUp, colorIdx = EColorIndex.White, maskIdx = 3 }, // Pusher up
                                                   new Symbol { tileType = MapTile.TileType.PusherRight, colorIdx = EColorIndex.White, maskIdx = 4 }, // Pusher right
                                                   new Symbol { tileType = MapTile.TileType.PusherDown, colorIdx = EColorIndex.White, maskIdx = 5 },}; // Pusher down

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

            float[][,] inputData = new float[Colors.Length][,];
            for ( int i = 0; i < inputData.Length; i++ )
                inputData[i] = new float[dataSizeY, dataSizeX];

            for ( int y = 0; y < dataSizeY; y++ )
            {
                for ( int x = 0; x < dataSizeX; x++ )
                {
                    var normInput = NormalizeInput( dest.GetPixel( x, y ) ); ;
                    for ( int i = 0; i < inputData.Length; i++ )
                        inputData[i][y,x] = Colors[i].Affinity( normInput );
                }
            }

            // TODO: Store ready-for-use float data in project
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var masksBmp = new Bitmap( myAssembly.GetManifestResourceStream( "LevelRecognitor.masks_34.png" ) );
            var frameSize = masksBmp.Width;
            var masks = new float[masksBmp.Height / frameSize][,];
            var biases = new float[masks.Length];
            for ( int idx = 0; idx < masks.Length; idx++ )
            {
                masks[idx] = new float[frameSize, frameSize];
                for ( int y = 0; y < frameSize; y++ )
                {
                    for ( int x = 0; x < frameSize; x++ )
                    {
                        var maskValue = masksBmp.GetPixel( x, y + idx * frameSize ).R / 127.5f - 1.0f; // [0, 255] to [-1.0f, 1.0f]
                        masks[idx][y, x] = maskValue;
                        if ( maskValue > 0.0f )
                            biases[idx] += maskValue;
                    }
                }
            }

            var sizeX = dataSizeX - frameSize;
            var sizeY = dataSizeY - frameSize;

#if DUMP_RESULT
            var result = new float[symbols.Length][,];
            for ( int i = 0; i < result.Length; i++ )
                result[i] = new float[sizeY, sizeX];
#endif
            ILevelEncoder encoder = new DefaultLevelEncoder();

            var defValue = encoder.EncodeTile( MapTile.TileType.Empty, MovingObject.ObjectType.None );
            StringBuilder output = new StringBuilder(LevelExtents2);
            output.Length = LevelExtents2;
            for ( int i = 0; i < LevelExtents2; i++ )
                output[i] = defValue;

            var sw = Stopwatch.StartNew();
            for ( int symbolIdx = 0; symbolIdx < symbols.Length; symbolIdx++ )
            {
                var symbol = symbols[symbolIdx];
                var symWeights = masks[symbol.maskIdx];
#if DUMP_RESULT
                var symResults = result[symbolIdx];
#endif
                var symInput = inputData[(int)symbol.colorIdx];
                var symBiases = biases[symbol.maskIdx];

                // TODO: Go through all 'tiles' instead
                //for ( int frameY = 0; frameY < sizeY; frameY++ )
                Parallel.For( 0, sizeY, frameY =>
                {
                    var tilePos = ( frameY * LevelExtents / sizeY ) * LevelExtents;
                    for ( int frameX = 0; frameX < sizeX; frameX++ )
                    {
                        var tileX = frameX * LevelExtents / sizeX;
                        if ( output[tilePos + tileX] != defValue )
                        {
                            frameX = ( tileX + 1 ) * sizeX / LevelExtents;
                            continue;
                        }

                        float weight = 0.0f;

                        for ( int y = 0; y < frameSize; y++ ) // Matmul (input,masks)
                        {
                            for ( int x = 0; x < frameSize; x++ )
                            {
                                weight += symWeights[y, x] * symInput[y + frameY, x + frameX];
                            }
                        }
                        if ( weight / symBiases > WeightThreshold )
                        {
                            output[tilePos + tileX] = encoder.EncodeTile( symbol.tileType, symbol.objectType );
                            frameX = ( tileX + 1 ) * sizeX / LevelExtents;
                        }
#if DUMP_RESULT
                        symResults[frameY, frameX] = weight;
#endif
                    }
                } );
            } //);
            Console.WriteLine( String.Format( "Elapsed time: {0} ms", sw.ElapsedMilliseconds ) );
#if DUMP_RESULT
            var dumpOutput = new Bitmap( dataSizeX - frameSize, sizeY * symbols.Length, System.Drawing.Imaging.PixelFormat.Format32bppRgb );
            var hbmp = dumpOutput.LockBits( new Rectangle( 0, 0, dumpOutput.Width, dumpOutput.Height ), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb );
            var perMaskOffset = sizeX * sizeY;
            for ( int symbolIdx = 0; symbolIdx < symbols.Length; symbolIdx++ )
            {
                var symBiases = biases[symbols[symbolIdx].maskIdx];
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

            dumpOutput.UnlockBits( hbmp );
            dumpOutput.Save( "result.png", System.Drawing.Imaging.ImageFormat.Png );
#endif
            Success = true;
            Output = output.ToString();
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
