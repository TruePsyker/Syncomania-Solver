using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
//using Android.Net;
using Android.Graphics;
using SyncomaniaSolver;

namespace App.Droid
{
    [IntentFilter( new string[] { Intent.ActionSend }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType =  "image/*" )]
    [Activity(Label = "Syncomania Solver", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if ( Intent.Action == Intent.ActionSend )
            {
                SetContentView (Resource.Layout.Main);
                var textView = FindViewById<TextView> (Resource.Id.textView1);

                Android.Net.Uri imageUri = Intent.GetParcelableExtra( Intent.ExtraStream ) as Android.Net.Uri;

                textView.Text = "Processing image...";

                Bitmap bmp = null;
                using ( var stream = ContentResolver.OpenInputStream( imageUri ) )
                {
                    bmp = BitmapFactory.DecodeStream( stream );
                }

                var inputData = ImageProcessor.GetInputDataFromImage( bmp );
                int frameSize;
                float[][,] masks;
                float[] biases;
                ImageProcessor.GetConvolutionFilters( out frameSize, out masks, out biases );
                var lr = new LevelRecognitor( inputData, frameSize, masks, biases );

                if ( lr.Success == false )
                    return;

                var map = new GameMap();
                map.LoadMap( lr.Output );

                var state = map.Solve_AStar();

                textView.Text = SyncomaniaSolver.SolutionDumper.Dump( state );


                //using (var webClient = new System.Net.WebClient())
                //{
                //     var imageBytes = webClient.DownloadData(imageUri);
                //     if (imageBytes != null && imageBytes.Length > 0)
                //     {
                //          bmp = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                //     }
                //}

                //Log.Debug( "Share", Intent.Type );
            }
        }
    }
}


