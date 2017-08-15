using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Mvc;

using DnsClient;
using DnsClient.Protocol;
using System.Text;

namespace dns_api.Controllers
{
    public class NameServer
    {
        public NameServer(string zone, string hostName, IPAddress address)
        {
            Zone = zone;
            HostName = hostName;
            Address = address;
        }

        public string Zone { get; set; }
        public string HostName { get; set; }
        public IPAddress Address { get; set; }
    }

    class NameServerEqualityComparer : IEqualityComparer<NameServer>
    {
        public bool Equals(NameServer s1, NameServer s2)
        {
            if ((s1.Zone == s2.Zone) && (s1.HostName == s2.HostName) && s1.Address.Equals(s2.Address))
                return true;
            else
                return false;
        }

        public int GetHashCode(NameServer s)
        {
            return s.GetHashCode();
        }
    }

    [Route("api/v1")]
    public class Version1 : Controller
    {
        ILookupClient _client;

        public Version1(ILookupClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        private OkObjectResult CreateRecordResult<T>(IDnsQueryResponse response, IEnumerable<T> records)
        {
            return Ok(new
            {
                Headers = new
                {
                    AuthoritativeAnswer = response.Header.HasAuthorityAnswer,
                    TruncatedResponse = response.Header.ResultTruncated,
                    RecursionDesired = response.Header.RecursionDesired,
                    RecursionAvailable = response.Header.RecursionAvailable,
                    AuthenticData = response.Header.IsAuthenticData,
                    CheckingDisabled = response.Header.IsCheckingDisabled,
                },
                NameServer = response.NameServer.Endpoint.Address.ToString(),
                Records = records,
            });
        }

        private NameServer[] rootServers = new NameServer[]
        {
            new NameServer(".", "A.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 198, 41,  0,   4   })),
            new NameServer(".", "B.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 228, 79,  201 })),
            new NameServer(".", "C.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 33,  4,   12  })),
            new NameServer(".", "D.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 199, 7,   91,  13  })),
            new NameServer(".", "E.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 203, 230, 10  })),
            new NameServer(".", "F.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 5,   5,   241 })),
            new NameServer(".", "G.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 112, 36,  4   })),
            new NameServer(".", "H.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 198, 97,  190, 53  })),
            new NameServer(".", "I.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 36,  148, 17  })),
            new NameServer(".", "J.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 192, 58,  128, 30  })),
            new NameServer(".", "K.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 193, 0,   14,  129 })),
            new NameServer(".", "L.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 199, 7,   83,  42  })),
            new NameServer(".", "M.ROOT-SERVERS.NET.", new IPAddress(new byte[] { 202, 12,  27,  33  }))
        };

        private async Task<IDnsQueryResponse> QueryNonRecursiveServer(string name, QueryType qType, QueryClass qClass, IPAddress server)
        {
            ILookupClient client = new LookupClient(server);
            client.UseCache = false;
            client.Recursion = false;
            return await client.QueryAsync(name, qType, qClass);
        }

        // GET api/v1/trace
        [HttpGet("trace")]
        public async Task<IActionResult> Trace([FromQuery]string name)
        {
            StringBuilder builder = new StringBuilder();

            NameServer[] servers = rootServers;
            string currentZone = ".";
            string normalizedQuery = name.ToUpper();

            builder.AppendLine("Beginning query for " + normalizedQuery);
            builder.AppendLine("Querying " + currentZone + " for " + normalizedQuery);

            bool recursionFinished = false;

            while (!recursionFinished)
            {
                List<List<NameServer>> delegations = new List<List<NameServer>>();

                foreach (var server in servers)
                {
                    builder.AppendLine("Querying " + server.HostName + " at " + server.Address + " for " + normalizedQuery);
                    var response = await QueryNonRecursiveServer(name, QueryType.A, QueryClass.IN, server.Address);
                    if (response.Answers.Count > 0)
                    {
                        foreach (var record in response.Answers)
                        {
                            builder.AppendLine(record.RecordToString());
                        }
                    }
                    else if (response.Authorities.Count > 0)
                    {
                        List<NameServer> nameServers = new List<NameServer>();

                        foreach (var record in response.Authorities.NsRecords())
                        {
                            try
                            {
                                var addtional = response.Additionals.ARecords().Where(r => r.DomainName.Value == record.NSDName.Value).First();
                                nameServers.Add(new NameServer(record.DomainName.ToString().ToUpper(), record.NSDName.Value.ToUpper(), addtional.Address));
                            }
                            catch
                            {
                                builder.Append("Warning no glue for " + record.NSDName.Value.ToUpper() + " resolved to ");
                                var nsQuery = await _client.QueryAsync(record.NSDName.Value, QueryType.A);
                                builder.AppendLine(nsQuery.Answers.ARecords().First().Address.ToString() + " using a public resolver");
                                nameServers.Add(new NameServer(record.DomainName.ToString().ToUpper(), record.NSDName.Value.ToUpper(), nsQuery.Answers.ARecords().First().Address));
                            }
                        }

                        delegations.Add(nameServers);
                    }
                }

                if(delegations.Count > 0)
                {
                    string newZone = delegations.First().First().Zone;
                    builder.Append("Received delegation from " + currentZone + " to " + newZone + " ");
                    currentZone = newZone;
                    bool delegationsMatch = true;

                    foreach (var delegation in delegations)
                    {
                        if (!delegation.All(x => delegations.First().Contains(x, new NameServerEqualityComparer())) || delegation.Count != delegations.First().Count)
                        {
                            delegationsMatch = false;
                        }
                    }

                    if (delegationsMatch)
                    {
                        builder.AppendLine("-- verified all responses match");

                        foreach (var nameServer in delegations.First())
                        {
                            builder.AppendLine("Delegation to " + nameServer.HostName + " at " + nameServer.Address);
                        }

                        servers = new NameServer[delegations.First().Count];
                        delegations.First().CopyTo(servers);
                    }
                    else
                    {
                        builder.AppendLine("-- error delegation mismatch");
                        recursionFinished = true;
                    }
                }
                else
                {
                    builder.AppendLine("Answer received");
                    recursionFinished = true;
                }
            }

            return Ok(builder.ToString());
        }

        // GET api/v1/a
        [HttpGet("a")]
        public async Task<IActionResult> GetA([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.A);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.ARecords();
            var returnModels = records.Select(ReturnModelA.FromRecord);
            return CreateRecordResult(response, returnModels);
        }

        // GET api/v1/aaaa
        [HttpGet("aaaa")]
        public async Task<IActionResult> GetAaaa([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.AAAA);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.AaaaRecords();
            var returnModels = records.Select(ReturnModelAaaa.FromRecord);
            return CreateRecordResult(response, returnModels);
        }


        // GET api/v1/caa
        [HttpGet("caa")]
        public async Task<IActionResult> GetCaa([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.CAA);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.CaaRecords();
            var returnModels = records.Select(ReturnModelCaa.FromRecord);
            return CreateRecordResult(response, returnModels);
        }

        // GET api/v1/mx
        [HttpGet("mx")]
        public async Task<IActionResult> GetMx([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.MX);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.MxRecords();
            var returnModels = records.Select(ReturnModelMx.FromRecord);
            return CreateRecordResult(response, returnModels);
        }

        // GET api/v1/ns
        [HttpGet("ns")]
        public async Task<IActionResult> GetNs([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.NS);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.NsRecords();
            var returnModels = records.Select(ReturnModelNs.FromRecord);
            return CreateRecordResult(response, returnModels);
        }

        // GET api/v1/srv
        [HttpGet("srv")]
        public async Task<IActionResult> GetSrv([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.SRV);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.SrvRecords();
            var returnModels = records.Select(ReturnModelSrv.FromRecord);
            return CreateRecordResult(response, returnModels);
        }

        // GET api/v1/txt
        [HttpGet("txt")]
        public async Task<IActionResult> GetTxt([FromQuery]string name)
        {
            var response = await _client.QueryAsync(name, QueryType.TXT);
            if(response.HasError)
                return BadRequest(response.Header);

            var records = response.Answers.TxtRecords();
            var returnModels = records.Select(ReturnModelTxt.FromRecord);
            return CreateRecordResult(response, returnModels);
        }
    }
}
