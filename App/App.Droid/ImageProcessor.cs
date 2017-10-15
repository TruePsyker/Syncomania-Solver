using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using static SyncomaniaSolver.LevelRecognitor;
using Android.Media;
using System.Reflection;

namespace App.Droid
{
    public static class ImageProcessor
    {
        public static float[][,] GetInputDataFromImage( Bitmap bmp )
        {
            if ( bmp == null )
                return null;

            var dest = Bitmap.CreateScaledBitmap( bmp, InputSize, InputSize * bmp.Height / bmp.Width, true );

            float[][,] inputData = new float[Colors.Length][,];
            for ( int i = 0; i < inputData.Length; i++ )
                inputData[i] = new float[InputSize, InputSize];

            for ( int y = 0; y < InputSize; y++ )
            {
                for ( int x = 0; x < InputSize; x++ )
                {
                    var normInput = NormalizeInput( new Color( dest.GetPixel( x, y ) ) );
                    for ( int i = 0; i < inputData.Length; i++ )
                        inputData[i][y,x] = Colors[i].Affinity( normInput );
                }
            }
            return inputData;
        }

        public static void GetConvolutionFilters( out int frameSize, out float[][,] masks, out float[] biases )
        {
            // TODO: Store ready-for-use float data in project
            //System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var myAssembly = typeof( SyncomaniaSolver.LevelRecognitor ).GetTypeInfo().Assembly;
            var masksBmp = BitmapFactory.DecodeStream( myAssembly.GetManifestResourceStream( "App.masks_34.png" ) );
            frameSize = masksBmp.Width;
            masks = new float[masksBmp.Height / frameSize][,];
            biases = new float[masks.Length];
            for ( int idx = 0; idx < masks.Length; idx++ )
            {
                masks[idx] = new float[frameSize, frameSize];
                for ( int y = 0; y < frameSize; y++ )
                {
                    for ( int x = 0; x < frameSize; x++ )
                    {
                        var maskValue = new Color( masksBmp.GetPixel( x, y + idx * frameSize )).R / 127.5f - 1.0f; // [0, 255] to [-1.0f, 1.0f]
                        masks[idx][y, x] = maskValue;
                        if ( maskValue > 0.0f )
                            biases[idx] += maskValue;
                    }
                }
            }
        }

        private static Vector3 NormalizeInput( Color input )
        {
            return new Vector3 { x = Math.Max( input.R - 106, 0.0f ) / 149.0f,
                                 y = Math.Max( input.G - 106, 0.0f ) / 149.0f,
                                 z = Math.Max( input.B - 106, 0.0f ) / 149.0f };
        }

        //private void DumpConvolutionProduct( float[][,] result, float[] biases )
        //{
        //    var size = result[0].Length( 0 );
        //    var dumpOutput = new Bitmap( size, size * symbols.Length, System.Drawing.Imaging.PixelFormat.Format32bppRgb );
        //    var hbmp = dumpOutput.LockBits( new Rectangle( 0, 0, dumpOutput.Width, dumpOutput.Height ), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb );
        //    var perMaskOffset = size * size;
        //    for ( int symbolIdx = 0; symbolIdx < symbols.Length; symbolIdx++ )
        //    {
        //        var symBiases = biases[symbols[symbolIdx].maskIdx];
        //        for ( int frameY = 0, baseOffsetY = symbolIdx * perMaskOffset * 4; frameY < size; frameY++, baseOffsetY += size * 4 )
        //        {
        //            for ( int frameX = 0, baseOffset = baseOffsetY; frameX < size; frameX++, baseOffset+=4 )
        //            {
        //                var weight = result[symbolIdx][frameY, frameX];
        //                weight /= symBiases;
        //                weight = Math.Min( Math.Max( 0, weight ), 1);
        //                weight *= 255.0f;
        //                int compW = (int)weight; compW |= compW << 8; compW |= compW << 16;

        //                System.Runtime.InteropServices.Marshal.WriteInt32( hbmp.Scan0, baseOffset, compW );
        //            }
        //        }
        //    }

        //    dumpOutput.UnlockBits( hbmp );
        //    dumpOutput.Save( "result.png", System.Drawing.Imaging.ImageFormat.Png );
        //}
    }
}