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

            int cnt = 0;
            foreach ( var ch in recognitor.Output )
            {
                Console.Write( ch );
                if ( ++cnt == LevelRecognitor.LevelExtents )
                {
                    cnt = 0;
                    Console.Write( '\n' );
                }
            }

            GameMap map = new GameMap();
            map.LoadMap( recognitor.Output );

            var state = map.Solve_AStar();

            DumpHistory( state );

            Console.ReadKey();
        }

        static void DumpHistory( SolutionState solution )
        {
            Console.WriteLine( "Iterations: {0}", solution.IterationsCount );
            Console.WriteLine( "Unique states created: {0}", solution.UniqueStatesCount );
            Console.WriteLine( "Max front states count: {0}", solution.MaxFrontStatesCount );
            Console.WriteLine( "Elapsed time: {0} ms", solution.ElapsedTime );

            var stateAtFinish = solution.State;

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
