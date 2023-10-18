using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Controls
{
    internal class GamePageViewModel : IQueryAttributable
    {
        public int? level { get; private set; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            level = query[key: "Level"] as int?;
        }
    }
}
