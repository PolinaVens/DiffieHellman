
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Numerics;
using System.Collections.Generic;
namespace Diffie_Hellman
{
    class key
    {
        public string status;
        public BigInteger g, p;//публичные числа
        int a;// секретное число
        public BigInteger A;//Промежуточный результат(g^a mod p)
        BigInteger Key = -1;// Конeчный ключ 

        public key(BigInteger g, BigInteger p)//Конструктор 
        {
            Random rand = new Random();
            this.g = g;
            this.p = p;
            this.a = rand.Next() % 7321;
            this.A = BigInteger.Pow(g, a) % p;
            this.status = "created";

        }
        public key()//Конструктор 
        {
            Random rand = new Random();
            this.g = rand.Next() % 96557;
            this.p = rand.Next() % 1405695061;
            this.a = rand.Next() % 7321;
            this.A = BigInteger.Pow(g, a) % p;
            this.status = "created";

        }
        public BigInteger GenerateKey(BigInteger B)//Генерируем ключ из промежуточного результата полученного от другой стороны
        {
            if (status != "done")
            {
                Key = BigInteger.Pow(B, a) % p;
                status = "done";
            }
            return Key;
        }
        public BigInteger GetKey() => this.Key;

    }

    class Program
    {


        static void Main(string[] args)
        {

            Console.WriteLine("Введите IP второго компьютера(Для ожидания подключения нажмите Enter)");
            string ip = Console.ReadLine();

            if (ip != "")
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//Создаем сокет
                IPEndPoint ippoint = new IPEndPoint(IPAddress.Parse(ip), 801);
                socket.Connect(ippoint);//Подключаемся к другому компьютеру


                key Key = new key();

                string text = JsonConvert.SerializeObject(Key);//Упаковываем экземпляр класса key в json
                byte[] buff = new byte[1024];
                buff = Encoding.Unicode.GetBytes(text);
                socket.Send(buff);//отправляем
                buff = new byte[1024];
                do
                {
                    socket.Receive(buff);
                }
                while (socket.Available > 0);//Получаем ответ

                dynamic Recive = JsonConvert.DeserializeObject(Encoding.Unicode.GetString(buff));

                Console.WriteLine("key=" + Key.GenerateKey(Convert.ToInt64(Recive["A"])));//Выводим ключ
                socket.Close();
            }
            else listen();
        }




        static void listen()
        {

            Dictionary<string, key> ListOfKeys = new Dictionary<string, key>();//Ассоциативный массив для хранения ключей
            Socket listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//Создаем сокет
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 801);
            listenSock.Bind(ipPoint);
            listenSock.Listen(10);//Начинаем слушать
            for (; ; )
            {
                Console.WriteLine("\nОжидаем подключение другого компьютера");
                Socket socket = listenSock.Accept();//Приняли подключение
                byte[] data = new byte[1024];

                do
                {
                    socket.Receive(data);
                }
                while (socket.Available > 0);//Получаем данные
                string text = Encoding.Unicode.GetString(data);
                key recive = JsonConvert.DeserializeObject<key>(text);

                if (recive.status == "created")
                {
                    key Key = new key(recive.g, recive.p);
                    text = JsonConvert.SerializeObject(Key);
                    socket.Send(Encoding.Unicode.GetBytes(text));//Отправляем промежуточный результат
                    Key.GenerateKey(recive.A);//Генерируем  закрытый ключ
                    ListOfKeys.Add(socket.RemoteEndPoint.ToString(), Key);
                    Console.Clear();


                }

                Console.WriteLine("Сохраненные ключи");//Выводим список ключей
                foreach (var dic in ListOfKeys)
                {

                    Console.WriteLine("ip:" + dic.Key + " private key:" + dic.Value.GetKey());
                }


                socket.Close();//Закрывааем сокет
            }
        }
    }
}
