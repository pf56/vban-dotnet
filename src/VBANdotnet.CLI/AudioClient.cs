using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;

#nullable enable
namespace VBANdotnet.CLI;


public class AudioClient : UdpServer
{
	public AudioClient(IPAddress address, int port) : base(address, port)
	{
		Writer = Program.Data.Writer;
	}

	private PipeWriter Writer;
	private bool _stop;

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

			Memory<byte> memory = Writer.GetMemory((int)size);
			int i = 0;

			for(int f = 28; f < size; f += 4)
			{
				AudioData audioData = new()
				{
					Left = (short)(receiveBytes[f + 1] << 8 | receiveBytes[f]),
					Right = (short)(receiveBytes[f + 3] << 8 | receiveBytes[f + 2])
				};

				AudioData.Write(audioData).CopyTo(memory[i..(i+4)]);

				i += 4;
			}


			Writer.Advance((int)(size - 28));
			_ = Writer.FlushAsync();
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