﻿using System;
using System.Net;
using System.Threading.Tasks;
using Neo4jClient.ApiModels.Cypher;
using Neo4jClient.Cypher;
using Neo4jClient.Execution;
using NUnit.Framework;

namespace Neo4jClient.Test.GraphClientTests
{
    [TestFixture]
    public class FactoryTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfConfigurationIsNotDefined()
        {
            new GraphClientFactory(null);
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public void ShouldThrowExceptionIfRootApiIsNotDefined()
        {
            using (var testHarness = new RestTestHarness
            {
                { MockRequest.Get("/"), new MockResponse { StatusCode = HttpStatusCode.OK } }
            })
            {
                var httpClient = testHarness.GenerateHttpClient(testHarness.BaseUri);

                var executeConfiguration = new ExecutionConfiguration
                {
                    HttpClient = httpClient,
                    UserAgent =
                        string.Format("Neo4jClient/{0}", typeof(NeoServerConfiguration).Assembly.GetName().Version),
                    UseJsonStreaming = true,
                    JsonConverters = GraphClient.DefaultJsonConverters
                };

                NeoServerConfiguration.GetConfiguration(new Uri(testHarness.BaseUri), null, null, executeConfiguration);
            }
        }

        [Test]
        public void GraphClientFactoryUseCase()
        {
            const string queryText = @"MATCH (d) RETURN d";

            var cypherQuery = new CypherQuery(queryText, null, CypherResultMode.Set, CypherResultFormat.Rest);
            var cypherApiQuery = new CypherApiQuery(cypherQuery);

            using (var testHarness = new RestTestHarness
            {
                { MockRequest.Get("/"), MockResponse.NeoRoot() },
                { MockRequest.PostObjectAsJson("/cypher", cypherApiQuery), new MockResponse { StatusCode = HttpStatusCode.OK } }
            })
            {
                var httpClient = testHarness.GenerateHttpClient(testHarness.BaseUri);

                var executeConfiguration = new ExecutionConfiguration
                {
                    HttpClient = httpClient,
                    UserAgent =
                        string.Format("Neo4jClient/{0}", typeof(NeoServerConfiguration).Assembly.GetName().Version),
                    UseJsonStreaming = true,
                    JsonConverters = GraphClient.DefaultJsonConverters
                };

                var configuration = NeoServerConfiguration.GetConfiguration(new Uri(testHarness.BaseUri), null, null, executeConfiguration);

                var factory = new GraphClientFactory(configuration);

                using (var client = factory.Create(httpClient))
                {
                    client.Cypher.Match("(d)").Return<object>("d").ExecuteWithoutResults();
                }
            }
        }
    }
}
