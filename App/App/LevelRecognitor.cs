using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SyncomaniaSolver
{
    public class LevelRecognitor
    {
        public struct Vector3
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

        public const int InputSize = 400;
        const int borderWidth = 10;
        const int areaSize = 10;

        const float WeightThreshold = 0.72f;

        static Vector3 Blue = new Vector3( 0.467f, 0.56f, 0.6f );
        static Vector3 Red = new Vector3( 1.0f, 0.212f, 0.144f );
        static Vector3 Green = new Vector3( 0.481f, 1.0f, 0.0f );
        static Vector3 White = new Vector3( 1.0f, 1.0f, 1.0f );
        public static Vector3[] Colors = new Vector3[(int)EColorIndex.COUNT] { Blue, Red, Green, White };

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

        public bool Success { get; private set; }

        public Tuple<MapTile.TileType, MovingObject.ObjectType>[,] Output { get; private set; }

        public LevelRecognitor( float[][,] inputData, int frameSize, float[][,] masks, float[] biases )
        {
            var dataSize = InputSize - borderWidth * 2;

#if DUMP_RESULT
            var result = new float[symbols.Length][,];
            for ( int i = 0; i < result.Length; i++ )
                result[i] = new float[areaSize * LevelExtents, areaSize * LevelExtents];
#endif

            var defValue = Tuple.Create(MapTile.TileType.Empty, MovingObject.ObjectType.None );
            var output = new Tuple<MapTile.TileType,MovingObject.ObjectType>[LevelExtents, LevelExtents];
            for ( int row = 0; row < LevelExtents; row++ )
                for ( int col = 0; col < LevelExtents; col++ )
                    output[row,col] = defValue;

            var sw = Stopwatch.StartNew();
            Parallel.For( 0, LevelExtents2, tileIdx =>
            {
                for ( int symbolIdx = 0; symbolIdx < symbols.Length; symbolIdx++ )
                {
                    var symbol = symbols[symbolIdx];
                    var symWeights = masks[symbol.maskIdx];
#if DUMP_RESULT
                    var symResults = result[symbolIdx];
#endif
                    var symInput = inputData[(int)symbol.colorIdx];
                    var symBiases = biases[symbol.maskIdx];

                    var tileY = tileIdx / LevelExtents;
                    var tileX = tileIdx % LevelExtents;
                    var startY = borderWidth + tileY * dataSize / LevelExtents - areaSize / 2;

                    for ( int areaY = 0; areaY < areaSize; areaY++ )
                    {
                        var startX = borderWidth + tileX * dataSize / LevelExtents - areaSize / 2;
                        var endX = startX + areaSize;
                        var frameY = startY + areaY;
                        for ( int areaX = 0; areaX < areaSize; areaX++ )
                        {
                            float weight = 0.0f;
                            var frameX = startX + areaX;

                            for ( int y = 0; y < frameSize; y++ ) // Matmul (input,masks)
                                for ( int x = 0; x < frameSize; x++ )
                                    weight += symWeights[y, x] * symInput[y + frameY, x + frameX];
#if DUMP_RESULT
                            symResults[tileY * areaSize + areaY, tileX * areaSize + areaX] = weight;
#endif
                            if ( weight / symBiases > WeightThreshold )
                            {
                                output[tileY, tileX] = Tuple.Create( symbol.tileType, symbol.objectType );
                                return;
                            }
                        }
                    }
                }
            } );
            sw.Stop();
            //Logger.WriteLine( String.Format( "Elapsed time: {0} ms", sw.ElapsedMilliseconds ) );

#if DUMP_RESULT
            DumpConvolutionProduct( result, biases );
#endif
            Success = true;
            Output = output;
        }

        // Define Logger interface


    }
}
