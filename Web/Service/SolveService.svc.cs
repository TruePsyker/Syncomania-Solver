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
        public List<SyncomaniaSolver.Direction> Solve( string input )
        {
            var gm = new SyncomaniaSolver.GameMap();

            try {
                if ( gm.LoadMap( input ) == false )
                    return null;

                var gs = gm.Solve_AStar();

                return HistoryDumper( gs );
            } catch { }
                
            return null;
        }

        static List<SyncomaniaSolver.Direction> HistoryDumper( SyncomaniaSolver.GameState stateAtFinish )
        {
            if ( stateAtFinish.IsFinished() )
            {
                List<SyncomaniaSolver.Direction> moves = new List<SyncomaniaSolver.Direction>();
                foreach ( var state in stateAtFinish.History )
                {
                    moves.Add( state.moveDir );
                }
                return moves;
            }
            else
            {
                return null;
            }
        }
    }
}
