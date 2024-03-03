// USINGS

#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Text;
using UdpClient = NetCoreServer.UdpClient;

namespace VBANdotnet.CLI;


public class ScreamClient : UdpClient
{
    public string Multicast;

    public ScreamClient(string address, int port) : base(address, port)
    { }

    public void DisconnectAndStop()
    {
        _stop = true;
        Disconnect();
        while(IsConnected)
            Thread.Yield();
    }

    protected override void OnConnected()
    {
        Console.WriteLine($"Multicast UDP client connected a new session with Id {Id}");

        // Join UDP multicast group
        JoinMulticastGroup(Multicast);

        // Start receive datagrams
        ReceiveAsync();
    }

    protected override void OnDisconnected()
    {
        Console.WriteLine($"Multicast UDP client disconnected a session with Id {Id}");

        // Wait for a while...
        Thread.Sleep(1000);

        // Try to connect again
        if(!_stop)
            Connect();
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        Console.WriteLine("Incoming: " + size);

        for(int f = 5; f < size; f += 8)
        {
            AudioData audioData = new()
            {
                Left = (short) (buffer[f + 3] << 24 | buffer[f + 2] << 16 | buffer[f + 1] << 8 | buffer[f]),
                Right = (short) (buffer[f + 7] << 24 | buffer[f + 6] << 16 | buffer[f + 5] << 8 | buffer[f + 4])
            };


            Program.writerPosition += 1;
            Program.writerPosition %= Program.Buffer.Length;
            Program.Buffer[Program.writerPosition] = audioData;

            //Console.WriteLine("Written: " + Program.writerPosition + " " + audioData.Left + " " + audioData.Right);
        }

        // Continue receive datagrams
        ReceiveAsync();
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Multicast UDP client caught an error with code {error}");
    }

    private bool _stop;

}
