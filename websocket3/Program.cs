using System;

namespace websocket3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("웹 소켓 서버를 시작합니다.");

            wsServer ws = new wsServer("127.0.0.1", 8081);
        }
    }
}
