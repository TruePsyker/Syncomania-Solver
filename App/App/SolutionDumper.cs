using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncomaniaSolver
{
    public static class SolutionDumper
    {
        public static string Dump( SolutionState solution )
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine( string.Format( "Iterations: {0}", solution.IterationsCount ) );
            sb.AppendLine( string.Format( "Unique states created: {0}", solution.UniqueStatesCount ) );
            sb.AppendLine( string.Format( "Max front states count: {0}", solution.MaxFrontStatesCount ) );
            sb.AppendLine( string.Format( "Elapsed time: {0} ms", solution.ElapsedTime ) );

            var stateAtFinish = solution.State;

            if ( stateAtFinish.IsFinished() )
            {
                sb.AppendLine( string.Format( "Solution turns count: {0}", stateAtFinish.turn ) );
                foreach ( var state in stateAtFinish.History )
                {
                    sb.AppendLine( state.ToString() );
                }
            }
            else
            {
                sb.AppendLine( "No solution found." );
            }

            return sb.ToString();
        }
    }
}
