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
            string[] map;
            if ( TryParseInput( input, out map ) == false )
                return string.Empty;

            var gm = new SyncomaniaSolver.GameMap();

            gm.LoadMap( map );
            var gs = gm.Solve_AStar();
            return "";
        }

        private bool TryParseInput( string input, out string[] map )
        {
            map = new string[0];
            return false;
        }
    }
}
