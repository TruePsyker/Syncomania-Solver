using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncomaniaSolver
{
    public static class SolutionDumper
    {
        public static void Dump( SolutionState solution )
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
