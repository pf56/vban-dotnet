using System.Net.Sockets;
using System.Text;
using Xt;

public static class PrintSimple
{
	private static readonly XtMix Mix = new(48000, XtSample.Int16);
	private static readonly XtChannels Channels = new(0, 0, 2, 0);
	private static readonly XtFormat Format = new(Mix, Channels);

	public struct VBANHeader
	{
		static long[] VBAN_SRList =
		{
			6000, 12000, 24000, 48000, 96000, 192000, 384000,
			8000, 16000, 32000, 64000, 128000, 256000, 512000,
			11025, 22050, 44100, 88200, 176400, 352800, 705600
		};

		public string VBAN { get; private set; }
		public string Name { get; private set; }
		public long SampleRate { get; private set; }
		public int SubProtocol { get; private set; }
		public int NbSamples { get; private set; }
		public int NbChannels { get; private set; }
		public int BitResolution { get; private set; }
		public int Codec { get; private set; }

		private UInt32 _VBAN;
		private byte _SR;
		private byte _SAMPLES;
		private byte _CHANNELS;
		private byte _FORMAT;
		private char[] _NAME;
		private UInt32 _COUNTER;

		public byte[] Write()
		{
			MemoryStream stream = new();
			BinaryWriter writer = new(stream);

			writer.Write(_VBAN);
			writer.Write(_SR);
			writer.Write(_SAMPLES);
			writer.Write(_CHANNELS);
			writer.Write(_FORMAT);
			writer.Write(_NAME);
			writer.Write(_COUNTER);

			return stream.ToArray();
		}

		public static VBANHeader Read(byte[] bytes)
		{
			BinaryReader reader = new(new MemoryStream(bytes));
			VBANHeader header = default;

			header._VBAN = reader.ReadUInt32();
			header.VBAN = Encoding.ASCII.GetString(BitConverter.GetBytes(header._VBAN));

			header._SR = reader.ReadByte();
			header.SampleRate = VBAN_SRList[header._SR & 0b00011111];
			header.SubProtocol = header._SR & 0b11100000;

			header._SAMPLES = reader.ReadByte();
			header.NbSamples = header._SAMPLES + 1;

			header._CHANNELS = reader.ReadByte();
			header.NbChannels = header._CHANNELS + 1;

			header._FORMAT = reader.ReadByte();
			header.BitResolution = header._FORMAT & 0b00000111;
			header.Codec = header._FORMAT & 0b11110000;

			header._NAME = reader.ReadChars(16);
			header.Name = new string(header._NAME).TrimEnd('\u0000');

			header._COUNTER = reader.ReadUInt32();

			return header;
		}
	};

	[STAThread]
	public static void Main()
	{
		// setup XtAudio
		using XtPlatform platform = XtAudio.Init(null, IntPtr.Zero);
		XtSystem system = platform.SetupToSystem(XtSetup.ConsumerAudio);
		XtService service = platform.GetService(system);
		if(service == null) return;

		// get default output device
		string defaultOutput = service.GetDefaultDeviceId(true);
		if(defaultOutput == null) return;
		using XtDevice device = service.OpenDevice(defaultOutput);
		if(!device.SupportsFormat(Format)) return;

		short[] mybuffer = new short[1024*1024*1];
		bool go = false;

		Task.Run(async () =>
		{
			UdpClient receivingUdpClient = new(45234);
			long j = 0;

			while (!go)
			{
				try
				{
					var result = await receivingUdpClient.ReceiveAsync();
					byte[] receiveBytes = result.Buffer;

					VBANHeader header = VBANHeader.Read(receiveBytes);

					var data = new
					{
						header.VBAN,
						header.Name,
						header.SampleRate,
						header.SubProtocol,
						header.NbSamples,
						header.NbChannels,
						header.BitResolution,
						header.Codec
					};

					//Console.WriteLine(JsonSerializer.Serialize(data));
					//Console.WriteLine("size: " + header.NbChannels * header.NbSamples * 2 + " of " + receiveBytes.Length);

					int incomingData = (receiveBytes.Length - 28) / 4;
					if(j >= mybuffer.Length - 2*incomingData)
					{
						go = true;
						Console.WriteLine("done");
						break;
					}

					for(int f = 28; f < receiveBytes.Length; f += 4)
					{
						mybuffer[j] = (short)(receiveBytes[f + 1] << 8 | receiveBytes[f]);
						mybuffer[j + 1] = (short)(receiveBytes[f + 3] << 8 | receiveBytes[f + 2]);

						j += 2;
					}
				}
				catch(Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		});

		Console.WriteLine("waiting");
		while(!go)
		{
			Thread.Sleep(100);
		}
		Console.WriteLine("going");

		int played = 0;
		XtOnBuffer processUdp = (XtStream stream, in XtBuffer buffer, object user) =>
		{
			XtSafeBuffer safe = XtSafeBuffer.Get(stream);
			safe.Lock(in buffer);
			short[] output = (short[])safe.GetOutput();

			int length = Math.Min(output.Length, mybuffer.Length);
			mybuffer[played..(played+length)].CopyTo(output, 0);
			played += length;

			safe.Unlock(in buffer);
			return 0;
		};

		XtBufferSize size = device.GetBufferSize(Format);
		XtStreamParams streamParams = new(true, processUdp, null, null);
		XtDeviceStreamParams deviceParams = new(in streamParams, in Format, size.current);
		using XtStream stream = device.OpenStream(in deviceParams, null);
		using XtSafeBuffer safe = XtSafeBuffer.Register(stream);

		stream.Start();
		Thread.Sleep(10000);
		stream.Stop();
	}
}