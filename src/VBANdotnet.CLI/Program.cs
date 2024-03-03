using System.Net;
using System.Threading.Channels;
using VBANdotnet.CLI;
using Xt;

public static class Program
{
	public static readonly Channel<AudioData> Data = Channel.CreateUnbounded<AudioData>(
		new UnboundedChannelOptions
		{
			SingleWriter = true,
			SingleReader = true
		});

	private static readonly XtMix s_mix = new(48000, XtSample.Int16);
	private static readonly XtChannels s_channels = new(0, 0, 2, 0);
	private static readonly XtFormat s_format = new(s_mix, s_channels);

	[STAThread]
	public static void Main()
	{
		// setup XtAudio
		using XtPlatform platform = XtAudio.Init(null, IntPtr.Zero);
		XtSystem system = platform.SetupToSystem(XtSetup.ConsumerAudio);
		XtService service = platform.GetService(system);
		if(service == null)
		{
			return;
		}

		using XtDeviceList list = service.OpenDeviceList(XtEnumFlags.All);
		for(int d = 0; d < list.GetCount(); d++)
		{
			string id = list.GetId(d);
			Console.WriteLine(system + ": " + list.GetName(id));
		}

		string defaultOutput = service.GetDefaultDeviceId(true);
		if(defaultOutput == null)
		{
			return;
		}

		using XtDevice device = service.OpenDevice(defaultOutput);
		if(!device.SupportsFormat(s_format))
		{
			return;
		}

		XtBufferSize size = device.GetBufferSize(s_format);
		Console.WriteLine($"starting with buffer of size {size.current}");

		// start receiving the audio stream
		AudioClient audioClient = new(IPAddress.Any, 6980);
		audioClient.Start();

		// copy audio stream to output
		XtStreamParams streamParams = new(true, ProcessUdp, null, null);
		XtDeviceStreamParams deviceParams = new(in streamParams, in s_format, size.min);
		using XtStream stream = device.OpenStream(in deviceParams, null);
		stream.Start();

		while(true)
		{
			Thread.Sleep(100);
		}
	}

	private static unsafe int ProcessUdp(XtStream stream, in XtBuffer buffer, object user)
	{
		int size = buffer.frames;
		for(int i = 0; i < size; i++)
		{
			if(!Data.Reader.TryRead(out AudioData audioData))
			{
				break;
			}

			// the PCM channels are interleaved
			((short*)buffer.output)[i * 2] = audioData.Left;
			((short*)buffer.output)[i * 2 + 1] = audioData.Right;
		}

		return 0;
	}
}