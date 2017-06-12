using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SyncomaniaSolver
{
    class Program
    {


        static void Main( string[] args )
        {
            //#if RELEASE
            //            if (args.Length < 1)
            //                return;

            //            var mapFileName = args[0];

            //            string data;
            //            using (StreamReader sr = new StreamReader(mapFileName))
            //            {
            //                data = sr.ReadToEnd();
            //            }
            //#else
            var data = level_8;
            //#endif

            GameMap map = new GameMap();
            map.LoadMap( data );

            //var state = map.Solve_BFS();
            var state = map.Solve_AStar();

            SolutionDumper.Dump( state );

            Console.ReadKey();
        }

        static string[] test_map = new string[] { "@@#> #",
                                                  "_# e< ",
                                                  "  >^  " };

        static string[] level_2 = new string[] { "       ",
                                                "@# # #@",
                                                " # # # ",
                                                " # # # ",
                                                "   e   ",
                                                " # # # ",
                                                " # # # ",
                                                "@# # #@",
                                                "       " };

        static string[] level_8 = new string[] { "###     ###",
                                                 "#     #   #",
                                                 "##@   # @##",
                                                 "##    #  ##",
                                                 "## ##### ##",
                                                 "     e     ",
                                                 "## ##### ##",
                                                 "##  #    ##",
                                                 "##@ #   @##",
                                                 "#   #     #",
                                                 "###     ###" };
        // TODO:
        // Detect and consider diagonal symmetry
        // AntiActors
        // Block and AA pushing
    }


}
