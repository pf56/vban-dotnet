using System.Net;
using System.Net.Sockets;
using NetCoreServer;

#nullable enable
namespace VBANdotnet.CLI;


public class AudioClient : UdpServer
{
	public AudioClient(IPAddress address, int port) : base(address, port)
	{ }

	protected override void OnStarted()
	{
		// Start receive datagrams
		ReceiveAsync();
	}

	protected override void OnReceived(EndPoint endpoint, byte[] receiveBytes, long offset, long size)
	{
		try
		{
			//VBANHeader header = VBANHeader.Read(receiveBytes);

			for(int f = 28; f < size; f += 4)
			{
				AudioData audioData = new()
				{
					Left = (short)(receiveBytes[f + 1] << 8 | receiveBytes[f]),
					Right = (short)(receiveBytes[f + 3] << 8 | receiveBytes[f + 2])
				};


				Program.writerPosition += 1;
				Program.writerPosition %= Program.Buffer.Length;
				Program.Buffer[Program.writerPosition] = audioData;

				//Console.WriteLine("Written: " + Program.writerPosition + " " + audioData.Left + " " + audioData.Right);
			}
		}
		catch(Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		// Continue receive datagrams
		ReceiveAsync();
	}

	protected override void OnError(SocketError error)
	{
		Console.WriteLine($"Echo UDP client caught an error with code {error}");
	}
}