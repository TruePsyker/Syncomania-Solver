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

            SolutionDumper.Dump( state );

            Console.ReadKey();
        }
    }
}
