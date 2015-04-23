/*
 * Copyright (c) 2015 Cees de Groot
 * Apache License, Version 2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PagerDutyAPI;

namespace PagerDutyAPITests
{
    [TestClass]
    public class IntegrationAPITest
    {
        [TestMethod]
        public void TestSendEvent()
        {
            var apiClientInfo = new APIClientInfo("pagerduty", "http://www.pagerduty.com");
            var client = PagerDutyAPI.IntegrationAPI.MakeClient(apiClientInfo, "HKEY_CURRENT_USER", "Test Service");
            var incidentKey = System.Guid.NewGuid().ToString();

            var response = client.Trigger("test event", "test data", incidentKey);
            AssertResponseIsSuccess(incidentKey, response);

            response = client.Acknowledge(incidentKey, "test ack", "more test data");
            AssertResponseIsSuccess(incidentKey, response);

            response = client.Resolve(incidentKey, "test resolve", "even more test data");
            AssertResponseIsSuccess(incidentKey, response);
        }

        private static void AssertResponseIsSuccess(string incidentKey, EventAPIResponse response) {
            Assert.IsTrue(response.IsSuccess(), "unexpected status: " + response.Status);
            Assert.AreEqual("Event processed", response.Message);
            Assert.AreEqual(incidentKey, response.IncidentKey);
        }
    }
}
