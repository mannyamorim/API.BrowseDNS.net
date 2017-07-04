using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using DnsClient;

namespace dns_api.Controllers
{
    [Route("api/v1")]
    public class Version1 : Controller
    {
        ILookupClient _client;

        public Version1(ILookupClient client)
        {
            if(client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
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
            return Ok(returnModels);
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
            return Ok(returnModels);
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
            return Ok(returnModels);
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
            return Ok(returnModels);
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
            return Ok(returnModels);
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
            return Ok(returnModels);
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
            return Ok(returnModels);
        }
    }
}
