using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VBANdotnet.CLI;
using Xt;


public static class Program
{
	private static readonly XtMix Mix = new(48000, XtSample.Int16);
	private static readonly XtChannels Channels = new(0, 0, 2, 0);
	private static readonly XtFormat Format = new(Mix, Channels);

	public static Pipe Data = new(new PipeOptions());

	[STAThread]
	public static async Task Main()
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

		int ProcessUdp(XtStream stream, in XtBuffer buffer, object user)
		{
			PipeReader reader = Data.Reader;
			XtSafeBuffer safe = XtSafeBuffer.Get(stream);
			safe.Lock(in buffer);
			short[] output = (short[])safe.GetOutput();

			if(reader.TryRead(out ReadResult result))
			{
				int size = (int)(result.Buffer.Length / 4) * 4;
				int maxIterations = Math.Min(buffer.frames, size / 4);

				for(int i = 0; i < maxIterations; i++)
				{
					ReadOnlySequence<byte> data = result.Buffer.Slice(i * 4, 4);

					AudioData audioData = AudioData.Read(data.ToArray());
					output[i*2] = audioData.Left;
					output[i*2+1] = audioData.Right;
				}

				reader.AdvanceTo(result.Buffer.GetPosition(maxIterations * 4, result.Buffer.Start));
			}
			else
			{
				Console.WriteLine("missing audio");
			}

			safe.Unlock(in buffer);
			return 0;
		}

		XtBufferSize size = device.GetBufferSize(Format);
		XtStreamParams streamParams = new(true, ProcessUdp, null, null);
		XtDeviceStreamParams deviceParams = new(in streamParams, in Format, size.current);
		using XtStream stream = device.OpenStream(in deviceParams, null);
		using XtSafeBuffer safe = XtSafeBuffer.Register(stream);

		stream.Start();

		using IHost host = Host.CreateDefaultBuilder()
			.ConfigureServices(services =>
			{
				services.AddHostedService<UDPService>();
			})
			.Build();

		await host.RunAsync();
	}
}