using System;
using System.Text;
using System.Drawing;

namespace SyncomaniaSolver
{
    class MapRecognitor
    {
        const int tilesCount = 11;
        const float mapBorderRelativeWidth = 18.0f / 720f;
        const float tileRelativeSize = 62.1f / 720f;
        const float staticObjectFeatureOffset = 10.0f / 720f;
        const float boxFeatureOffset = 15.0f / 720f;
        const float actorFeatureOffset = 18.0f / 720f;
        const float arrowFeaturesOffset = 6.0f / 720f;

        Color redPattern = Color.FromArgb( 255, 138, 128 );
        Color greenPattern = Color.FromArgb( 177, 255, 90 );
        Color whitePattern = Color.White;
        Color greyPattern = Color.FromArgb( 177, 190, 193 );

        static PointF[] arrowFeaturesPositions = { new PointF( -arrowFeaturesOffset, 0 ), new PointF( 0, arrowFeaturesOffset ),
                                                   new PointF( arrowFeaturesOffset, 0 ),  new PointF( 0, -arrowFeaturesOffset ) };

        MapRecognitor( string fileName )
        {
            var img = Image.FromFile( fileName );

            if ( img == null )
                return;

            var bmp = new Bitmap( img );

            var width = bmp.Width;
            var height = bmp.Height;

            var borderWidth = mapBorderRelativeWidth * width;

            StringBuilder map = new StringBuilder( tilesCount * tilesCount );
            for ( int y = 0; y < tilesCount; y++ )
            {
                for ( int x = 0; x < tilesCount; x++ )
                {
                    var tilex = mapBorderRelativeWidth + tileRelativeSize * x;
                    var tiley = mapBorderRelativeWidth + tileRelativeSize * y;

                    var px = ( tilex + staticObjectFeatureOffset ) * width;
                    var py = ( tiley + staticObjectFeatureOffset ) * width;
                    var probeColor = bmp.GetPixel( (int)( px + 0.5f ), (int)( py + 0.5f ) );

                    if ( IsSameColor( greyPattern, probeColor ) )
                    {
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.Block] );
                        continue;
                    }
                    else if ( IsSameColor( redPattern, probeColor ) )
                    {
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.Trap] );
                        continue;
                    }
                    else if ( IsSameColor( greenPattern, probeColor ) )
                    {
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.Exit] );
                        continue;
                    }

                    px = ( tilex + boxFeatureOffset ) * width;
                    py = ( tiley + boxFeatureOffset ) * width;
                    probeColor = bmp.GetPixel( (int)( px + 0.5f ), (int)( py + 0.5f ) );

                    if ( IsSameColor( greyPattern, probeColor ) )
                    {
                        // It is a box
                        map.Append( 'x' );
                    }
                    else
                    {
                        px = ( tilex + actorFeatureOffset ) * width;
                        py = ( tiley + actorFeatureOffset ) * width;
                        probeColor = bmp.GetPixel( (int)( px + 0.5f ), (int)( py + 0.5f ) );

                        if ( IsSameColor( redPattern, probeColor ) )
                        {
                            map.Append( 'o' );
                        }
                        else if ( IsSameColor( whitePattern, probeColor ) )
                        {
                            map.Append( 'a' );
                        }
                    }

                    // Check for pushers
                    bool[] arrowFeatures = { true, true, true, true };
                    for ( int i = 0; i < arrowFeaturesPositions.Length; i++ )
                    {
                        var srcx = (int)( ( tilex + tileRelativeSize * 0.5f + arrowFeaturesPositions[i].X ) * width + 0.5f );
                        var srcy = (int)( ( tiley + tileRelativeSize * 0.5f + arrowFeaturesPositions[i].Y ) * width + 0.5f );

                        var destx = (int)( ( tilex + tileRelativeSize * 0.5f + arrowFeaturesPositions[( i + 1 ) % 4].X ) * width + 0.5f );
                        var desty = (int)( ( tiley + tileRelativeSize * 0.5f + arrowFeaturesPositions[( i + 1 ) % 4].Y ) * width + 0.5f );

                        var dx = Math.Sign( destx - srcx );
                        var dy = Math.Sign( desty - srcy );
                        for ( ; srcx != destx && srcy != desty; srcx += dx, srcy += dy )
                        {
                            probeColor = bmp.GetPixel( srcx, srcy );

                            if ( IsSameColor( whitePattern, probeColor ) == false )
                            {
                                arrowFeatures[i] = false;
                                break;
                            }
                        }
                    }

                    if ( arrowFeatures[0] && arrowFeatures[1] )
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.PusherDown] );
                    else if ( arrowFeatures[1] && arrowFeatures[2] )
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.PusherRight] );
                    else if ( arrowFeatures[2] && arrowFeatures[3] )
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.PusherUp] );
                    else if ( arrowFeatures[3] && arrowFeatures[0] )
                        map.Append( MapTile.EncodedTiles[(int)MapTile.TileType.PusherLeft] );
                }
            }

            Success = true;

            Output = map.ToString();
        }

        private bool IsSameColor( Color refColor, Color color )
        {
            return Math.Abs( color.R - refColor.R ) < 10 && Math.Abs( color.G - refColor.G ) < 10 && Math.Abs( color.B - refColor.B ) < 10;
        }

        public bool Success { get; private set; }

        public string Output { get; private set; }
    }
}
