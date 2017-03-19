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

            //var state = map.PathFind_BFS_Queue();
            var state = map.Solve_AStar();

            DumpPath( state );
            // state.DumpHistory( dumper )

            Console.ReadKey();
        }

        static void DumpPath( GameState stateAtFinish )
        {
            if ( stateAtFinish.IsFinished() )
            {
                List<GameState> ordered = new List<GameState>();
                while ( stateAtFinish.PrevState != null )
                {
                    ordered.Add( stateAtFinish );
                    stateAtFinish = stateAtFinish.PrevState;
                }

                for ( var idx = ordered.Count - 1; idx >= 0; idx-- )
                {
                    Console.WriteLine( ordered[idx].ToString() );
                }
                Console.WriteLine( "Solution turns count: {0}", ordered.Count );
            }
            else
            {
                Console.WriteLine( "No solution found." );
            }
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
        // Detect and consider diagonal symmetry
        // AntiActors
        // Block and AA pushing
    }


}
