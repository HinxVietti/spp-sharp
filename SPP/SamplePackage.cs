using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public abstract class SamplePackageBase
{
    public static readonly Encoding DefaultEncoding = Encoding.ASCII;

    public uint Version = 1;// 0 0 0 1 => 0.0.0.1
    public byte HeaderType = 0;
    public byte DataType = 0;

    public virtual int PackageType { get; } = 829452403; //spp1
    public virtual uint HeaderSize { get { return (uint)Header.Length; } }
    public virtual uint DataSize { get { return (uint)Data.Length; } }

    public abstract byte[] Header { get; }
    public abstract byte[] Data { get; }


    public byte[][] CopyTo(Stream stream)
    {
        stream.Position = 0;
        //reserved
        stream.WriteByte(255);                                      // #0
        //type = spp1
        var ptype = BitConverter.GetBytes(PackageType);
        stream.Write(ptype, 0, 4);                                  // #1
        //reserved
        stream.WriteByte(255);                                      // #5
        stream.WriteByte(254);                                      // #6
        stream.WriteByte(10);                                      // #7

        var v = BitConverter.GetBytes(Version);
        stream.Write(v, 0, 4);                                      // #8

        var hs = BitConverter.GetBytes(HeaderSize);
        stream.Write(hs, 0, 4);                                     // #12
        stream.WriteByte(HeaderType);                               // #16

        var ds = BitConverter.GetBytes(DataSize);
        stream.Write(ds, 0, 4);                                     // #17
        stream.WriteByte(DataType);                                 // #21

        //22bytes

        var hdata = Header;
        stream.Write(hdata, 0, hdata.Length);
        var ddata = Data;
        stream.Write(ddata, 0, ddata.Length);

        var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hmd5 = md5.ComputeHash(hdata);
        byte[] dmd5 = md5.ComputeHash(ddata);

        stream.Write(hmd5, 0, hmd5.Length);
        stream.Write(dmd5, 0, dmd5.Length);

        stream.Flush();

        var res = new byte[2][];
        res[0] = hmd5;
        res[1] = dmd5;
        return res;
    }
}


public class SamplePackage : SamplePackageBase
{
    public static readonly Encoding HeaderTextEncoding = Encoding.UTF8;
    public const int MD5SPHL = 128;//bit

    public override byte[] Header => GetHeaderData();
    public override byte[] Data => GetBodyData();
    public override int PackageType { get => m_ptye; }

    private int m_ptye = 829452403;
    private byte[] m_data;
    private string m_header;


    public virtual string GetHeader()
    {
        return m_header;
    }


    public SamplePackage(int t = 829452403)
    {
        m_ptye = t;
    }

    private byte[] GetHeaderData()
    {
        string header = GetHeader();
        return HeaderTextEncoding.GetBytes(header);
    }


    protected virtual byte[] GetBodyData()
    {
        return m_data;
    }


    public void SetData(byte[] data)
    {
        m_data = data;
    }

    public void SetHeader(byte[] header)
    {
        m_header = HeaderTextEncoding.GetString(header);
    }


    public static SamplePackage GetPackage(Stream stream, out byte[] headermd5, out byte[] datamd5)
    {
        try
        {
            stream.Position = 0;

            stream.ReadByte();

            var intbuffer = new byte[4];
            stream.Read(intbuffer, 0, 4);
            int ptype = BitConverter.ToInt32(intbuffer, 0);

            //reserved.. 3bytes
            stream.ReadByte();
            stream.ReadByte();
            stream.ReadByte();

            //stream.Read(intbuffer, 0, 4);
            uint v = SPPUTil.NextUint(stream);
            uint hsize = SPPUTil.NextUint(stream);
            byte ht = (byte)stream.ReadByte();
            uint dsize = SPPUTil.NextUint(stream);
            byte dt = (byte)stream.ReadByte();

            var header = new byte[hsize];
            stream.Read(header, 0, header.Length);

            var data = new byte[dsize];
            stream.Read(data, 0, data.Length);

            headermd5 = new byte[MD5SPHL / 8]; //8bit per byte
            datamd5 = new byte[MD5SPHL / 8];

            stream.Read(headermd5, 0, headermd5.Length);
            stream.Read(datamd5, 0, datamd5.Length);

            var sp = new SamplePackage(ptype)
            {
                Version = v,
                HeaderType = ht,
                DataType = dt,
            };
            sp.SetHeader(header);
            sp.SetData(data);
            return sp;
        }
        catch (Exception e)
        {
            //return null;
            throw e;
        }
    }

}


public static class SPPUTil
{
    public static bool TryNextUint(Stream stream, out uint val)
    {
        var array = new byte[4];
        if (stream.Read(array, 0, 4) == 4)
        {
            val = BitConverter.ToUInt32(array, 0);
            return true;
        }
        val = 0;
        return false;
    }

    public static uint NextUint(Stream stream)
    {
        if (TryNextUint(stream, out var val))
            return val;
        return 0;
    }

}