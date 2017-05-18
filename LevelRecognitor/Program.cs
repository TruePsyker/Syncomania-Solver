using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncomaniaSolver
{
    class Program
    {
        static void Main( string[] args )
        {
            var recognitor = new LevelRecognitor( args[0] );

            if ( recognitor.Success == false )
                return;
            //if ( recognitor.Success )
            //{
            //    Console.WriteLine( recognitor.Output );
            //}

            int cnt = 0;
            foreach ( var ch in recognitor.Output )
            {
                Console.Write( ch );
                if ( ++cnt == LevelRecognitor.LevelSize )
                {
                    cnt = 0;
                    Console.Write( '\n' );
                }
            }
            Console.ReadKey();

            if ( recognitor.Success == false )
                return;

            GameMap map = new GameMap();
            map.LoadMap( recognitor.Output );

            var state = map.Solve_AStar();

            DumpHistory( state );

            Console.ReadKey();
        }

        static void DumpHistory( GameState stateAtFinish )
        {
            if ( stateAtFinish.IsFinished() )
            {
                Console.WriteLine( "Solution turns count: {0}", stateAtFinish.turn );
                foreach ( var state in stateAtFinish.History )
                {
                    Console.WriteLine( state.ToString() );
                }
            }
            else
            {
                Console.WriteLine( "No solution found." );
            }
        }
    }
}
