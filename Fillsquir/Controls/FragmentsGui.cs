using Fillsquir.Interfaces;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Controls
{
    internal class FragmentsGui: GeometryElement
    {
        internal FragmentsGui(GameSettings settings):base(settings)
        {
        }
        protected override void DrawMainShape(SKCanvas canvas)
        {
            throw new NotImplementedException();
        }
    }
}
