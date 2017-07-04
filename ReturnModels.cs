using System;
using System.Collections.Generic;

using DnsClient;
using DnsClient.Protocol;

public class ReturnModelA
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public string Address { get; set; }

    public static Func<ARecord, ReturnModelA> FromRecord =
        e => new ReturnModelA
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Address = e.Address.ToString(),
        };
}

public class ReturnModelAaaa
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public string Address { get; set; }

    public static Func<AaaaRecord, ReturnModelAaaa> FromRecord =
        e => new ReturnModelAaaa
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Address = e.Address.ToString(),
        };
}

public class ReturnModelCaa
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public byte Flags { get; set; }
    public string Tag { get; set; }
    public string Value { get; set; }

    public static Func<CaaRecord, ReturnModelCaa> FromRecord =
        e => new ReturnModelCaa
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Flags = e.Flags,
            Tag = e.Tag,
            Value = e.Value,
        };
}

public class ReturnModelMx
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public ushort Priority { get; set; }
    public string Server { get; set; }

    public static Func<MxRecord, ReturnModelMx> FromRecord =
        e => new ReturnModelMx
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Priority = e.Preference,
            Server = e.Exchange,
        };
}

public class ReturnModelNs
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public string Server { get; set; }

    public static Func<NsRecord, ReturnModelNs> FromRecord =
        e => new ReturnModelNs
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Server = e.NSDName,
        };
}

public class ReturnModelSrv
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public string Server { get; set; }
    public ushort Port { get; set; }
    public ushort Priority { get; set; }

    public static Func<SrvRecord, ReturnModelSrv> FromRecord =
        e => new ReturnModelSrv
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Server = e.Target,
            Port = e.Port,
            Priority = e.Priority,
        };
}

public class ReturnModelTxt
{
    public string Name { get; set; }
    public int TTL { get; set; }
    public ICollection<string> Text { get; set; }

    public static Func<TxtRecord, ReturnModelTxt> FromRecord =
        e => new ReturnModelTxt
        {
            Name = e.DomainName,
            TTL = e.TimeToLive,
            Text = e.Text,
        };
}