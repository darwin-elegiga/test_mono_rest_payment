using System;
using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="StatusFileStpContent"/> class
    /// </summary>
    public class StatusFileStpContentTests
    {
        public class Constructor : StatusFileStpContentTests
        {
            [Fact]
            public void ThrowsArgumentNullException_WhenStatusFileIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new StatusFileStpContent(null));
            }

            [Fact]
            public void InitializesStpGroupsAsEmpty_WhenStatusFileHasNoRecords()
            {
                var statusFile = new StatusFile
                {
                    Header = new StatusHeader { FileName = "testfile.txt" },
                    Records = new List<StatusRecord>()
                };
                var stpContent = new StatusFileStpContent(statusFile);
                Assert.NotNull(stpContent.StpGroups);
                Assert.Empty(stpContent.StpGroups);
            }

            [Fact]
            public void InitializesStpGroupsCorrectly_WhenStatusFileHasStpRecords()
            {
                var statusFile = new StatusFile
                {
                    Header = new StatusHeader { FileName = "testfile.txt" },
                    Records = new List<StatusRecord>
                    {
                        new StatusRecord { NETWORKTYPE = "STP", NETWORKNAME = "Network1", NETWORKENROLLMENTTYPE = "B2B", TXID = "1001" },
                        new StatusRecord { NETWORKTYPE = "STP", NETWORKNAME = "Network1", NETWORKENROLLMENTTYPE = "B2B", TXID = "1002" },
                        new StatusRecord { NETWORKTYPE = "STP", NETWORKNAME = "Network2", NETWORKENROLLMENTTYPE = "C2B", TXID = "2001" },
                        new StatusRecord { NETWORKTYPE = "OTHER", NETWORKNAME = "Network3", NETWORKENROLLMENTTYPE = "B2B", TXID = "3001" }
                    }
                };
                var stpContent = new StatusFileStpContent(statusFile);
                Assert.NotNull(stpContent.StpGroups);

                var stpGroupsList = stpContent.StpGroups.ToList();
                Assert.Equal(2, stpGroupsList.Count);

                var group1 = stpGroupsList.FirstOrDefault(g => g.NetworkName == "Network1" && g.EnrollType == "B2B");
                Assert.NotNull(group1);
                Assert.Equal(new List<long> { 1001, 1002 }, group1.TransactionIds.ToList());
                var group2 = stpGroupsList.FirstOrDefault(g => g.NetworkName == "Network2" && g.EnrollType == "C2B");
                Assert.NotNull(group2);
                Assert.Equal(new List<long> { 2001 }, group2.TransactionIds.ToList());
                var group3 = stpGroupsList.FirstOrDefault(g => g.NetworkName == "Network3");
                Assert.Null(group3);                
            }
        }
    }
}
