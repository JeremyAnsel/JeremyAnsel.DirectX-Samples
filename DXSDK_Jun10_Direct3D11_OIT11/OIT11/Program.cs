using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OIT11
{
    class Program
    {
        static void Main(string[] args)
        {
            var game = new MainGameWindow();
            game.BuildWindow(WindowConstant.UseDefault, WindowConstant.UseDefault, 480, 360, IntPtr.Zero, false);
            game.Run();
        }
    }
}
