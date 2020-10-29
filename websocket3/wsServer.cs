using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace websocket3
{
    class wsServer
    {
        //초기화 시 입력 받는 값들.
        public string addr = "";
        public int port = 0;

        //읽기에 사용되는 값들
        NetworkStream clientStream = null;
        byte[] readBuffer = new byte[500000];

        //동작에 사용되는 멤버 변수들.
        TcpListener listner = null;
        TcpClient client = null;

        //생성자 만들기
        public wsServer(string _addr, int _port)
        {
            //입력받은 값들 저장
            addr = _addr;
            port = _port;

            listner = new TcpListener(IPAddress.Parse(addr), port);

            listner.Start();
            Console.WriteLine("웹소켓 서버를 오픈합니다.");

            // 클라이언트 접속 대기
            listner.BeginAcceptTcpClient(OnServerConnect, null);

            //현재의 클라이언트로 부터 데이터를 받아 온다.
            clientStream = client.GetStream();
            clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onAcceptReader, null);



        }

        void OnServerConnect(IAsyncResult ar)
        {
            int receiveLength = clientStream.EndRead(ar);

        }

        private void onAcceptReader(IAsyncResult ar)
        {
            
        }


    }
}
