using System.Text;

#nullable enable
namespace VBANdotnet.CLI;

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
}