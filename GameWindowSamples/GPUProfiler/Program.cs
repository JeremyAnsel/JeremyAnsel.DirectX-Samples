using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUProfiler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var deviceResources = new RenderTargetDeviceResources(1920, 1080);
                var device = deviceResources.D3DDevice;
                var context = deviceResources.D3DContext;

                Console.WriteLine(deviceResources.AdapterDescription.AdapterDescription);

                var queryDisjoint = device.CreateQuery(new D3D11QueryDesc(D3D11QueryType.TimestampDisjoint));
                var queryStart = device.CreateQuery(new D3D11QueryDesc(D3D11QueryType.Timestamp));
                var queryEnd = device.CreateQuery(new D3D11QueryDesc(D3D11QueryType.Timestamp));
                var queryEvent = device.CreateQuery(new D3D11QueryDesc(D3D11QueryType.Event));

                context.Begin(queryDisjoint);
                context.End(queryStart);

                var mainGameComponent = new MainGameComponent();
                mainGameComponent.CreateDeviceDependentResources(deviceResources);
                mainGameComponent.CreateWindowSizeDependentResources();
                mainGameComponent.Update(null);
                mainGameComponent.Render();

                deviceResources.Present();

                context.End(queryEvent);

                while (context.GetData(queryEvent))
                {
                    System.Threading.Thread.Sleep(1);
                }

                context.End(queryEnd);
                context.End(queryDisjoint);

                long timestampStart;
                while (context.GetData(queryStart, D3D11AsyncGetDataOptions.None, out timestampStart))
                {
                    Console.WriteLine("timestampStart sleep");
                    System.Threading.Thread.Sleep(1);
                }

                long timestampEnd;
                while (context.GetData(queryEnd, D3D11AsyncGetDataOptions.None, out timestampEnd))
                {
                    Console.WriteLine("timestampEnd sleep");
                    System.Threading.Thread.Sleep(1);
                }

                D3D11QueryDataTimestampDisjoint disjointData;
                while (context.GetData(queryDisjoint, D3D11AsyncGetDataOptions.None, out disjointData))
                {
                    Console.WriteLine("disjointData sleep");
                    System.Threading.Thread.Sleep(1);
                }

                Console.WriteLine("timestampStart: {0}", timestampStart);
                Console.WriteLine("timestampEnd: {0}", timestampEnd);

                if (disjointData.IsDisjoint)
                {
                    Console.WriteLine("disjointData is disjoint");
                }
                else
                {
                    Console.WriteLine("frequency: {0}", disjointData.Frequency);

                    long delta = timestampEnd - timestampStart;
                    double time = (delta * 1000.0) / disjointData.Frequency;
                    Console.WriteLine("delta: {0}", delta);
                    Console.WriteLine("time: {0}", time);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
