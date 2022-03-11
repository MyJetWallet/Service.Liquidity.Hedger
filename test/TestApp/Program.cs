using System;
using ProtoBuf.Grpc.Client;
using Service.Liquidity.Hedger.Client;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();


            var factory = new LiquidityHedgerClientFactory("http://localhost:5001");


            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}