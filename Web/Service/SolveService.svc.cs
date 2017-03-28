using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Service
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени класса "WebService" в коде, SVC-файле и файле конфигурации.
    // ПРИМЕЧАНИЕ. Чтобы запустить клиент проверки WCF для тестирования службы, выберите элементы WebService.svc или WebService.svc.cs в обозревателе решений и начните отладку.
    public class SolveService : ISolveService
    {
        public string Solve( string input )
        {
            var gm = new SyncomaniaSolver.GameMap();

            try {
                if ( gm.LoadMap( input ) == false )
                    return string.Empty;

                var gs = gm.Solve_AStar();

                return HistoryDumper( gs );
            } catch { }
                
            return "An error has occured";
        }

        static string HistoryDumper( SyncomaniaSolver.GameState stateAtFinish )
        {
            if ( stateAtFinish.IsFinished() )
            {
                StringBuilder strSolution = new StringBuilder();
                strSolution.AppendLine( String.Format( "Solution turns count: {0}", stateAtFinish.turn ) );

                foreach ( var state in stateAtFinish.History )
                {
                    strSolution.AppendLine( state.ToString() );
                }
                return strSolution.ToString();
            }
            else
            {
                return "No solution found.";
            }
        }
    }
}
