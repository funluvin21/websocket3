using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

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
            Console.WriteLine("웹소켓 서버를 오픈합니다.!!!");

            // 클라이언트 접속 대기
            listner.BeginAcceptTcpClient(OnServerConnect, null);
            Console.WriteLine("클라이언트와의 접속을 기다립니다.");

        }

        void OnServerConnect(IAsyncResult ar)
        {
            client = listner.EndAcceptTcpClient(ar);
            Console.WriteLine("한 클라이언트가 접속했습니다.");

            // 클라이언트 접속을 다시 대기
            listner.BeginAcceptTcpClient(OnServerConnect, null);

            //현재의 클라이언트로 부터 데이터를 받아 온다.
            clientStream = client.GetStream();
            clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onAcceptReader, null);

        }

        // 클라이언트의 데이터를 읽어 오는 메소드
        void onAcceptReader(IAsyncResult ar)
        {
            //받은 데이터의 길리을 확인합니다.
            int receiverLength = clientStream.EndRead(ar);

            //받은 데이터가 없는 경우는 접속이 끊어진 경우 입니다.
            if (receiverLength <= 0)
            {
                Console.WriteLine("접속이 끊어 졌습니다.");
                return;
            }

            //받은 메세지를 출력합니다.
            string newMessage = Encoding.UTF8.GetString(readBuffer, 0, receiverLength);
            Console.WriteLine(string.Format("받은 메세지 : \n {0}", newMessage));

            // 첫 3문자가 GET으로 시작하지 않는 경우, 잘못된 접속이므로 종료합니다.
            if (!Regex.IsMatch(newMessage,"GET"))
            {
                Console.WriteLine("잘못된 접속입니다.");
                client.Close();
                return;
            }

            //클라이언트로 응답을 돌려 줍니다.
            const string eoi = "\r\n";   //HTTP/1.1 defines sequence CR LF as end-of-line marker

            //보낼 메세지
            string resMessage = "HTTP/1.1 101 Switching Protocols " + eoi
                              + "Connection: Upgrade " + eoi
                              + "Upgrade: websocket " + eoi
                              + "Sec-WebSocket-Accept: "
                              + Convert.ToBase64String(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(new Regex("Sec-WebSocket-Key: (.*)").Match(newMessage).Groups[1].Value.Trim() + "258EAFA5-E914-470DA-95CA-C5AB0DC85B11")))
                              + eoi
                              + eoi;

            // 보낸 메세지를 출력해 봅니다.
            Console.WriteLine(string.Format("보낸 메세지 : \n {0}", resMessage));

            //메세지를 보내 줍니다.
            Byte[] response = Encoding.UTF8.GetBytes(resMessage);
            clientStream.Write(response, 0, response.Length);

            //에코 메세지 받기 시작
            //clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onEchoReader, null);
                 
        }
        /*
        //에코 메세지를 받아오는 부분
        void onEchoReader(IAsyncResult ar)
        {
            //받은 데이터의 길이를 확인합니다.
            int receiveLength = clientStream.EndRead(ar);

            //받은 데이터가 6인 경우는 종료 상태 일 뿐이므로, 종료 데이터를 보내고 우리도 접속을 종료합니다.
            if (receiveLength == 6)
            {
                Console.WriteLine("접속 해제 요청이 와 접속을 종료합니다. ");
                client.Close();
                return;
            }

            //받은 데이터가 없는 경우는 접속이 끊어진 경우 입니다.
            if(receiveLength <= 0)
            {
                Console.WriteLine(" 접속이 끊어졌습니다. ");
                return;
            }

            BitArray maskingCheck = new BitArray(new byte[] { readBuffer[1] });
            int receivedSize = (int)readBuffer[1];

            byte[] mask = new byte[] { readBuffer[2], readBuffer[3], readBuffer[4], readBuffer[5] };

            if (maskingCheck.Get(0))
            {
                Console.WriteLine("마스킹 되어 있습니다. ");
                receivedSize -= 128;   //마스킹으로 인해 추가된 값을 빼 줍니다.
            }
            else
            {
                Console.WriteLine("마스킹 되어 있지 않습니다. ");
            }

            //문자열을 길이를 파악합니다.
            Console.WriteLine("받은 데이터 길이 비트 : {0}", receivedSize);
            Console.WriteLine("받은 데이터 길이 : {0}",receiveLength);

            //받은 메세지 디코딩
            byte[] decodedByte = new byte[receivedSize];
            for ( int _i = 0; _i < receivedSize; _i++ )
            {
                int curIndex = _i + 6;
                decodedByte[_i] = (byte)(readBuffer[curIndex] ^ mask[_i % 4]);
            }

            //받은 메세지 출력
            //string newMessage = Encoding.UTF8.GetString(readBuffer, 6, receiveLength - 6);
            string newMessage = Encoding.UTF8.GetString(decodedByte, 0, receivedSize);
            Console.WriteLine(string.Format("받은 메세지: {0}", newMessage));

            string sendSource = "Success!!!!!!";

            byte[] sendMessage = Encoding.UTF8.GetBytes(sendSource);

            //보낼 메세지를 만들기
            List<byte> sendByteList = new List<byte>();

            //첫 데이터의 정보르 ㄹ만들어 추가 합니다.
            BitArray firstInfor = new BitArray(
                new bool[]
                {
                    true,    //FIN
                    false,   //RSV1
                    false,   //RSV2
                    false,   //RSV3

                    //opcode (0x01 :텍스트)
                    false,
                    false,
                    false,
                    true
                }
                );

            byte[] inforByte = new byte[1];
            firstInfor.CopyTo(inforByte, 0);
            sendByteList.Add(inforByte[0]);

            //문자열의 길이를 추가합니다.
            sendByteList.Add((byte)sendMessage.Length);

            //실제 데이터를 추가 합니다.
            sendByteList.AddRange(sendMessage);

            //보낸 메세지 출력
            Console.WriteLine(string.Format("보낸메세지:\n{0}",sendSource));

            //받은 메시지를 그대로 보내 줍니다.
            clientStream.Write(sendByteList.ToArray(), 0, sendByteList.Count);
            Console.WriteLine(string.Format("보낸메세지 길이:{0}",sendByteList.Count));

            //또 다음 메세지를 받을 수 있도록 대기 합니다.
            clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onEchoReader, null);

        }
        */
    }
}
